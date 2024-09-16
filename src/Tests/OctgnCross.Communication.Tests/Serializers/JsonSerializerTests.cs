using System;
using NUnit.Framework;
using Octgn.Communication.Serializers;
using System.Text;
using NUnit.Framework.Legacy;
using Octgn.Communication.Packets;

namespace Octgn.Communication.Tests.Serializers
{
    [TestFixture]
    public class JsonSerializerTests : TestBase
    {
        [TestCase]
        public void Serialize_Works() {
            var serializer = new JsonSerializer();
            var packet = new RequestPacket("test") {
                ["test2"] = "test3"
            };

            var serialized = serializer.Serialize(packet);

            var str = Encoding.UTF8.GetString(serialized);
            Console.WriteLine(str);

            var unserialized = (RequestPacket)serializer.Deserialize(typeof(RequestPacket), serialized);

            ClassicAssert.NotNull(unserialized);

            ClassicAssert.AreEqual("test", unserialized.Name);
            ClassicAssert.AreEqual("test3", unserialized["test2"]);
        }
    }
}
