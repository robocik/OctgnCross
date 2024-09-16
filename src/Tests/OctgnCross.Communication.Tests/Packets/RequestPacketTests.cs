using NUnit.Framework;
using Octgn.Communication.Packets;
using NUnit.Framework.Legacy;
using Octgn.Communication.Serializers;
using System.Text;

namespace Octgn.Communication.Tests.Packets
{
    public class RequestPacketTests
    {
        [TestCase]
        public void XML_Serialization()
        {
            Serialization(new XmlSerializer());
        }

        [TestCase]
        public void JSON_Serialization()
        {
            Serialization(new JsonSerializer());
        }

        private void Serialization(ISerializer serializer)
        {
            var req = new RequestPacket("asdf");

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.Default, true))
            using (var reader = new BinaryReader(ms, Encoding.Default, true)) {
                req.Serialize(writer, serializer);

                ms.Position = 0;

                var r2 = new RequestPacket("name");
                r2.Deserialize(reader, serializer);

                ClassicAssert.AreEqual(req.Name, r2.Name);
            }
        }
    }
}
