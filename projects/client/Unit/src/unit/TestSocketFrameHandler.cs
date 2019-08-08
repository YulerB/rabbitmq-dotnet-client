using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Unit.src.unit
{
    [TestFixture]
    public class TestSocketFrameHandler
    {
        [Test]
        public void TestCreateSocketFrameHandler()
        {
            SocketFrameHandler handler = new SocketFrameHandler(
                new RabbitMQ.Client.AmqpTcpEndpoint(), 
                (a) => new TcpClientAdapter(new Socket(a, SocketType.Stream, ProtocolType.Tcp)), 
                1, 1, 1);
        }
        [Test]
        public void TestWriteFrame()
        {
            SocketFrameHandler handler = new SocketFrameHandler(
                new RabbitMQ.Client.AmqpTcpEndpoint(),
                (a) => new TcpClientAdapter(new Socket(a, SocketType.Stream, ProtocolType.Tcp)),
                1, 1, 1);
            handler.WriteFrame(new HeaderOutboundFrame(1, new RabbitMQ.Client.Framing.BasicProperties(), 1));
        }
        [Test]
        public void TestWriteFrameSet()
        {
            SocketFrameHandler handler = new SocketFrameHandler(
                new RabbitMQ.Client.AmqpTcpEndpoint(),
                (a) => new TcpClientAdapter(new Socket(a, SocketType.Stream, ProtocolType.Tcp)),
                1, 1, 1);
            handler.WriteFrameSet(new List<OutboundFrame> { new HeaderOutboundFrame(1, new RabbitMQ.Client.Framing.BasicProperties(), 1) ,
                 new HeaderOutboundFrame(1, new RabbitMQ.Client.Framing.BasicProperties(), 1)
                });
        }
    }
}
