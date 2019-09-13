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
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    internal class TestConsumerOperationDispatch : IntegrationFixture
    {
        private const string x = "dotnet.tests.consumer-operation-dispatch.fanout";
        private List<IModel> channels = new List<IModel>();
        private List<string> queues = new List<string>();
        private List<CollectingConsumer> consumers = new List<CollectingConsumer>();

        // number of channels (and consumers)
        private const int y = 100;

        // number of messages to be published
        private const int n = 100;

        public static CountdownEvent counter = new CountdownEvent(y);

        [TearDown]
        protected override void ReleaseResources()
        {
            foreach (var ch in channels)
            {
                if (ch.IsOpen)
                {
                    ch.Close();
                }
            }

            queues.Clear();
            consumers.Clear();
            counter.Reset();
            base.ReleaseResources();
        }

        private class CollectingConsumer : DefaultBasicConsumer
        {
            public ConcurrentBag<ulong> DeliveryTags { get; private set; }

            public CollectingConsumer(IModel model)
                : base(model)
            {
                this.DeliveryTags = new ConcurrentBag<ulong>();
            }

            public override void HandleBasicDeliver(string consumerTag,
                ulong deliveryTag, bool redelivered, string exchange, string routingKey,
                IBasicProperties properties, byte[] body)
            {
                // we test concurrent dispatch from the moment basic.delivery is returned.
                // delivery tags have guaranteed ordering and we verify that it is preserved
                // (per-channel) by the concurrent dispatcher.
                this.DeliveryTags.Add(deliveryTag);

                if (DeliveryTags.Count == n)
                {
                    counter.Signal();
                }

                this.Model.BasicAck(deliveryTag: deliveryTag, multiple: false);
            }
        }

        [Test]
        public void TestDeliveryOrderingWithSingleChannel()
        {
            var Ch = Conn.CreateModel();
            Ch.ExchangeDeclare(x, "fanout", durable: false);

            for (int i = 0; i < y; i++)
            {
                var ch = Conn.CreateModel();
                channels.Add(ch);

                var cons = new CollectingConsumer(ch);
                consumers.Add(cons);

                var q = ch.QueueDeclare(string.Empty, durable: false, exclusive: true, autoDelete: true, arguments: null);
                ch.QueueBind(queue: q, exchange: x, routingKey: string.Empty);
                queues.Add(q);

                ch.BasicConsume(queue: q, autoAck: false, consumer: cons);
            }

            var messageBody = encoding.GetBytes("msg");
            var props = new BasicProperties();
            for (int i = 0; i < n; i++)
            {
                Ch.BasicPublish(exchange: x, routingKey: string.Empty, basicProperties: props, body: messageBody);
            }
            counter.Wait(TimeSpan.FromSeconds(30));

            foreach (var cons in consumers)
            {
                Assert.AreEqual(n,cons.DeliveryTags.Count, "Messages Not Received");
            }

            for (int i = 1; i < y; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    Assert.AreEqual(consumers[0].DeliveryTags.ToArray()[j], consumers[i].DeliveryTags.ToArray()[j], "DeliveryTag Different accross consumers");
                }
            }

            for (int i = 0; i < y; i++)
            {
                for (int j = 1; j < n; j++)
                {
                    Assert.IsTrue(
                        consumers[i].DeliveryTags.ToArray()[j] - consumers[i].DeliveryTags.ToArray()[j - 1] == 1 ||
                        (long)consumers[i].DeliveryTags.ToArray()[j] - (long)consumers[i].DeliveryTags.ToArray()[j - 1] == -1,
                        "Sequence out of order");
                }
            }


        }

        // see rabbitmq/rabbitmq-dotnet-client#61
        [Test]
        public void TestChannelShutdownDoesNotShutDownDispatcher()
        {
            using (var ch1 = Conn.CreateModel())
            {
                using (var ch2 = Conn.CreateModel())
                {
                    Model.ExchangeDeclare(x, "fanout", durable: false);

                    var q1 = ch1.QueueDeclare().QueueName;
                    var q2 = ch2.QueueDeclare().QueueName;
                    ch2.QueueBind(queue: q2, exchange: x, routingKey: string.Empty);

                    using (var latch = new ManualResetEvent(false))
                    {
                        var cons = new EventingBasicConsumer(ch1);
                        ch1.BasicConsume(q1, true, cons);
                        var c2 = new EventingBasicConsumer(ch2);
                        c2.Received += (object sender, BasicDeliverEventArgs e) =>
                        {
                            latch.Set();
                        };
                        ch2.BasicConsume(q2, true, c2);
                        // closing this channel must not affect ch2
                        ch1.Close();

                        ch2.BasicPublish(
                            exchange: x, 
                            basicProperties: null, 
                            body: encoding.GetBytes("msg"), 
                            routingKey: string.Empty);

                        Wait(latch);
                    }
                    ch2.Close();
                }
            }
        }

        private class ShutdownLatchConsumer : DefaultBasicConsumer
        {
            public ManualResetEvent Latch { get; private set; }
            public ManualResetEvent DuplicateLatch { get; private set; }

            public ShutdownLatchConsumer(ManualResetEvent latch, ManualResetEvent duplicateLatch)
            {
                this.Latch = latch;
                this.DuplicateLatch = duplicateLatch;
            }

            public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
            {
                // keep track of duplicates
                if (this.Latch.WaitOne(0)){
                    this.DuplicateLatch.Set();
                } else {
                    this.Latch.Set();
                }
            }
        }

        [Test]
        public void TestModelShutdownHandler()
        {
            var latch = new ManualResetEvent(false);
            var duplicateLatch = new ManualResetEvent(false);
            var q = this.Model.QueueDeclare().QueueName;
            var c = new ShutdownLatchConsumer(latch, duplicateLatch);

            this.Model.BasicConsume(queue: q, autoAck: true, consumer: c);
            this.Model.Close();
            Wait(latch, TimeSpan.FromSeconds(5));
            Assert.IsFalse(duplicateLatch.WaitOne(TimeSpan.FromSeconds(5)),
                           "event handler fired more than once");
        }
    }
}
