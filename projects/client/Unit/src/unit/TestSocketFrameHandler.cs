// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2016 Pivotal Software, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestSocketFrameHandler
    {
        [Test]
        public void TestCreateSocketFrameHandler()
        {
            SocketFrameHandler handler = new SocketFrameHandler(
                new RabbitMQ.Client.AmqpTcpEndpoint(), 
                (a, b) => new TcpClientAdapter(new Socket(a, SocketType.Stream, ProtocolType.Tcp), b), 
                1, 1, 1);
        }
        [Test]
        public void TestWriteFrame()
        {
            SocketFrameHandler handler = new SocketFrameHandler(
                new RabbitMQ.Client.AmqpTcpEndpoint(),
                (a, b) => new TcpClientAdapter(new Socket(a, SocketType.Stream, ProtocolType.Tcp), b),
                1, 1, 1);
            handler.WriteFrame(new HeaderOutboundFrame(1, new RabbitMQ.Client.Framing.BasicProperties(), 1));
        }
        [Test]
        public void TestWriteFrameSet()
        {
            SocketFrameHandler handler = new SocketFrameHandler(
                new RabbitMQ.Client.AmqpTcpEndpoint(),
                (a, b) => new TcpClientAdapter(new Socket(a, SocketType.Stream, ProtocolType.Tcp), b),
                1, 1, 1);
            handler.WriteFrameSet(new List<OutboundFrame> { new HeaderOutboundFrame(1, new RabbitMQ.Client.Framing.BasicProperties(), 1) ,
                 new HeaderOutboundFrame(1, new RabbitMQ.Client.Framing.BasicProperties(), 1)
                });
        }
    }
}
