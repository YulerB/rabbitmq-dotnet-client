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

using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#if (NETFX_CORE)
using Trace = System.Diagnostics.Debug;
#endif

namespace RabbitMQ.Client.Impl
{
    public abstract partial class ModelBase : IFullModel, IRecoverable
    {
        private const int ZERO = 0;
        private const ulong ULZERO = 0UL;
        private readonly Dictionary<string, IBasicConsumer> m_consumers = new Dictionary<string, IBasicConsumer>();

        ///<summary>Only used to kick-start a connection open
        ///sequence. See <see cref="Connection.Open"/> </summary>
        private TaskCompletionSource<ConnectionStart> m_connectionStartCell = null;

        public void CreateConnectionStart()
        {
            m_connectionStartCell = new TaskCompletionSource<ConnectionStart>();
        }
        public ConnectionStart WaitForConnection()
        {
            m_connectionStartCell.Task.Wait();
            var cs = m_connectionStartCell.Task.Result;
            m_connectionStartCell = null;

            return cs;
        }
        private TimeSpan m_handshakeContinuationTimeout = TimeSpan.FromSeconds(10);
        private TimeSpan m_continuationTimeout = TimeSpan.FromSeconds(20);

        private RpcContinuationQueue m_continuationQueue = new RpcContinuationQueue();
        private ManualResetEvent m_flowControlBlock = new ManualResetEvent(true);

        private readonly object m_eventLock = new object();
        private readonly object m_shutdownLock = new object();
        private readonly object _rpcLock = new object();

        private readonly SynchronizedList<ulong> m_unconfirmedSet = new SynchronizedList<ulong>();

        private EventHandler<BasicAckEventArgs> m_basicAck;
        private EventHandler<BasicNackEventArgs> m_basicNack;
        private EventHandler<EventArgs> m_basicRecoverOk;
        private EventHandler<BasicReturnEventArgs> m_basicReturn;
        private EventHandler<CallbackExceptionEventArgs> m_callbackException;
        private EventHandler<FlowControlEventArgs> m_flowControl;
        private EventHandler<ShutdownEventArgs> m_modelShutdown;

        private bool m_onlyAcksReceived = true;

        private EventHandler<EventArgs> m_recovery;

        public IConsumerDispatcher ConsumerDispatcher { get; private set; }

        public ModelBase(ISession session)
            : this(session, session.Connection.ConsumerWorkService)
        { }

        public ModelBase(ISession session, ConsumerWorkService workService)
        {
            if (workService is AsyncConsumerWorkService asyncConsumerWorkService)
            {
                ConsumerDispatcher = new AsyncConsumerDispatcher(this, asyncConsumerWorkService);
            }
            else
            {
                ConsumerDispatcher = new ConcurrentConsumerDispatcher(this, workService);
            }

            Initialise(session);
        }


        protected void Initialise(ISession session)
        {
            CloseReason = null;
            NextPublishSeqNo = ULZERO;
            Session = session;
            Session.CommandReceived = HandleCommand;
            Session.SessionShutdown += OnSessionShutdown;
        }

        public TimeSpan HandshakeContinuationTimeout
        {
            get { return m_handshakeContinuationTimeout; }
            set { m_handshakeContinuationTimeout = value; }
        }

        public TimeSpan ContinuationTimeout
        {
            get { return m_continuationTimeout; }
            set { m_continuationTimeout = value; }
        }

        public event EventHandler<BasicAckEventArgs> BasicAcks
        {
            add
            {
                lock (m_eventLock)
                {
                    m_basicAck += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_basicAck -= value;
                }
            }
        }

        public event EventHandler<BasicNackEventArgs> BasicNacks
        {
            add
            {
                lock (m_eventLock)
                {
                    m_basicNack += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_basicNack -= value;
                }
            }
        }

        public event EventHandler<EventArgs> BasicRecoverOk
        {
            add
            {
                lock (m_eventLock)
                {
                    m_basicRecoverOk += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_basicRecoverOk -= value;
                }
            }
        }

        public event EventHandler<BasicReturnEventArgs> BasicReturn
        {
            add
            {
                lock (m_eventLock)
                {
                    m_basicReturn += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_basicReturn -= value;
                }
            }
        }

        public event EventHandler<CallbackExceptionEventArgs> CallbackException
        {
            add
            {
                lock (m_eventLock)
                {
                    m_callbackException += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_callbackException -= value;
                }
            }
        }

        public event EventHandler<FlowControlEventArgs> FlowControl
        {
            add
            {
                lock (m_eventLock)
                {
                    m_flowControl += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_flowControl -= value;
                }
            }
        }

        public event EventHandler<ShutdownEventArgs> ModelShutdown
        {
            add
            {
                bool ok = false;
                if (CloseReason == null)
                {
                    lock (m_shutdownLock)
                    {
                        if (CloseReason == null)
                        {
                            m_modelShutdown += value;
                            ok = true;
                        }
                    }
                }

                if (!ok)
                {
                    value(this, CloseReason);
                }
            }
            remove
            {
                lock (m_shutdownLock)
                {
                    m_modelShutdown -= value;
                }
            }
        }

        public event EventHandler<EventArgs> Recovery
        {
            add
            {
                lock (m_eventLock)
                {
                    m_recovery += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_recovery -= value;
                }
            }
        }

        public ushort ChannelNumber
        {
            get { return Session.ChannelNumber; }
        }

        public ShutdownEventArgs CloseReason { get; private set; }

        public IBasicConsumer DefaultConsumer { get; set; }

        public bool IsClosed
        {
            get { return !IsOpen; }
        }

        public bool IsOpen
        {
            get { return CloseReason == null; }
        }

        public ulong NextPublishSeqNo { get; private set; }

        public ISession Session { get; private set; }

        public void Close(ushort replyCode, string replyText, bool abort)
        {
            Close(new ShutdownEventArgs(ShutdownInitiator.Application,
                replyCode, replyText),
                abort);
        }

        public void Close(ShutdownEventArgs reason, bool abort)
        {
            var k = new ShutdownContinuation();
            ModelShutdown += k.OnConnectionShutdown;

            try
            {
                ConsumerDispatcher.Quiesce();
                if (SetCloseReason(reason))
                {
                    _Private_ChannelClose(reason.ReplyCode, reason.ReplyText, 0, 0);
                }
                k.Wait(TimeSpan.FromMilliseconds(10000));
                ConsumerDispatcher.Shutdown(this);
            }
            catch (AlreadyClosedException)
            {
                if (!abort)
                {
                    throw;
                }
            }
            catch (IOException)
            {
                if (!abort)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                if (!abort)
                {
                    throw;
                }
            }
        }

        internal string ConnectionOpen(string virtualHost,
            string capabilities,
            bool insist)
        {
            var k = new ConnectionOpenContinuation();
            lock (_rpcLock)
            {
                Enqueue(k);
                try
                {
                    _Private_ConnectionOpen(virtualHost, capabilities, insist);
                }
                catch (AlreadyClosedException)
                {
                    // let continuation throw OperationInterruptedException,
                    // which is a much more suitable exception before connection
                    // negotiation finishes
                }
                k.GetReply(HandshakeContinuationTimeout);
            }

            return k.m_knownHosts;
        }

        internal ConnectionSecureOrTune ConnectionSecureOk(string response)
        {
            var k = new ConnectionStartRpcContinuation();
            lock(_rpcLock)
            {
                Enqueue(k);
                try
                {
                    _Private_ConnectionSecureOk(response);
                }
                catch (AlreadyClosedException)
                {
                    // let continuation throw OperationInterruptedException,
                    // which is a much more suitable exception before connection
                    // negotiation finishes
                }
                k.GetReply(HandshakeContinuationTimeout);
            }
            return k.m_result;
        }

        internal ConnectionSecureOrTune ConnectionStartOk(ConnectionStartOk args)
        {
            var k = new ConnectionStartRpcContinuation();
            lock(_rpcLock)
            {
                Enqueue(k);
                try
                {
                    _Private_ConnectionStartOk(args);
                }
                catch (AlreadyClosedException)
                {
                    // let continuation throw OperationInterruptedException,
                    // which is a much more suitable exception before connection
                    // negotiation finishes
                }
                k.GetReply(HandshakeContinuationTimeout);
            }
            return k.m_result;
        }

        public abstract bool DispatchAsynchronous(AssembledCommandBase<FrameBuilder> cmd);

        public void Enqueue(IRpcContinuation k)
        {
            bool ok = false;
            if (CloseReason == null)
            {
                lock (m_shutdownLock)
                {
                    if (CloseReason == null)
                    {
                        m_continuationQueue.Enqueue(k);
                        ok = true;
                    }
                }
            }
            if (!ok)
            {
                k.HandleModelShutdown(CloseReason);
            }
        }

        public void FinishClose()
        {
            if (CloseReason != null)
            {
                Session.Close(CloseReason);
            }
            if (m_connectionStartCell != null)
            {
                m_connectionStartCell.SetResult(null);
            }
        }

        public void HandleCommand(ISession session, AssembledCommandBase<FrameBuilder> cmd)
        {
            if (!DispatchAsynchronous(cmd))// Was asynchronous. Already processed. No need to process further.
                m_continuationQueue.Next().HandleCommand(cmd);
        }

        public IMethod ModelRpc<T>(T method) 
            where T: IMethod
        {
            var k = new SimpleBlockingRpcContinuation();
            lock (_rpcLock)
            {
                TransmitAndEnqueue(new SendCommand<T>(method), k);
                return k.GetReply(this.ContinuationTimeout).Method;
            }
        }

        public void ModelSend<T>(T method, RabbitMQ.Client.Impl.BasicProperties header, byte[] body) where T: IMethod
        {
            if (method.HasContent)
            {
                m_flowControlBlock.WaitOne();
            }
            Session.Transmit(new SendCommand<T>(method, header, body));
        }

        public void ModelSend<T>(T method) where T : IMethod
        {
            if (method.HasContent)
            {
                m_flowControlBlock.WaitOne();
            }
            Session.Transmit(new SendCommand<T>(method));
        }

        public virtual void OnBasicAck(BasicAckEventArgs args)
        {
            EventHandler<BasicAckEventArgs> handler;
            lock (m_eventLock)
            {
                handler = m_basicAck;
            }
            if (handler != null)
            {
                foreach (EventHandler<BasicAckEventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, args);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e, "OnBasicAck"));
                    }
                }
            }

            HandleAckNack(args.DeliveryTag, args.Multiple, false);
        }

        public virtual void OnBasicNack(BasicNackEventArgs args)
        {
            EventHandler<BasicNackEventArgs> handler;
            lock (m_eventLock)
            {
                handler = m_basicNack;
            }
            if (handler != null)
            {
                foreach (EventHandler<BasicNackEventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, args);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e, "OnBasicNack"));
                    }
                }
            }

            HandleAckNack(args.DeliveryTag, (args.Settings & BasicNackFlags.Multiple) == BasicNackFlags.Multiple, true);
        }

        public virtual void OnBasicRecoverOk(EventArgs args)
        {
            EventHandler<EventArgs> handler;
            lock (m_eventLock)
            {
                handler = m_basicRecoverOk;
            }
            if (handler != null)
            {
                foreach (EventHandler<EventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, args);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e, "OnBasicRecover"));
                    }
                }
            }
        }

        public virtual void OnBasicReturn(BasicReturnEventArgs args)
        {
            EventHandler<BasicReturnEventArgs> handler;
            lock (m_eventLock)
            {
                handler = m_basicReturn;
            }
            if (handler != null)
            {
                foreach (EventHandler<BasicReturnEventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, args);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e, "OnBasicReturn"));
                    }
                }
            }
        }

        public virtual void OnCallbackException(CallbackExceptionEventArgs args)
        {
            EventHandler<CallbackExceptionEventArgs> handler;
            lock (m_eventLock)
            {
                handler = m_callbackException;
            }
            if (handler != null)
            {
                foreach (EventHandler<CallbackExceptionEventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, args);
                    }
                    catch
                    {
                        // Exception in
                        // Callback-exception-handler. That was the
                        // app's last chance. Swallow the exception.
                        // FIXME: proper logging
                    }
                }
            }
        }

        public virtual void OnFlowControl(FlowControlEventArgs args)
        {
            EventHandler<FlowControlEventArgs> handler;
            lock (m_eventLock)
            {
                handler = m_flowControl;
            }
            if (handler != null)
            {
                foreach (EventHandler<FlowControlEventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, args);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e, "OnFlowControl"));
                    }
                }
            }
        }

        ///<summary>Broadcasts notification of the final shutdown of the model.</summary>
        ///<remarks>
        ///<para>
        ///Do not call anywhere other than at the end of OnSessionShutdown.
        ///</para>
        ///<para>
        ///Must not be called when m_closeReason == null, because
        ///otherwise there's a window when a new continuation could be
        ///being enqueued at the same time as we're broadcasting the
        ///shutdown event. See the definition of Enqueue() above.
        ///</para>
        ///</remarks>
        public virtual void OnModelShutdown(ShutdownEventArgs reason)
        {
            m_continuationQueue.HandleModelShutdown(reason);
            EventHandler<ShutdownEventArgs> handler;
            lock (m_shutdownLock)
            {
                handler = m_modelShutdown;
                m_modelShutdown = null;
            }
            if (handler != null)
            {
                foreach (EventHandler<ShutdownEventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, reason);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e, "OnModelShutdown"));
                    }
                }
            }
            lock (m_unconfirmedSet.SyncRoot)
                Monitor.Pulse(m_unconfirmedSet.SyncRoot);
            m_flowControlBlock.Set();
        }

        public void OnSessionShutdown(object sender, ShutdownEventArgs reason)
        {
            this.ConsumerDispatcher.Quiesce();
            SetCloseReason(reason);
            OnModelShutdown(reason);
            BroadcastShutdownToConsumers(m_consumers, reason);
            this.ConsumerDispatcher.Shutdown(this);
        }

        protected void BroadcastShutdownToConsumers(Dictionary<string, IBasicConsumer> cs, ShutdownEventArgs reason)
        {
            foreach (var c in cs)
            {
                this.ConsumerDispatcher.HandleModelShutdown(c.Value, reason);
            }
        }

        public bool SetCloseReason(ShutdownEventArgs reason)
        {
            if (CloseReason == null)
            {
                lock (m_shutdownLock)
                {
                    if (CloseReason == null)
                    {
                        CloseReason = reason;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
                return false;
        }

        public sealed override string ToString()
        {
            return Session.ToString();
        }

        public void TransmitAndEnqueue<T>(SendCommand<T> cmd, IRpcContinuation k) where T: IMethod
        {
            Enqueue(k);
            Session.Transmit(cmd);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                try
                {
                    Abort();
                }
                finally
                {
                    m_basicAck = null;
                    m_basicNack = null;
                    m_basicRecoverOk = null;
                    m_basicReturn = null;
                    m_callbackException = null;
                    m_flowControl = null;
                    m_modelShutdown = null;
                    m_recovery = null;
                }
            }

            // dispose unmanaged resources
        }

        public abstract void ConnectionTuneOk(ConnectionTuneOk args);

        public void HandleBasicAck(BasicAckEventArgs args)
        {
            OnBasicAck(args);
        }

        public void HandleBasicCancel(string consumerTag, bool nowait)
        {
            IBasicConsumer consumer;
            lock (m_consumers)
            {
                consumer = m_consumers[consumerTag];
                m_consumers.Remove(consumerTag);
            }
            if (consumer == null)
            {
                consumer = DefaultConsumer;
            }
            ConsumerDispatcher.HandleBasicCancel(consumer, consumerTag);
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            var k =
                (BasicConsumerRpcContinuation)m_continuationQueue.Next();
            /*
                        Trace.Assert(k.m_consumerTag == consumerTag, string.Format(
                            "Consumer tag mismatch during cancel: {0} != {1}",
                            k.m_consumerTag,
                            consumerTag
                            ));
            */
            lock (m_consumers)
            {
                k.Consumer = m_consumers[consumerTag];
                m_consumers.Remove(consumerTag);
            }
            ConsumerDispatcher.HandleBasicCancelOk(k.Consumer, consumerTag);
            k.HandleCommand(null); // release the continuation.
        }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            var k =
                (BasicConsumerRpcContinuation)m_continuationQueue.Next();
            k.ConsumerTag = consumerTag;
            lock (m_consumers)
            {
                m_consumers[consumerTag] = k.Consumer;
            }
            ConsumerDispatcher.HandleBasicConsumeOk(k.Consumer, consumerTag);
            k.HandleCommand(null); // release the continuation.
        }

        public virtual void HandleBasicDeliver(BasicDeliverEventArgs args)
        {
            IBasicConsumer consumer;
            lock (m_consumers)
            {
                consumer = m_consumers[args.ConsumerTag];
            }
            if (consumer == null)
            {
                if (DefaultConsumer == null)
                {
                    throw new InvalidOperationException("Unsolicited delivery -" +
                                                        " see IModel.DefaultConsumer to handle this" +
                                                        " case.");
                }
                else
                {
                    consumer = DefaultConsumer;
                }
            }

            ConsumerDispatcher.HandleBasicDeliver(consumer,
                    args);
        }

        public void HandleBasicGetEmpty()
        {
            var k = (BasicGetRpcContinuation)m_continuationQueue.Next();
            k.m_result = null;
            k.HandleCommand(null); // release the continuation.
        }

        public virtual void HandleBasicGetOk(BasicGetResult args)
        {
            var k = (BasicGetRpcContinuation)m_continuationQueue.Next();
            k.m_result = args;
            k.HandleCommand(null); // release the continuation.
        }

        public void HandleBasicNack(BasicNackEventArgs args)
        {
            OnBasicNack(args);
        }

        public void HandleBasicRecoverOk()
        {
            var k = m_continuationQueue.Next();
            OnBasicRecoverOk(new EventArgs());
            k.HandleCommand(null);
        }

        public void HandleBasicReturn(ushort replyCode,
            string replyText,
            string exchange,
            string routingKey,
            IBasicProperties basicProperties,
            byte[] body)
        {
            OnBasicReturn(new BasicReturnEventArgs
            {
                ReplyCode = replyCode,
                ReplyText = replyText,
                Exchange = exchange,
                RoutingKey = routingKey,
                BasicProperties = basicProperties,
                Body = body
            });
        }

        public void HandleChannelClose(ushort replyCode,
            string replyText,
            ushort classId,
            ushort methodId)
        {
            SetCloseReason(new ShutdownEventArgs(ShutdownInitiator.Peer,
                replyCode,
                replyText,
                classId,
                methodId));

            Session.Close(CloseReason, false);
            try
            {
                _Private_ChannelCloseOk();
            }
            finally
            {
                Session.Notify();
            }
        }

        public void HandleChannelCloseOk()
        {
            FinishClose();
        }

        public void HandleChannelFlow(bool active)
        {
            if (active) m_flowControlBlock.Set(); else m_flowControlBlock.Reset();
            _Private_ChannelFlowOk(active);
            OnFlowControl(new FlowControlEventArgs(active));
        }

        public void HandleConnectionBlocked(string reason)
        {
            var cb = ((Connection)Session.Connection);

            cb.HandleConnectionBlocked(reason);
        }

        public void HandleConnectionClose(ushort replyCode,
            string replyText,
            ushort classId,
            ushort methodId)
        {
            var reason = new ShutdownEventArgs(ShutdownInitiator.Peer,
                replyCode,
                replyText,
                classId,
                methodId);
            try
            {
                _Private_ConnectionCloseOk();
                 ((Connection)Session.Connection).InternalClose(reason);
               SetCloseReason((Session.Connection).CloseReason);
            }
            catch (IOException)
            {
                // Ignored. We're only trying to be polite by sending
                // the close-ok, after all.
            }
            catch (AlreadyClosedException)
            {
                // Ignored. We're only trying to be polite by sending
                // the close-ok, after all.
            }
        }

        public void HandleConnectionOpenOk(string knownHosts)
        {
            var k = (ConnectionOpenContinuation)m_continuationQueue.Next();
            k.m_redirect = false;
            k.m_host = null;
            k.m_knownHosts = knownHosts;
            k.HandleCommand(null); // release the continuation.
        }

        public void HandleConnectionSecure(string challenge)
        {
            var k = (ConnectionStartRpcContinuation)m_continuationQueue.Next();
            k.m_result = new ConnectionSecureOrTune(challenge);
            k.HandleCommand(null); // release the continuation.
        }

        public void HandleConnectionStart(ConnectionStart connectionStart)
        {
            if (m_connectionStartCell == null)
            {
                var reason =
                    new ShutdownEventArgs(ShutdownInitiator.Library,
                        Constants.CommandInvalid,
                        "Unexpected Connection.Start");
                ((Connection)Session.Connection).Close(reason);
            }
            m_connectionStartCell.SetResult(connectionStart);
        }

        ///<summary>Handle incoming Connection.Tune
        ///methods.</summary>
        public void HandleConnectionTune(ConnectionTuneDetails args)
        {
            var k = (ConnectionStartRpcContinuation)m_continuationQueue.Next();
            k.m_result = new ConnectionSecureOrTune(args);
            k.HandleCommand(null); // release the continuation.
        }

        public void HandleConnectionUnblocked()
        {
            var cb = ((Connection)Session.Connection);

            cb.HandleConnectionUnblocked();
        }

        public void HandleQueueDeclareOk(string queue,
            uint messageCount,
            uint consumerCount)
        {
            var k = (QueueDeclareRpcContinuation)m_continuationQueue.Next();
            k.m_result = new QueueDeclareOk(queue, messageCount, consumerCount);
            k.HandleCommand(null); // release the continuation.
        }

        public abstract void _Private_BasicCancel(string consumerTag,
            bool nowait);

        public abstract void _Private_BasicConsume(BasicConsume args);

        public abstract void _Private_BasicGet(string queue,
            bool autoAck);

        public abstract void _Private_BasicPublish(BasicPublishFull args);
        //string exchange,
        //    string routingKey,
        //    bool mandatory,
        //    IBasicProperties basicProperties,
        //    byte[] body);

        public abstract void _Private_BasicRecover(bool requeue);

        public abstract void _Private_ChannelClose(ushort replyCode,
            string replyText,
            ushort classId,
            ushort methodId);

        public abstract void _Private_ChannelCloseOk();

        public abstract void _Private_ChannelFlowOk(bool active);

        public abstract void _Private_ChannelOpen(string outOfBand);

        public abstract void _Private_ConfirmSelect(bool nowait);

        public abstract void _Private_ConnectionClose(ushort replyCode,
            string replyText,
            ushort classId,
            ushort methodId);

        public abstract void _Private_ConnectionCloseOk();

        public abstract void _Private_ConnectionOpen(string virtualHost,
            string capabilities,
            bool insist);

        public abstract void _Private_ConnectionSecureOk(string response);

        public abstract void _Private_ConnectionStartOk(ConnectionStartOk args);

        public abstract void _Private_ExchangeBind(ExchangeBind args);

        public abstract void _Private_ExchangeDeclare(string exchange,
            string type,
             ExchangeDeclareFlags flag,
            Dictionary<string, object> arguments);

        public abstract void _Private_ExchangeDelete(string exchange,
            ExchangeDeleteFlags flag);

        public abstract void _Private_ExchangeUnbind(ExchangeUnbind args);

        public abstract void _Private_QueueBind(string queue,
            string exchange,
            string routingKey,
            bool nowait,
            Dictionary<string, object> arguments);

        public abstract void _Private_QueueDeclare(string queue,
            QueueDeclareFlags flag,
            Dictionary<string, object> arguments);

        public abstract uint _Private_QueueDelete(string queue,
            QueueDeleteFlags flag);

        public abstract uint _Private_QueuePurge(string queue,
            bool nowait);

        public void Abort()
        {
            Abort(Constants.ReplySuccess, "Goodbye");
        }

        public void Abort(ushort replyCode, string replyText)
        {
            Close(replyCode, replyText, true);
        }

        public abstract void BasicAck(ulong deliveryTag, bool multiple);

        public void BasicCancel(string consumerTag)
        {
            var k = new BasicConsumerRpcContinuation (consumerTag);

            lock(_rpcLock)
            {
                Enqueue(k);
                _Private_BasicCancel(consumerTag, false);
                k.GetReply(this.ContinuationTimeout);
            }
            lock (m_consumers)
            {
                m_consumers.Remove(consumerTag);
            }

            ModelShutdown -= k.Consumer.HandleModelShutdown;
        }

        public string BasicConsume(string queue,
            string consumerTag,
            BasicConsumeFlags settings,
            Dictionary<string, object> arguments,
            IBasicConsumer consumer)
        {
            // TODO: Replace with flag
            if (ConsumerDispatcher is AsyncConsumerDispatcher asyncDispatcher)
            {
                if (!(consumer is IAsyncBasicConsumer asyncConsumer))
                {
                    // TODO: Friendly message
                    throw new InvalidOperationException("In the async mode you have to use an async consumer");
                }
            }

            var k = new BasicConsumerRpcContinuation ( consumer );

            lock(_rpcLock)
            {
                Enqueue(k);
                // Non-nowait. We have an unconventional means of getting
                // the RPC response, but a response is still expected.
                _Private_BasicConsume(new BasicConsume(0,queue, consumerTag, settings & ~BasicConsumeFlags.NoWait, arguments));
                k.GetReply(this.ContinuationTimeout);
            }
            string actualConsumerTag = k.ConsumerTag;

            return actualConsumerTag;
        }

        public BasicGetResult BasicGet(string queue,
            bool autoAck)
        {
            var k = new BasicGetRpcContinuation();
            lock(_rpcLock)
            {
                Enqueue(k);
                _Private_BasicGet(queue, autoAck);
                k.GetReply(this.ContinuationTimeout);
            }

            return k.m_result;
        }

        public abstract void BasicNack(ulong deliveryTag,BasicNackFlags settings);

        internal void AllocatatePublishSeqNos(int count)
        {
            var c = ZERO;
            lock (m_unconfirmedSet.SyncRoot)
            {
                while(c < count)
                {
                    if (NextPublishSeqNo > 0UL)
                    {
                        if (!m_unconfirmedSet.Contains(NextPublishSeqNo))
                        {
                            m_unconfirmedSet.Add(NextPublishSeqNo);
                        }
                        NextPublishSeqNo++;
                    }
                    c++;
                }
            }
        }

        public void BasicPublish(BasicPublishFull args
//            string exchange,
//            string routingKey,
//            bool mandatory,
//            IBasicProperties basicProperties,
//            byte[] body
            )
        {
            if (!args.HasBasicProperties())
            {
                args.BasicProperties = CreateBasicProperties();
            }
            if (NextPublishSeqNo > 0UL)
            {
                lock (m_unconfirmedSet.SyncRoot)
                {
                    if (!m_unconfirmedSet.Contains(NextPublishSeqNo))
                    {
                        m_unconfirmedSet.Add(NextPublishSeqNo);
                    }
                    NextPublishSeqNo++;
                }
            }
            _Private_BasicPublish(args);
        }

        public abstract void BasicQos(uint prefetchSize,
            ushort prefetchCount,
            bool global);

        public void BasicRecover(bool requeue)
        {
            var k = new SimpleBlockingRpcContinuation();

            lock(_rpcLock)
            {
                Enqueue(k);
                _Private_BasicRecover(requeue);
                k.GetReply(this.ContinuationTimeout);
            }
        }

        public abstract void BasicRecoverAsync(bool requeue);

        public abstract void BasicReject(ulong deliveryTag,
            bool requeue);

        public void Close()
        {
            Close(Constants.ReplySuccess, "Goodbye");
        }

        public void Close(ushort replyCode, string replyText)
        {
            Close(replyCode, replyText, false);
        }

        public void ConfirmSelect()
        {
            if (NextPublishSeqNo == ULZERO)
            {
                NextPublishSeqNo = 1;
            }
            _Private_ConfirmSelect(false);
        }

        ///////////////////////////////////////////////////////////////////////////

        public abstract RabbitMQ.Client.Impl.BasicProperties CreateBasicProperties();
        public IBasicPublishBatch CreateBasicPublishBatch()
        {
            return new BasicPublishBatch(this);
        }


        public void ExchangeBind(ExchangeBind args)
        {
            _Private_ExchangeBind(args);
        }

        public void ExchangeBindNoWait(ExchangeBind args)
        {
            args.Nowait = true;
            _Private_ExchangeBind(args);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, Dictionary<string, object> arguments)
        {
            _Private_ExchangeDeclare(exchange, type,
                (durable ? ExchangeDeclareFlags.Durable : ExchangeDeclareFlags.None) | (autoDelete ? ExchangeDeclareFlags.AutoDelete : ExchangeDeclareFlags.None),
                arguments);
        }

        public void ExchangeDeclareNoWait(string exchange,
            string type,
            bool durable,
            bool autoDelete,
            Dictionary<string, object> arguments)
        {
            _Private_ExchangeDeclare(
                exchange, 
                type, 
                (durable ? ExchangeDeclareFlags.Durable : ExchangeDeclareFlags.None) | 
                (autoDelete ? ExchangeDeclareFlags.AutoDelete : ExchangeDeclareFlags.None) | 
                ExchangeDeclareFlags.NoWait, 
                arguments);
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            _Private_ExchangeDeclare(exchange, string.Empty, ExchangeDeclareFlags.Passive, null);
        }

        public void ExchangeDelete(string exchange,
            bool ifUnused)
        {
            _Private_ExchangeDelete(exchange, ifUnused ? ExchangeDeleteFlags.IfUnused : ExchangeDeleteFlags.None);
        }

        public void ExchangeDeleteNoWait(string exchange,
            bool ifUnused)
        {
            _Private_ExchangeDelete(exchange, ifUnused ? ExchangeDeleteFlags.IfUnused : ExchangeDeleteFlags.None);
        }

        public void ExchangeUnbind(ExchangeUnbind args)
        {
            args.Nowait = false;
            _Private_ExchangeUnbind(args);
        }

        public void ExchangeUnbindNoWait(ExchangeUnbind args)
        {
            args.Nowait = true;
            _Private_ExchangeUnbind(args);
        }

        public void QueueBind(string queue,
            string exchange,
            string routingKey,
            Dictionary<string, object> arguments)
        {
            _Private_QueueBind(queue, exchange, routingKey, false, arguments);
        }

        public void QueueBindNoWait(string queue,
            string exchange,
            string routingKey,
            Dictionary<string, object> arguments)
        {
            _Private_QueueBind(queue, exchange, routingKey, true, arguments);
        }

        public QueueDeclareOk QueueDeclare(string queue, 
            bool durable,
            bool exclusive, bool autoDelete,
            Dictionary<string, object> arguments)
        {
            return QueueDeclare(queue, false, durable, exclusive, autoDelete, arguments);
        }

        public void QueueDeclareNoWait(string queue, bool durable, bool exclusive,
            bool autoDelete, Dictionary<string, object> arguments)
        {
            _Private_QueueDeclare(queue, 
                (durable ? QueueDeclareFlags.Durable : QueueDeclareFlags.None) |
                (exclusive ? QueueDeclareFlags.Exclusive : QueueDeclareFlags.None) | 
                (autoDelete ? QueueDeclareFlags.AutoDelete : QueueDeclareFlags.None) |
                QueueDeclareFlags.NoWait,
                arguments);
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return QueueDeclare(queue, true, false, false, false, null);
        }

        public uint MessageCount(string queue)
        {
            var ok = QueueDeclarePassive(queue);
            return ok.MessageCount;
        }

        public uint ConsumerCount(string queue)
        {
            var ok = QueueDeclarePassive(queue);
            return ok.ConsumerCount;
        }

        public uint QueueDelete(string queue,
            bool ifUnused,
            bool ifEmpty)
        {
            return _Private_QueueDelete(queue, (ifUnused ? QueueDeleteFlags.IfUnused : QueueDeleteFlags.None) |
                (ifEmpty ? QueueDeleteFlags.IfEmpty : QueueDeleteFlags.None));
        }

        public void QueueDeleteNoWait(string queue,
            bool ifUnused,
            bool ifEmpty)
        {
            _Private_QueueDelete(queue, 
                (ifUnused ? QueueDeleteFlags.IfUnused : QueueDeleteFlags.None) | 
                (ifEmpty? QueueDeleteFlags.IfEmpty : QueueDeleteFlags.None) | 
                QueueDeleteFlags.NoWait);
        }

        public uint QueuePurge(string queue)
        {
            return _Private_QueuePurge(queue, false);
        }

        public abstract void QueueUnbind(string queue,
            string exchange,
            string routingKey,
            Dictionary<string, object> arguments);

        public abstract void TxCommit();

        public abstract void TxRollback();

        public abstract void TxSelect();

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            if (NextPublishSeqNo == ULZERO)
            {
                throw new InvalidOperationException("Confirms not selected");
            }
            bool isWaitInfinite = (timeout.TotalMilliseconds == Timeout.Infinite);
            Stopwatch stopwatch = Stopwatch.StartNew();
            lock (m_unconfirmedSet.SyncRoot)
            {
                while (true)
                {
                    if (!IsOpen)
                    {
                        throw new AlreadyClosedException(CloseReason);
                    }

                    if (m_unconfirmedSet.IsEmpty())
                    {
                        bool aux = m_onlyAcksReceived;
                        m_onlyAcksReceived = true;
                        timedOut = false;
                        return aux;
                    }
                    if (isWaitInfinite)
                    {
                        Monitor.Wait(m_unconfirmedSet.SyncRoot);
                    }
                    else
                    {
                        TimeSpan elapsed = stopwatch.Elapsed;
                        if (elapsed > timeout || !Monitor.Wait(
                            m_unconfirmedSet.SyncRoot, timeout - elapsed))
                        {
                            timedOut = true;
                            return true;
                        }
                    }
                }
            }
        }

        public bool WaitForConfirms()
        {
            return WaitForConfirms(TimeSpan.FromMilliseconds(Timeout.Infinite), out bool timedOut);
        }

        public bool WaitForConfirms(TimeSpan timeout)
        {
            return WaitForConfirms(timeout, out bool timedOut);
        }

        public void WaitForConfirmsOrDie()
        {
            WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(Timeout.Infinite));
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            bool onlyAcksReceived = WaitForConfirms(timeout, out bool timedOut);
            if (!onlyAcksReceived)
            {
                Close(new ShutdownEventArgs(ShutdownInitiator.Application,
                    Constants.ReplySuccess,
                    "Nacks Received", new IOException("nack received")),
                    false);
                throw new IOException("Nacks Received");
            }
            if (timedOut)
            {
                Close(new ShutdownEventArgs(ShutdownInitiator.Application,
                    Constants.ReplySuccess,
                    "Timed out waiting for acks",
                    new IOException("timed out waiting for acks")),
                    false);
                throw new IOException("Timed out waiting for acks");
            }
        }

        internal void SendCommands<T>(List<SendCommand<T>> commands) where T: IMethod
        {
            m_flowControlBlock.WaitOne();
            AllocatatePublishSeqNos(commands.Count);
            Session.Transmit(commands);
        }

        protected virtual void HandleAckNack(ulong deliveryTag, bool multiple, bool isNack)
        {
            lock (m_unconfirmedSet.SyncRoot)
            {
                if (multiple)
                {
                    
                    for (ulong i = m_unconfirmedSet[ZERO]; i <= deliveryTag; i++)
                    {
                        // removes potential duplicates
                        while (m_unconfirmedSet.Remove(i))
                        {
                        }
                    }
                }
                else
                {
                    while (m_unconfirmedSet.Remove(deliveryTag))
                    {
                    }
                }
                m_onlyAcksReceived = m_onlyAcksReceived && !isNack;
                if (m_unconfirmedSet.IsEmpty())
                {
                    Monitor.Pulse(m_unconfirmedSet.SyncRoot);
                }
            }
        }

        private QueueDeclareOk QueueDeclare(string queue, bool passive, bool durable, bool exclusive,
            bool autoDelete, Dictionary<string, object> arguments)
        {
            var k = new QueueDeclareRpcContinuation();
            lock(_rpcLock)
            {
                Enqueue(k);
                _Private_QueueDeclare(queue,
                    (passive ? QueueDeclareFlags.Passive : QueueDeclareFlags.None) |
                    (durable ? QueueDeclareFlags.Durable : QueueDeclareFlags.None) |
                    (exclusive ? QueueDeclareFlags.Exclusive : QueueDeclareFlags.None) |
                    (autoDelete ? QueueDeclareFlags.AutoDelete : QueueDeclareFlags.None),
                    arguments);
                k.GetReply(this.ContinuationTimeout);
            }
            return k.m_result;
        }
    }
}
