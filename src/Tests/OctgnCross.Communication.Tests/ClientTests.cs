﻿using NUnit.Framework;
using Octgn.Communication.Packets;
using Octgn.Communication.Serializers;
using Octgn.Communication.Tcp;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;

namespace Octgn.Communication.Tests
{
    [TestFixture]
    public class ClientTests : TestBase
    {
        [TestCase]
        public async Task ConnectSucceeds_ConnectedEvent_ThrowsException()
        {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                var expectedException = new NotImplementedException();

                void Signal_OnException(object sender, ExceptionEventArgs args) {
                    if (args.Exception != expectedException)
                        throw args.Exception;
                }

                try {
                    Signal.OnException += Signal_OnException;
                    using (var client = CreateClient("a")) {
                        client.Connected += (_, __) => throw expectedException;
                        await client.Connect("localhost:" + port);
                    }
                } finally {
                    Signal.OnException -= Signal_OnException;
                }
            }
        }

        [TestCase]
        public async Task Connect_ThrowsException_ConnectCalledMoreThanOnce()
        {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                using (var client = CreateClient("a")) {
                    await client.Connect("localhost:" + port);

                    try
                    {
                        await client.Connect("localhost:" + port);
                        ClassicAssert.Fail("Should have thrown an exception");
                    } catch (InvalidOperationException)
                    {
                        ClassicAssert.Pass();
                    }
                }
            }
        }

        [TestCase]
        public async Task RequestReceived_GetsInvoked_WhenRequestIsReceived()
        {
            var port = NextPort;

            using (var server = new Server(new TcpListener(new IPEndPoint(IPAddress.Loopback, port), new TestHandshaker()), new XmlSerializer())) {
                server.Initialize();

                using (var client = CreateClient("userA")) {
                    await client.Connect("localhost:" + port);

                    var tcs = new TaskCompletionSource<RequestPacket>();

                    client.RequestReceived += (_, args) => {
                        args.IsHandled = true;
                        tcs.SetResult(null);
                        return Task.FromResult<object>(null);
                    };

                    var request = new RequestPacket("test");
                    await server.ConnectionProvider.GetConnections("userA").First().Request(request);

                    var delayTask = Task.Delay(10000);

                    var completedTask = await Task.WhenAny(tcs.Task, delayTask);

                    if (completedTask == delayTask) throw new TimeoutException();
                }
            }
        }
    }
}
