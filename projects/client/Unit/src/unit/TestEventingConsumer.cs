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
using System.Text;
using System.Threading;
using System.Diagnostics;

using RabbitMQ.Client.Events;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Unit {
    [TestFixture]
    public class TestEventingConsumer : IntegrationFixture {

        [Test]
        public void TestEventingConsumerRegistrationEvents()
        {
            string q = Model.QueueDeclare();

            var registeredLatch = new ManualResetEvent(false);
            object registeredSender = null;
            var unregisteredLatch = new ManualResetEvent(false);
            object unregisteredSender = null;

            EventingBasicConsumer ec = new EventingBasicConsumer(Model);
            ec.Registered += (s, args) =>
            {
                registeredSender = s;
                registeredLatch.Set();
            };

            ec.Unregistered += (s, args) =>
            {
                unregisteredSender = s;
                unregisteredLatch.Set();
            };

            string tag = Model.BasicConsume(q, false, ec);
            Wait(registeredLatch);

            Assert.IsNotNull(registeredSender);
            Assert.AreEqual(ec, registeredSender);
            Assert.AreEqual(Model, ((EventingBasicConsumer)registeredSender).Model);

            Model.BasicCancel(tag);
            Wait(unregisteredLatch);
            Assert.IsNotNull(unregisteredSender);
            Assert.AreEqual(ec, unregisteredSender);
            Assert.AreEqual(Model, ((EventingBasicConsumer)unregisteredSender).Model);
        }

        [Test]
        public void TestEventingConsumerDeliveryEvents()
        {
            string q = Model.QueueDeclare();
            using (AutoResetEvent resetEvent = new AutoResetEvent(false))
            {
                bool receivedInvoked = false;
                object receivedSender = null;

                EventingBasicConsumer ec = new EventingBasicConsumer(Model);
                ec.Received += (s, args) =>
                {
                    receivedInvoked = true;
                    receivedSender = s;
                    resetEvent.Set();
                };


                Model.BasicConsume(q, true, ec);


                for (int i = 0; i < 40000; i++)
                {
                    Model.BasicPublish("", q, null, encoding.GetBytes("msg"));
                    resetEvent.WaitOne();
                }


                Assert.IsTrue(receivedInvoked);
                Assert.IsNotNull(receivedSender);
                Assert.AreEqual(ec, receivedSender);
                Assert.AreEqual(Model, ((EventingBasicConsumer)receivedSender).Model);

                bool shutdownInvoked = false;
                object shutdownSender = null;

                ec.Shutdown += (s, args) =>
                {
                    shutdownInvoked = true;
                    shutdownSender = s;
                    resetEvent.Set();
                };

                Model.Close();
                resetEvent.WaitOne();

                Assert.IsTrue(shutdownInvoked);
                Assert.IsNotNull(shutdownSender);
                Assert.AreEqual(ec, shutdownSender);
                Assert.AreEqual(Model, ((EventingBasicConsumer)shutdownSender).Model);
            }
        }


        [Test]
        public void TestEventingConsumerDeliveryEventsNoAck()
        {
            int messages = 300000;
            int received = 0;
            string q = Model.QueueDeclare();

            var data = new byte[1024];
            for (int i = 0; i < messages; i++)
            {
                Model.BasicPublish(string.Empty, q, null, data);
            }

            using (ManualResetEvent reset = new ManualResetEvent(false))
            {
                EventingBasicConsumer ec = new EventingBasicConsumer(Model);

                ec.Received += (s, args) =>
                {
                    if (++received == messages) reset.Set();
                };

                Model.BasicConsume(q, true, ec);
                reset.WaitOne(TimeSpan.FromMinutes(2));
                
                Model.BasicCancel(ec.ConsumerTag);
                Assert.AreEqual(messages, received);
            }
        }
        [Test]
        public void TestEventingConsumerDeliveryEventsWithAck()
        {
            int messages = 300000;
            int received = 0;
            string q = Model.QueueDeclare();

            var data = new byte[1024];
            for (int i = 0; i < messages; i++)
            {
                Model.BasicPublish(string.Empty, q, null, data);
            }

            using (ManualResetEvent reset = new ManualResetEvent(false))
            {
                EventingBasicConsumer ec = new EventingBasicConsumer(Model);

                ec.Received += (s, args) =>
                {
                    Model.BasicAck(args.DeliveryTag, false);
                    if (++received == messages) reset.Set();
                };

                Model.BasicConsume(q, false, ec);
                reset.WaitOne(TimeSpan.FromMinutes(2));
                Model.BasicCancel(ec.ConsumerTag);
                Assert.AreEqual(messages, received);
            }
        }


        [Test]
        public void TestEventingConsumerDeliveryEventsNoAck1()
        {
            int messages = 300000;
            int received = 0;
            string q = Model.QueueDeclare();

            var data = new byte[1024];

            using (ManualResetEvent reset = new ManualResetEvent(false))
            {
                EventingBasicConsumer ec = new EventingBasicConsumer(Model);

                ec.Received += (s, args) =>
                {
                    if (++received == messages) reset.Set();
                };

                Model.BasicConsume(q, true, ec);

                for (int i = 0; i < messages; i++)
                {
                    Model.BasicPublish(string.Empty, q, null, data);
                }

                reset.WaitOne(TimeSpan.FromMinutes(2));

                Model.BasicCancel(ec.ConsumerTag);
                Assert.AreEqual(messages, received);
            }
        }
        [Test]
        public void TestEventingConsumerDeliveryEventsWithAck1()
        {
            int messages = 300000;
            int received = 0;
            string q = Model.QueueDeclare();

            var data = new byte[1024];

            using (ManualResetEvent reset = new ManualResetEvent(false))
            {
                EventingBasicConsumer ec = new EventingBasicConsumer(Model);

                ec.Received += (s, args) =>
                {
                    Model.BasicAck(args.DeliveryTag, false);
                    if (++received == messages) reset.Set();
                };

                Model.BasicConsume(q, false, ec);

                for (int i = 0; i < messages; i++)
                {
                    Model.BasicPublish(string.Empty, q, null, data);
                }

                reset.WaitOne(TimeSpan.FromMinutes(2));

                Model.BasicCancel(ec.ConsumerTag);

                Assert.AreEqual(messages, received);
            }
        }
        [Test]
        public void TestEventingConsumerDeliveryEventsWithAck1Short()
        {
            int messages = 50000;
            int received = 0;
            string q = Model.QueueDeclare();

            var data = new byte[1024];

            using (ManualResetEvent reset = new ManualResetEvent(false))
            {
                EventingBasicConsumer ec = new EventingBasicConsumer(Model);

                ec.Received += (s, args) =>
                {
                    Model.BasicAck(args.DeliveryTag, false);
                    if (++received == messages) reset.Set();
                };

                Model.BasicConsume(q, false, ec);

                for (int i = 0; i < messages; i++)
                {
                    Model.BasicPublish(string.Empty, q, null, data);
                }

                reset.WaitOne(TimeSpan.FromMinutes(2));

                Model.BasicCancel(ec.ConsumerTag);

                Assert.AreEqual(messages, received);
            }
        }

    }
}
