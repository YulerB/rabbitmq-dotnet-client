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
using System.Linq;
using RabbitMQ1.Client.Events;
using System.Threading.Tasks;
using RabbitMQ1.Client;
using System.Collections.Generic;

namespace rcu {
    [TestFixture]
    public class TestEventingConsumer1 : IntegrationFixture1 {

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
    public class IntegrationFixture1 : IntegrationFixtureBase1
    {
        protected IConnection Conn;
        protected IModel Model;

        protected Encoding encoding = new UTF8Encoding();
        public static TimeSpan RECOVERY_INTERVAL = TimeSpan.FromSeconds(2);

        [SetUp]
        public virtual void Init()
        {
            var connFactory = new ConnectionFactory();
            Conn = connFactory.CreateConnection();
            Model = Conn.CreateModel();
        }

        [TearDown]
        public void Dispose()
        {
            if (Model != null && Model.IsOpen)
            {
                Model?.Close();
            }
            if (Conn != null && Conn.IsOpen)
            {
                Conn?.Close();
            }

            ReleaseResources();
        }

        protected virtual void ReleaseResources()
        {
            Model?.Dispose();
            Conn?.Dispose();
        }

        //
        // Connections
        //

        protected RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection CreateAutorecoveringConnection()
        {
            return CreateAutorecoveringConnection(RECOVERY_INTERVAL);
        }

        protected RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection CreateAutorecoveringConnection(IList<string> hostnames)
        {
            return CreateAutorecoveringConnection(RECOVERY_INTERVAL, hostnames);
        }

        protected RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection CreateAutorecoveringConnection(TimeSpan interval)
        {
            var cf = new ConnectionFactory();
            cf.AutomaticRecoveryEnabled = true;
            cf.NetworkRecoveryInterval = interval;
            return (RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection)cf.CreateConnection();
        }

        protected RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection CreateAutorecoveringConnection(TimeSpan interval, IList<string> hostnames)
        {
            var cf = new ConnectionFactory();
            cf.AutomaticRecoveryEnabled = true;
            // tests that use this helper will likely list unreachable hosts,
            // make sure we time out quickly on those
            cf.RequestedConnectionTimeout = 1000;
            cf.NetworkRecoveryInterval = interval;
            return (RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection)cf.CreateConnection(hostnames);
        }

        protected RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection CreateAutorecoveringConnection(IList<AmqpTcpEndpoint> endpoints)
        {
            var cf = new ConnectionFactory();
            cf.AutomaticRecoveryEnabled = true;
            // tests that use this helper will likely list unreachable hosts,
            // make sure we time out quickly on those
            cf.RequestedConnectionTimeout = 1000;
            cf.NetworkRecoveryInterval = RECOVERY_INTERVAL;
            return (RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection)cf.CreateConnection(endpoints);
        }

        protected RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection CreateAutorecoveringConnectionWithTopologyRecoveryDisabled()
        {
            var cf = new ConnectionFactory();
            cf.AutomaticRecoveryEnabled = true;
            cf.TopologyRecoveryEnabled = false;
            cf.NetworkRecoveryInterval = RECOVERY_INTERVAL;
            return (RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection)cf.CreateConnection();
        }

        protected IConnection CreateNonRecoveringConnection()
        {
            var cf = new ConnectionFactory();
            cf.AutomaticRecoveryEnabled = false;
            cf.TopologyRecoveryEnabled = false;
            return cf.CreateConnection();
        }

        //
        // Channels
        //

        protected void WithTemporaryAutorecoveringConnection(Action<RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection> action)
        {
            var factory = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true
            };

            var connection = (RabbitMQ1.Client.Framing.Impl.AutorecoveringConnection)factory.CreateConnection();
            try
            {
                action(connection);
            }
            finally
            {
                connection.Abort();
            }
        }

        protected void WithTemporaryModel(IConnection connection, Action<IModel> action)
        {
            using (IModel model = connection.CreateModel())
            {

                try
                {
                    action(model);
                }
                finally
                {
                    model.Abort();
                }
            }
        }

        protected void WithTemporaryModel(Action<IModel> action)
        {
            using (IModel model = Conn.CreateModel())
            {

                try
                {
                    action(model);
                }
                finally
                {
                    model.Abort();
                }
            }
        }

        protected void WithClosedModel(Action<IModel> action)
        {
            using (IModel model = Conn.CreateModel())
            {
                model.Close();

                action(model);
            }
        }

        protected bool WaitForConfirms(IModel m)
        {
            return m.WaitForConfirms(TimeSpan.FromSeconds(4));
        }

        //
        // Exchanges
        //

        protected string GenerateExchangeName()
        {
            return "exchange" + Guid.NewGuid().ToString();
        }

        protected byte[] RandomMessageBody()
        {
            return encoding.GetBytes(Guid.NewGuid().ToString());
        }

        protected string DeclareNonDurableExchange(IModel m, string x)
        {
            m.ExchangeDeclare(x, ExchangeType.Fanout, false);
            return x;
        }

        protected string DeclareNonDurableExchangeNoWait(IModel m, string x)
        {
            m.ExchangeDeclareNoWait(x, ExchangeType.Fanout, false, false, null);
            return x;
        }

        //
        // Queues
        //

        protected string GenerateQueueName()
        {
            return "queue" + Guid.NewGuid().ToString();
        }

        protected void WithTemporaryQueue(Action<IModel, string> action)
        {
            WithTemporaryQueue(Model, action);
        }

        protected void WithTemporaryNonExclusiveQueue(Action<IModel, string> action)
        {
            WithTemporaryNonExclusiveQueue(Model, action);
        }

        protected void WithTemporaryQueue(IModel model, Action<IModel, string> action)
        {
            WithTemporaryQueue(model, action, GenerateQueueName());
        }

        protected void WithTemporaryNonExclusiveQueue(IModel model, Action<IModel, string> action)
        {
            WithTemporaryNonExclusiveQueue(model, action, GenerateQueueName());
        }

        protected void WithTemporaryQueue(Action<IModel, string> action, string q)
        {
            WithTemporaryQueue(Model, action, q);
        }

        protected void WithTemporaryQueue(IModel model, Action<IModel, string> action, string queue)
        {
            try
            {
                model.QueueDeclare(queue, false, true, false, null);
                action(model, queue);
            }
            finally
            {
                WithTemporaryModel(x => x.QueueDelete(queue));
            }
        }

        protected void WithTemporaryNonExclusiveQueue(IModel model, Action<IModel, string> action, string queue)
        {
            try
            {
                model.QueueDeclare(queue, false, false, false, null);
                action(model, queue);
            }
            finally
            {
                WithTemporaryModel(tm => tm.QueueDelete(queue));
            }
        }

        protected void WithTemporaryQueueNoWait(IModel model, Action<IModel, string> action, string queue)
        {
            try
            {
                model.QueueDeclareNoWait(queue, false, true, false, null);
                action(model, queue);
            }
            finally
            {
                WithTemporaryModel(x => x.QueueDelete(queue));
            }
        }

        protected void EnsureNotEmpty(string q)
        {
            EnsureNotEmpty(q, "msg");
        }

        protected void EnsureNotEmpty(string q, string body)
        {
            WithTemporaryModel(x => x.BasicPublish("", q, null, encoding.GetBytes(body)));
        }

        protected void WithNonEmptyQueue(Action<IModel, string> action)
        {
            WithNonEmptyQueue(action, "msg");
        }

        protected void WithNonEmptyQueue(Action<IModel, string> action, string msg)
        {
            WithTemporaryNonExclusiveQueue((m, q) =>
            {
                EnsureNotEmpty(q, msg);
                action(m, q);
            });
        }

        protected void WithEmptyQueue(Action<IModel, string> action)
        {
            WithTemporaryNonExclusiveQueue((model, queue) =>
            {
                model.QueuePurge(queue);
                action(model, queue);
            });
        }

        protected void AssertMessageCount(string q, int count)
        {
            WithTemporaryModel((m) => {
                QueueDeclareOk ok = m.QueueDeclarePassive(q);
                Assert.AreEqual(count, ok.MessageCount);
            });
        }

        protected void AssertConsumerCount(string q, int count)
        {
            WithTemporaryModel((m) => {
                QueueDeclareOk ok = m.QueueDeclarePassive(q);
                Assert.AreEqual(count, ok.ConsumerCount);
            });
        }

        protected void AssertConsumerCount(IModel m, string q, int count)
        {
            QueueDeclareOk ok = m.QueueDeclarePassive(q);
            Assert.AreEqual(count, ok.ConsumerCount);
        }

        //
        // Shutdown
        //

        protected void AssertShutdownError(ShutdownEventArgs args, int code)
        {
            Assert.AreEqual(args.ReplyCode, code);
        }

        protected void AssertPreconditionFailed(ShutdownEventArgs args)
        {
            AssertShutdownError(args, 406);
        }

        protected bool InitiatedByPeerOrLibrary(ShutdownEventArgs evt)
        {
            return !(evt.Initiator == ShutdownInitiator.Application);
        }

        protected void Block()
        {
            using (var p = ExecRabbitMQCtl("set_vm_memory_high_watermark 0.000000001")) { }
            // give rabbitmqctl some time to do its job
            Thread.Sleep(1200);
            Publish(Conn);
        }

        protected void Unblock()
        {
            using (var p = ExecRabbitMQCtl("set_vm_memory_high_watermark 0.4")) { }
        }

        protected void Publish(IConnection conn)
        {
            using (IModel ch = conn.CreateModel())
            {
                ch.BasicPublish("amq.fanout", "", null, encoding.GetBytes("message"));
            }
        }

        //
        // Connection Closure
        //

        public class ConnectionInfo
        {
            public string Pid
            {
                get; set;
            }

            public uint PeerPort
            {
                get; set;
            }

            public ConnectionInfo(string pid, uint peerPort)
            {
                Pid = pid;
                PeerPort = peerPort;
            }

            public override string ToString()
            {
                return "pid = " + Pid + ", peer port: " + PeerPort;
            }
        }

        protected List<ConnectionInfo> ListConnections()
        {
            String stdout = null;
            using (Process proc = ExecRabbitMQCtl("list_connections -q pid peer_port"))
            {
                stdout = proc.StandardOutput.ReadToEnd();
            }

            try
            {
                // {Environment.NewLine} is not sufficient
                string[] splitOn = new string[] { "\r\n", "\n" };
                string[] lines = stdout.Split(splitOn, StringSplitOptions.RemoveEmptyEntries);
                // line: <rabbit@mercurio.1.11491.0>	58713
                return lines.Select(s =>
                {
                    var columns = s.Split('\t');
                    Debug.Assert(!string.IsNullOrEmpty(columns[0]), "columns[0] is null or empty!");
                    Debug.Assert(!string.IsNullOrEmpty(columns[1]), "columns[1] is null or empty!");
                    return new ConnectionInfo(columns[0], Convert.ToUInt32(columns[1].Trim()));
                }).ToList();
            }
            catch (Exception)
            {
                Console.WriteLine("Bad response from rabbitmqctl list_connections -q pid peer_port:" + Environment.NewLine + stdout);
                throw;
            }
        }

        protected void CloseConnection(IConnection conn)
        {
            var ci = ListConnections().First(x => conn.LocalPort == x.PeerPort);
            CloseConnection(ci.Pid);
        }

        protected void CloseAllConnections()
        {
            var cs = ListConnections();
            foreach (var c in cs)
            {
                CloseConnection(c.Pid);
            }
        }

        protected void CloseConnection(string pid)
        {
            using (var p = ExecRabbitMQCtl("close_connection \"" +
                            pid +
                           "\" \"Closed via rabbitmqctl\"")) { }
        }

        protected void RestartRabbitMQ()
        {
            StopRabbitMQ();
            Thread.Sleep(500);
            StartRabbitMQ();
        }


        //
        // Concurrency and Coordination
        //

        protected void Wait(ManualResetEvent latch)
        {
            Assert.IsTrue(latch.WaitOne(TimeSpan.FromSeconds(10)), "waiting on a latch timed out");
        }

        protected void Wait(ManualResetEvent latch, TimeSpan timeSpan)
        {
            Assert.IsTrue(latch.WaitOne(timeSpan), "waiting on a latch timed out");
        }

        //
        // TLS
        //

        public static string CertificatesDirectory()
        {
            return Environment.GetEnvironmentVariable("SSL_CERTS_DIR");
        }
    }
    public abstract class IntegrationFixtureBase1
    {
        protected void StopRabbitMQ()
        {
            using (var p = ExecRabbitMQCtl("stop_app")) { }
        }

        protected void StartRabbitMQ()
        {
            using (var p = ExecRabbitMQCtl("start_app")) { }
        }
        protected Process ExecRabbitMQCtl(string args)
        {
            // Allow the path to the rabbitmqctl.bat to be set per machine
            var envVariable = Environment.GetEnvironmentVariable("RABBITMQ_RABBITMQCTL_PATH");

            string rabbitmqctlPath;
            if (envVariable != null)
            {
                rabbitmqctlPath = envVariable;
            }
            else if (IsRunningOnMonoOrDotNetCore())
            {
                rabbitmqctlPath = "../../../../../../../rabbit/scripts/rabbitmqctl";
            }
            else
            {
                rabbitmqctlPath = @"..\..\..\..\..\..\..\rabbit\scripts\rabbitmqctl.bat";
            }

            return ExecCommand(rabbitmqctlPath, args);
        }

        protected Process ExecCommand(string command)
        {
            return ExecCommand(command, string.Empty);
        }

        protected Process ExecCommand(string command, string args)
        {
            return ExecCommand(command, args, null);
        }

        protected Process ExecCommand(string ctl, string args, string changeDirTo)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            if (changeDirTo != null)
            {
                proc.StartInfo.WorkingDirectory = changeDirTo;
            }

            string cmd;
            if (IsRunningOnMonoOrDotNetCore())
            {
                cmd = ctl;
            }
            else
            {
                cmd = "cmd.exe";
                args = "/c \"\"" + ctl + "\" " + args + "\"";
            }

            try
            {
                proc.StartInfo.FileName = cmd;
                proc.StartInfo.Arguments = args;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                String stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (stderr.Length > 0 || proc.ExitCode > 0)
                {
                    String stdout = proc.StandardOutput.ReadToEnd();
                    ReportExecFailure(cmd, args, stderr + "\n" + stdout);
                }

                return proc;
            }
            catch (Exception e)
            {
                ReportExecFailure(cmd, args, e.Message);
                throw e;
            }
        }
        public static bool IsRunningOnMonoOrDotNetCore()
        {
#if CORECLR
            return true;
#else
            return Type.GetType("Mono.Runtime") != null;
#endif
        }

        protected void ReportExecFailure(String cmd, String args, String msg)
        {
            Console.WriteLine("Failure while running " + cmd + " " + args + ":\n" + msg);
        }


    }
}
