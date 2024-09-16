﻿using System;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Octgn.Communication.Modules;
using Octgn.Communication.Serializers;
using System.Net;
using System.Linq;
using Octgn.Communication.Packets;
using FakeItEasy;
using NUnit.Framework.Legacy;
using Octgn.Communication.Tcp;

namespace Octgn.Communication.Tests
{
    [TestFixture]
    public class Implementation : TestBase
    {
        [TestCase]
        public async Task TcpLayerOperates() {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                using (var client = CreateClient("user")) {
                    await client.Connect("localhost:" + port);
                }
            }
        }

        [TestCase]
        public async Task TcpLayerSurvivesDisconnect() {
            var port = NextPort;

            var serializer = new XmlSerializer();

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Attach(new PingModule());
                server.Initialize();

                using (var client = CreateClient("user")) {
                    client.ReconnectRetryDelay = TimeSpan.FromSeconds(1);
                    await client.Connect("localhost:" + port);

                    var originalConnection = client.Connection;

                    using (var eveClientConnected = new AutoResetEvent(false)) {
                        client.Connected += (a, b) => eveClientConnected.Set();

                        var connections = server.ConnectionProvider.GetConnections();

                        // Force close connection on the server, causing the client to try can call back
                        // When it calls back, the server should auto pick it back up. This should be smooth as butter.
                        connections
                            .Where(con => con.State == ConnectionState.Connected)
                            .AsParallel()
                            .ForAll(con => con.Close());

                        var pingSucceeded = false;

                        // wait for all the connections on the server to close.
                        using (var cancellation = new CancellationTokenSource(ConnectionBase.WaitForResponseTimeout)) {
                            while (connections.Any(y => y.State == ConnectionState.Connected)) {
                                await Task.Delay(10, cancellation.Token);
                                cancellation.Token.ThrowIfCancellationRequested();
                            }
                        }

                        try {
                            await client.Connection.Ping();
                            pingSucceeded = true;
                        } catch (Exception){

                        }

                        if(pingSucceeded)
                            ClassicAssert.Fail("Connection breaking should have caused the client send to fail");

                        if (!eveClientConnected.WaitOne(MaxTimeout))
                            ClassicAssert.Fail("Client never reconnected");

                        await client.Connection.Ping();
                    }
                }
            }
        }

        [TestCase]
        public async Task ClientAsyncActuallyWaits()
        {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Attach(new PingModule());
                server.Initialize();

                using (var clientA = CreateClient("clientA"))
                using (var clientB = CreateClient("clientB")) {
                    await clientA.Connect("localhost:" + port);
                    await clientB.Connect("localhost:" + port);

                    var requestTime = await clientA.Connection.Ping();

                    ClassicAssert.AreNotEqual(default(DateTime), requestTime);
                }
            }
        }

        [TestCase]
        public async Task CanSendTwoRequests() {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Attach(new PingModule());
                server.Initialize();

                using (var clientA = CreateClient("clientA"))
                using (var clientB = CreateClient("clientB")) {
                    await clientA.Connect("localhost:" + port);
                    await clientB.Connect("localhost:" + port);

                    await clientA.Connection.Ping();
                    await clientA.Connection.Ping();
                }
            }
        }

        [TestCase]
        public async Task UnhandledPacketFailsProperly() {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                // Don't attach the ping module, that way the server won't know how to handle the ping request.
                //server.Attach(new PingModule());

                server.Initialize();

                using (var clientA = CreateClient("clientA")) {
                    await clientA.Connect("localhost:" + port);

                    var delayTask = Task.Delay(10000);

                    var pingTask = clientA.Connection.Ping();

                    try {
                        var result = await clientA.Connection.Ping();
                    } catch (ErrorResponseException ex) {
                        ClassicAssert.AreEqual(ErrorResponseCodes.UnhandledRequest, ex.Code);
                        return;
                    }

                    ClassicAssert.Fail("Ping request didn't throw an exception");
                }
            }
        }

        [TestCase]
        public async Task NewUser_GetsAddedToConnectionProvider() {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                using (var clientA = CreateClient("clientA")) {
                    await clientA.Connect("localhost:" + port);

                    ClassicAssert.AreEqual(1, server.ConnectionProvider.GetConnections().Count());

                    var con = server.ConnectionProvider.GetConnections("clientA").Single();
                }

            }
        }


        [TestCase]
        public async Task SendUserMessage() {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                using (var clientA = CreateClient("clientA"))
                using (var clientB = CreateClient("clientB")) {
                    await clientA.Connect("localhost:" + port);
                    await clientB.Connect("localhost:" + port);

                    using (var eveMessageReceived = new AutoResetEvent(false)) {
                        string messageBody = null;

                        clientB.RequestReceived += (_, args) => {
                            if (!(args.Request is Message message)) return Task.FromResult<object>(null);

                            messageBody = message.Body;

                            args.IsHandled = true;

                            eveMessageReceived.Set();

                            return Task.FromResult<object>(null);
                        };

                        var result = await clientA.SendMessage(clientB.User.Id, "asdf");

                        ClassicAssert.IsNotNull(result);

                        ClassicAssert.AreEqual("asdf", messageBody);

                        if (!eveMessageReceived.WaitOne(MaxTimeout))
                            ClassicAssert.Fail("clientB never got their message :(");
                    }
                }
            }
        }

        [TestCase]
        public async Task SendMessageToOfflineUser_ThrowsException() {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                using (var clientA = CreateClient("clientA")) {
                    await clientA.Connect("localhost:" + port);

                    try {
                        var result = await clientA.Request(new Message("clientB", "asdf"));
                        ClassicAssert.Fail("Request should have failed");
                    } catch (ErrorResponseException ex) {
                        ClassicAssert.AreEqual(Octgn.Communication.ErrorResponseCodes.UserOffline, ex.Code);
                    }
                }
            }
        }

        [TestCase]
        public async Task SendMessage_Fails_IfNoResponseNotHandledByReceiver() {
            var port = NextPort;

            ConnectionBase.WaitForResponseTimeout = TimeSpan.FromSeconds(60);

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                using (var clientA = CreateClient("clientA"))
                using (var clientB = CreateClient("clientB")) {
                    await clientA.Connect("localhost:" + port);
                    await clientB.Connect("localhost:" + port);

                    using (var eveMessageReceived = new AutoResetEvent(false)) {
                        string messageBody = null;

                        clientB.RequestReceived += (_, args) => {
                            if (!(args.Request is Message message)) return Task.FromResult<object>(null);

                            messageBody = message.Body;

                            // Don't set a response, this should trigger the error
                            //args.Response = new ResponsePacket(args.Request);

                            eveMessageReceived.Set();
                            return Task.FromResult<object>(null);
                        };

                        var sendTask = clientA.SendMessage(clientB.User.Id, "asdf");

                        if (!eveMessageReceived.WaitOne(MaxTimeout))
                            ClassicAssert.Fail("clientB never got their message :(");

                        try {
                            var result = await sendTask;
                        } catch (ErrorResponseException ex) {
                            ClassicAssert.AreEqual(ErrorResponseCodes.UnhandledRequest, ex.Code);
                            return;
                        }

                        ClassicAssert.Fail("SendMessage should have failed due to no response being sent back");
                    }
                }
            }
        }


        [TestCase]
        public async Task FailedHandshakeCausesServerToDisconnectClient() {
            var port = NextPort;

            var handshaker = A.Fake<IHandshaker>();

            A.CallTo(() => handshaker.OnHandshakeRequest(A<HandshakeRequestPacket>.Ignored, A<IConnection>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(new HandshakeResult() {
                    ErrorCode = "TestError"
                }));

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), handshaker), new XmlSerializer())) {
                var serverModule = new ServerModuleEvents();
                server.Attach(serverModule);
                server.Initialize();

                // Need to handle the 'hello' packet we send
                serverModule.Request += (sender, args) => {
                    if (args.Request.Name == "hello")
                        args.IsHandled = true;
                };

                using (var client = CreateClient("bad")) {
                    try {
                        await client.Connect("localhost:" + port);
                    } catch (HandshakeException ex) {
                        ClassicAssert.AreEqual(ex.ErrorCode, "TestError");
                    }

                    try {
                        await client.Request(new RequestPacket("hello"));
                        ClassicAssert.Fail("client request succeeded, it shouldn't have");
                    } catch (DisconnectedException) { } catch (NotConnectedException) { }

                    // make sure we're not connected
                    ClassicAssert.IsFalse(client.IsConnected);
                }
            }
        }

    }
}
