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

using System;
using System.IO;
using System.Text;
using System.Collections;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Impl;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestAsyncConsumer
    {
        [Test]
        public void TestAsyncEventingConsumerDeliveryEventsNoAck1()
        {
            int messages = 300000;
            int received = 0;

            using (var c = new ConnectionFactory { DispatchConsumersAsync = true }.CreateConnection())
            {
                using (var Model = c.CreateModel())
                {
                    string q = Model.QueueDeclare();

                    var data = new byte[1024];

                    AsyncEventingBasicConsumer ec = new AsyncEventingBasicConsumer(Model);

                    using (ManualResetEvent reset = new ManualResetEvent(false))
                    {
                        ec.Received += async (e, args) =>
                        {
                            if (Interlocked.Increment(ref received) == messages)
                                reset.Set();

                            await Task.FromResult(false);
                        };

                        Model.BasicConsume(q, true, ec);

                        for (int i = 0; i < messages; i++)
                        {
                            Model.BasicPublish(string.Empty, q, null, data);
                        }

                        reset.WaitOne(TimeSpan.FromMinutes(2));

                        Model.BasicCancel(ec.ConsumerTag);
                    }

                    Assert.AreEqual(messages, received);
                }
            }
        }
        
        [Test]
        public void TestBasicRoundtrip()
        {
            var cf = new ConnectionFactory{ DispatchConsumersAsync = true };
            using(var c = cf.CreateConnection())
            using(var m = c.CreateModel())
            {
                var q = m.QueueDeclare();
                var bp = m.CreateBasicProperties();
                var body = System.Text.Encoding.UTF8.GetBytes("async-hi");
                m.BasicPublish(string.Empty, q.QueueName, bp, body);
                var consumer = new AsyncEventingBasicConsumer(m);
                var are = new AutoResetEvent(false);
                consumer.Received += async (o, a) =>
                    {
                        are.Set();
                        await Task.Yield();
                    };
                var tag = m.BasicConsume(q.QueueName, true, consumer);
                // ensure we get a delivery
                var waitRes = are.WaitOne(500);
                Assert.IsTrue(waitRes);
                // unsubscribe and ensure no further deliveries
                m.BasicCancel(tag);
                m.BasicPublish(string.Empty, q.QueueName, bp, body);
                var waitResFalse = are.WaitOne(500);
                Assert.IsFalse(waitResFalse);
            }
        }

        [Test]
        public void NonAsyncConsumerShouldThrowInvalidOperationException()
        {
            var cf = new ConnectionFactory{ DispatchConsumersAsync = true };
            using(var c = cf.CreateConnection())
            using(var m = c.CreateModel())
            {
                var q = m.QueueDeclare();
                var bp = m.CreateBasicProperties();
                var body = System.Text.Encoding.UTF8.GetBytes("async-hi");
                m.BasicPublish(string.Empty, q.QueueName, bp, body);
                var consumer = new EventingBasicConsumer(m);
                Assert.Throws<InvalidOperationException>(() => m.BasicConsume(q.QueueName, false, consumer));
            }
        }
    }
}