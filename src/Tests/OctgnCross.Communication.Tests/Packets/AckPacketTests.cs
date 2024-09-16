using NUnit.Framework;
using Octgn.Communication.Packets;
using System;
using NUnit.Framework.Legacy;

namespace Octgn.Communication.Tests.Packets
{
    [TestFixture]
    public class AckPacketTests
    {
        [TestCase]
        public void PacketToString_NotNullOrWhitespace()
        {
            var packet = new Ack();

            ClassicAssert.False(string.IsNullOrWhiteSpace(packet.ToString()));
        }
    }
}
