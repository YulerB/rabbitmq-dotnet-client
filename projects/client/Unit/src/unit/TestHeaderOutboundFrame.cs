using NUnit.Framework;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unit.src.unit
{
    [TestFixture]
    public class TestHeaderOutboundFrame
    {
        [Test]
        public void TestCreateHeaderOutboundFrame()
        {
            using (var ms = new System.IO.MemoryStream())
            {
                var writter = new RabbitMQ.Util.NetworkBinaryWriter(ms);
                var channel = 1;
                HeaderOutboundFrame mFrame = new HeaderOutboundFrame(channel, new RabbitMQ.Client.Framing.BasicProperties(), 1);
                Assert.AreEqual(channel, mFrame.Channel);
                Assert.IsNull(mFrame.Payload);
                mFrame.WritePayload(writter);
                mFrame.WriteTo(writter);
                Assert.AreEqual(40, ms.Length);
            }
        }
    }
}
