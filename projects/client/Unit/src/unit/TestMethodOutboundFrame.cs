using NUnit.Framework;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unit.src.unit
{
    [TestFixture]
    public class TestMethodOutboundFrame
    {
        [Test]
        public void TestCreateMethodOutboundFrame()
        {
            using (var ms = new System.IO.MemoryStream())
            {
                var writter = new RabbitMQ.Util.NetworkBinaryWriter(ms);
                var channel = 1;
                MethodOutboundFrame mFrame = new MethodOutboundFrame(channel, new ConnectionSecureOk(new byte[1] { 1 }));
                Assert.AreEqual(channel, mFrame.Channel);
                Assert.IsNull(mFrame.Payload);
                mFrame.WritePayload(writter);
                mFrame.WriteTo(writter);
                Assert.AreEqual(30, ms.Length);
            }
        }
    }
}
