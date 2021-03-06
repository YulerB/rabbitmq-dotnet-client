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
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.IO;

#if NETFX_CORE

using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.ApplicationModel;

#else
#endif

using System.Text;
using System.Threading;
using System.Reflection;

namespace RabbitMQ.Client.Framing.Impl
{
    public class Connection : IConnection
    {
        private const ushort USZERO = default(ushort);
        private readonly object m_eventLock = new object();

        ///<summary>Heartbeat frame for transmission. Reusable across connections.</summary>
        private readonly EmptyOutboundFrame m_heartbeatFrame = new EmptyOutboundFrame();

        private ManualResetEventSlim m_appContinuation = new ManualResetEventSlim(false);
        private Dictionary<string, object> m_clientProperties;

        private volatile ShutdownEventArgs m_closeReason = null;
        private volatile bool m_closed = false;

        private EventHandler<CallbackExceptionEventArgs> m_callbackException;
        private EventHandler<EventArgs> m_recoverySucceeded;
        private EventHandler<ConnectionRecoveryErrorEventArgs> connectionRecoveryFailure;
        private EventHandler<ConnectionBlockedEventArgs> m_connectionBlocked;
        private EventHandler<ShutdownEventArgs> m_connectionShutdown;
        private EventHandler<EventArgs> m_connectionUnblocked;

        private IConnectionFactory m_factory;
        public readonly IFrameHandler m_frameHandler;
        private readonly Guid m_id = Guid.NewGuid();
        private ModelBase m_model0;
        private MainSession m_session0;
        private SessionManager m_sessionManager;
        private const int ZERO = 0;
        private const uint UZERO = 0U;
        private IList<ShutdownReportEntry> m_shutdownReport = new SynchronizedList<ShutdownReportEntry>(new List<ShutdownReportEntry>());

        //
        // Heartbeats
        //

        private ushort m_heartbeat = USZERO;
        private TimeSpan m_heartbeatTimeSpan = TimeSpan.FromSeconds(default(double));
        private int m_missedHeartbeats = ZERO;

        private Timer _heartbeatWriteTimer;
        private Timer _heartbeatReadTimer;
        private AutoResetEvent m_heartbeatRead = new AutoResetEvent(false);

        private readonly object _heartBeatReadLock = new object();
        private readonly object _heartBeatWriteLock = new object();
        private bool m_hasDisposedHeartBeatReadTimer;
        private bool m_hasDisposedHeartBeatWriteTimer;


        /*
             Closing
         
             There are 3 types of close.
             1. we get a close from code in the normal course of things.
             2. we get a close because of heartbeat missed.
             3. we get a remove socket disconnect because of heartbeat missed.
        */


#if CORECLR
        private static readonly string version = typeof(Connection).GetTypeInfo().Assembly
                                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                                .InformationalVersion;
#else
        private static readonly string version = typeof(Connection).Assembly
                                            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                            .InformationalVersion;
#endif

        // true if we haven't finished connection negotiation.
        // In this state socket exceptions are treated as fatal connection
        // errors, otherwise as read timeouts
        public ConsumerWorkService ConsumerWorkService { get; private set; }

        public Connection(
            IConnectionFactory factory, 
            bool insist, 
            IFrameHandler frameHandler, 
            string clientProvidedName = null
        )
        {
            ClientProvidedName = clientProvidedName;
            KnownHosts = null;
            FrameMax = UZERO;
            m_factory = factory;

            m_frameHandler = frameHandler;
            m_frameHandler.ReceivedFrame += M_frameHandler_ReceivedFrame;
            m_frameHandler.EndOfStreamEvent += M_frameHandler_EndOfStreamEvent;

            if (factory is IAsyncConnectionFactory asyncConnectionFactory && asyncConnectionFactory.DispatchConsumersAsync)
            {
                ConsumerWorkService = new AsyncConsumerWorkService();
            }
            else
            {
                ConsumerWorkService = new ConsumerWorkService();
            }

            m_sessionManager = new SessionManager(this);
            m_session0 = new MainSession(this) { Handler = NotifyReceivedCloseOk };
            m_model0 = (ModelBase)Protocol.CreateModel(m_session0);

            Open(insist);
        }

        private void M_frameHandler_ReceivedFrame(object sender, EventArgs e)
        {
            try
            {
                InboundFrame frame = m_frameHandler.ReadFrame();
                NotifyHeartbeatListener();

                if (frame.Type == FrameType.FrameHeartbeat)
                {
                    return;
                }

                if (frame.IsMainChannel())
                {
                    // In theory, we could get non-connection.close-ok
                    // frames here while we're quiescing (m_closeReason !=
                    // null). In practice, there's a limited number of
                    // things the server can ask of us on channel 0 -
                    // essentially, just connection.close. That, combined
                    // with the restrictions on pipelining, mean that
                    // we're OK here to handle channel 0 traffic in a
                    // quiescing situation, even though technically we
                    // should be ignoring everything except
                    // connection.close-ok.
                    m_session0.HandleFrame(frame);
                }
                else if (m_closeReason == null)
                {
                    // No close reason, not quiescing the
                    // connection. Handle the frame. (Of course, the
                    // Session itself may be quiescing this particular
                    // channel, but that's none of our concern.)
                    ISession session = m_sessionManager.Lookup(frame.Channel);
                    if (session == null)
                    {
                        throw new ChannelErrorException(frame.Channel);
                    }
                    else
                    {
                        session.HandleFrame(frame);
                    }
                }
            }
            catch (SoftProtocolException spe)
            {
                QuiesceChannel(spe);
            }
            catch (HardProtocolException hpe)
            {
                //System.Diagnostics.Debug.WriteLine("M_frameHandler_ReceivedFrame.HardProtocolException - " + hpe.ToString());
                //Console.WriteLine("M_frameHandler_ReceivedFrame.HardProtocolException - " + hpe.ToString());

                HardProtocolExceptionHandler(hpe);
            }
#if !NETFX_CORE
            catch (Exception ex)
            {
                HandleMainLoopException(new ShutdownEventArgs(ShutdownInitiator.Library, Constants.InternalError, "Unexpected Exception", ex));
            }
#endif
        }

        private void M_frameHandler_EndOfStreamEvent(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("M_frameHandler_EndOfStreamEvent");
            //Console.WriteLine("M_frameHandler_EndOfStreamEvent");
            try
            {
                if (m_model0.IsOpen)
                {
                    LogCloseError("Connection didn't close cleanly. Socket closed unexpectedly", new EndOfStreamException());
                }

                m_appContinuation.Set();

            }
            finally
            {
                FinishClose();
            }

        }

        public Guid Id { get { return m_id; } }

        public event EventHandler<EventArgs> RecoverySucceeded
        {
            add
            {
                lock (m_eventLock)
                {
                    m_recoverySucceeded += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_recoverySucceeded -= value;
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

        public event EventHandler<ConnectionBlockedEventArgs> ConnectionBlocked
        {
            add
            {
                lock (m_eventLock)
                {
                    m_connectionBlocked += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_connectionBlocked -= value;
                }
            }
        }

        public event EventHandler<ShutdownEventArgs> ConnectionShutdown
        {
            add
            {
                bool ok = false;
                lock (m_eventLock)
                {
                    if (m_closeReason == null)
                    {
                        m_connectionShutdown += value;
                        ok = true;
                    }
                }
                if (!ok)
                {
                    value(this, m_closeReason);
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_connectionShutdown -= value;
                }
            }
        }

        public event EventHandler<EventArgs> ConnectionUnblocked
        {
            add
            {
                lock (m_eventLock)
                {
                    m_connectionUnblocked += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_connectionUnblocked -= value;
                }
            }
        }

        public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError
        {
            add
            {
                lock (m_eventLock)
                {
                    connectionRecoveryFailure += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    connectionRecoveryFailure -= value;
                }
            }
        }
        public string ClientProvidedName { get; private set; }

        [Obsolete("Please explicitly close connections instead.")]
        public bool AutoClose
        {
            get { return m_sessionManager.AutoClose; }
            set { m_sessionManager.AutoClose = value; }
        }

        public ushort ChannelMax
        {
            get { return m_sessionManager.ChannelMax; }
        }

        public Dictionary<string, object> ClientProperties
        {
            get { return m_clientProperties; }
            set { m_clientProperties = value; }
        }

        public ShutdownEventArgs CloseReason
        {
            get { return m_closeReason; }
        }

        public AmqpTcpEndpoint Endpoint
        {
            get { return m_frameHandler.Endpoint; }
        }

        public uint FrameMax { get; set; }

        public ushort Heartbeat
        {
            get { return m_heartbeat; }
            set
            {
                m_heartbeat = value;
                // timers fire at slightly below half the interval to avoid race
                // conditions
                m_heartbeatTimeSpan = TimeSpan.FromMilliseconds((value * 1000) / 4);
                //m_frameHandler.ReadTimeout = value * 1000 * 2;
            }
        }

        public bool IsOpen
        {
            get { return CloseReason == null; }
        }

        public AmqpTcpEndpoint[] KnownHosts { get; set; }
        
        public int LocalPort
        {
            get { return m_frameHandler.LocalPort; }
        }

        ///<summary>Another overload of a Protocol property, useful
        ///for exposing a tighter type.</summary>
        public ProtocolBase Protocol
        {
            get { return m_frameHandler.Endpoint.Protocol as ProtocolBase; }
        }

        public Dictionary<string, object> ServerProperties { get; set; }

        public IList<ShutdownReportEntry> ShutdownReport
        {
            get { return m_shutdownReport; }
        }

        ///<summary>Explicit implementation of IConnection.Protocol.</summary>
        IProtocol IConnection.Protocol
        {
            get { return m_frameHandler.Endpoint.Protocol; }
        }

        public static Dictionary<string, object> DefaultClientProperties()
        {
            return new Dictionary<string, object>
            {
                {"product" , Encoding.UTF8.GetBytes("RabbitMQ")},
                {"version", Encoding.UTF8.GetBytes(version)},
                {"platform", Encoding.UTF8.GetBytes(".NET")},
                {"copyright", Encoding.UTF8.GetBytes("Copyright (c) 2007-2016 Pivotal Software, Inc.")},
                {"information", Encoding.UTF8.GetBytes("Licensed under the MPL.  See http://www.rabbitmq.com/")}
            };
        }

        public void Abort(ushort reasonCode, string reasonText, ShutdownInitiator initiator, int timeout)
        {
            Close(new ShutdownEventArgs(initiator, reasonCode, reasonText), true, timeout);
        }

        public void Close(ShutdownEventArgs reason)
        {
            Close(reason, false, Timeout.Infinite);
        }

        ///<summary>Try to close connection in a graceful way</summary>
        ///<remarks>
        ///<para>
        ///Shutdown reason contains code and text assigned when closing the connection,
        ///as well as the information about what initiated the close
        ///</para>
        ///<para>
        ///Abort flag, if true, signals to close the ongoing connection immediately
        ///and do not report any errors if it was already closed.
        ///</para>
        ///<para>
        ///Timeout determines how much time internal close operations should be given
        ///to complete. Negative or Timeout.Infinite value mean infinity.
        ///</para>
        ///</remarks>
        public void Close(ShutdownEventArgs reason, bool abort, int timeout)
        {
            //System.Diagnostics.Debug.WriteLine("Close");
            //Console.WriteLine("Close");
            if (!SetCloseReason(reason))
            {
                if (!abort)
                {
                    throw new AlreadyClosedException(m_closeReason);
                }
            }
            else
            {
                OnShutdown();

                m_session0.SetSessionClosing(false);

                Protocol.CreateConnectionClose(reason.ReplyCode, reason.ReplyText, out SendCommand<Impl.ConnectionClose> connectionClose);
                try
                {
                    // Try to send connection.close
                    // Wait for CloseOk in the MainLoop
                    m_session0.Transmit(connectionClose);
                }
                catch (AlreadyClosedException ace)
                {
                    if (!abort)
                    {
                        throw ace;
                    }
                }
                catch (NotSupportedException)
                {
                    // buffered stream had unread data in it and Flush()
                    // was called, ignore to not confuse the user
                }
                catch (IOException ioe)
                {
                    if (m_model0.IsOpen)
                    {
                        if (!abort)
                        {
                            throw ioe;
                        }
                        else
                        {
                            LogCloseError("Couldn't close connection cleanly. "
                                          + "Socket closed unexpectedly", ioe);
                        }
                    }
                }
                finally
                {
                    MaybeStopHeartbeatTimers();
                }
            }
            
            //if (!m_appContinuation.Wait(ValidatedTimeout(timeout)))
            //{
            //    m_frameHandler.Close();
            //}
        }

        public ISession CreateSession()
        {
            return m_sessionManager.Create();
        }

        public void EnsureIsOpen()
        {
            if (!IsOpen)
            {
                throw new AlreadyClosedException(CloseReason);
            }
        }

        // Only call at the end of the Mainloop or HeartbeatLoop
        private void FinishClose()
        {
            // Notify hearbeat loops that they can leave
            m_heartbeatRead.Set();
            m_closed = true;
            MaybeStopHeartbeatTimers();

            m_frameHandler.Close();
            m_model0.SetCloseReason(m_closeReason);
            m_model0.FinishClose();
        }

        private void HandleMainLoopException(ShutdownEventArgs reason)
        {
            if (!SetCloseReason(reason))
            {
                LogCloseError("Unexpected Main Loop Exception while closing: " + reason, new Exception(reason.ToString()));
                return;
            }

            OnShutdown();
            LogCloseError("Unexpected connection closure: " + reason, new Exception(reason.ToString()));
        }

        private bool HardProtocolExceptionHandler(HardProtocolException hpe)
        {
            var shutdownReason = hpe.ShutdownReason;

            if (SetCloseReason(shutdownReason))
            {
                OnShutdown();
                m_session0.SetSessionClosing(false);
                Protocol.CreateConnectionClose(shutdownReason.ReplyCode, shutdownReason.ReplyText, out SendCommand<Impl.ConnectionClose> connectionClose);

                try
                {
                    m_session0.Transmit(connectionClose);
                    return true;
                }
                catch (IOException ioe)
                {
                    LogCloseError("Broker closed socket unexpectedly", ioe);
                }
            }
            else
            {
                LogCloseError("Hard Protocol Exception occured "
                              + "while closing the connection", hpe);
            }

            return false;
        }

        internal void InternalClose(ShutdownEventArgs reason)
        {
            //System.Diagnostics.Debug.WriteLine("InternalClose");
            //Console.WriteLine("InternalClose");

            if (!SetCloseReason(reason))
            {
                if (m_closed)
                {
                    throw new AlreadyClosedException(m_closeReason);
                }
                // We are quiescing, but still allow for server-close
            }

            OnShutdown();
            m_session0.SetSessionClosing(true);

            FinishClose();
        }

        private void LogCloseError(String error, Exception ex)
        {
            ESLog.Error(error, ex);
            m_shutdownReport.Add(new ShutdownReportEntry(error, ex));
        }

        private void NotifyHeartbeatListener()
        {
            if (m_heartbeat != USZERO) m_heartbeatRead.Set();
        }

        private void NotifyReceivedCloseOk()
        {
            //System.Diagnostics.Debug.WriteLine("NotifyReceivedCloseOk - Closing");
            //Console.WriteLine("NotifyReceivedCloseOk - Closing");

            FinishClose();
        }

        internal void OnCallbackException(CallbackExceptionEventArgs args)
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

        protected virtual void OnConnectionBlocked(ConnectionBlockedEventArgs args)
        {
            EventHandler<ConnectionBlockedEventArgs> handler= m_connectionBlocked;
            if (handler != null)
            {
                foreach (EventHandler<ConnectionBlockedEventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, args);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e,
                            new Dictionary<string, object>
                            {
                                {"context", "OnConnectionBlocked"}
                            }));
                    }
                }
            }
        }

        protected virtual void OnConnectionUnblocked()
        {
            EventHandler<EventArgs> handler= m_connectionUnblocked;
            if (handler != null)
            {
                foreach (EventHandler<EventArgs> h in handler.GetInvocationList())
                {
                    try
                    {
                        h(this, EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        OnCallbackException(CallbackExceptionEventArgs.Build(e,
                            new Dictionary<string, object>
                            {
                                {"context", "OnConnectionUnblocked"}
                            }));
                    }
                }
            }
        }

        ///<summary>Broadcasts notification of the final shutdown of the connection.</summary>
        protected virtual void OnShutdown()
        {
            EventHandler<ShutdownEventArgs> handler;
            ShutdownEventArgs reason;
            lock (m_eventLock)
            {
                handler = m_connectionShutdown;
                reason = m_closeReason;
                m_connectionShutdown = null;
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
                        OnCallbackException(CallbackExceptionEventArgs.Build(e,
                            new Dictionary<string, object>
                            {
                                {"context", "OnShutdown"}
                            }));
                    }
                }
            }
        }

        private void Open(bool insist)
        {
            StartAndTune();
            m_model0.ConnectionOpen(m_factory.VirtualHost, String.Empty, false);
        }

        ///<summary>
        /// Sets the channel named in the SoftProtocolException into
        /// "quiescing mode", where we issue a channel.close and
        /// ignore everything except for subsequent channel.close
        /// messages and the channel.close-ok reply that should
        /// eventually arrive.
        ///</summary>
        ///<remarks>
        ///<para>
        /// Since a well-behaved peer will not wait indefinitely before
        /// issuing the close-ok, we don't bother with a timeout here;
        /// compare this to the case of a connection.close-ok, where a
        /// timeout is necessary.
        ///</para>
        ///<para>
        /// We need to send the close method and politely wait for a
        /// reply before marking the channel as available for reuse.
        ///</para>
        ///<para>
        /// As soon as SoftProtocolException is detected, we should stop
        /// servicing ordinary application work, and should concentrate
        /// on bringing down the channel as quickly and gracefully as
        /// possible. The way this is done, as per the close-protocol,
        /// is to signal closure up the stack *before* sending the
        /// channel.close, by invoking ISession.Close. Once the upper
        /// layers have been signalled, we are free to do what we need
        /// to do to clean up and shut down the channel.
        ///</para>
        ///</remarks>
        private void QuiesceChannel(SoftProtocolException pe)
        {
            // Construct the QuiescingSession that we'll use during
            // the quiesce process.

            ISession newSession = new QuiescingSession(this,
                pe.Channel,
                pe.ShutdownReason);

            // Here we detach the session from the connection. It's
            // still alive: it just won't receive any further frames
            // from the mainloop (once we return to the mainloop, of
            // course). Instead, those frames will be directed at the
            // new QuiescingSession.
            ISession oldSession = m_sessionManager.Swap(pe.Channel, newSession);

            // Now we have all the information we need, and the event
            // flow of the *lower* layers is set up properly for
            // shutdown. Signal channel closure *up* the stack, toward
            // the model and application.
            oldSession.Close(pe.ShutdownReason);

            // The upper layers have been signalled. Now we can tell
            // our peer. The peer will respond through the lower
            // layers - specifically, through the QuiescingSession we
            // installed above.
            newSession.Transmit(ChannelCloseWrapper(pe.ReplyCode, pe.Message));
        }

        private bool SetCloseReason(ShutdownEventArgs reason)
        {
            lock (m_eventLock)
            {
                if (m_closeReason == null)
                {
                    m_closeReason = reason;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void MaybeStartHeartbeatTimers()
        {
            if (Heartbeat != USZERO)
            {
                m_hasDisposedHeartBeatReadTimer = false;
                m_hasDisposedHeartBeatWriteTimer = false;
#if NETFX_CORE
                _heartbeatWriteTimer = new Timer(HeartbeatWriteTimerCallback);
                _heartbeatReadTimer = new Timer(HeartbeatReadTimerCallback);
                _heartbeatWriteTimer.Change(200, (int)m_heartbeatTimeSpan.TotalMilliseconds);
                _heartbeatReadTimer.Change(200, (int)m_heartbeatTimeSpan.TotalMilliseconds);
#else
                _heartbeatWriteTimer = new Timer(HeartbeatWriteTimerCallback, null, 200, (int)m_heartbeatTimeSpan.TotalMilliseconds);
                _heartbeatReadTimer = new Timer(HeartbeatReadTimerCallback, null, 200, (int)m_heartbeatTimeSpan.TotalMilliseconds);
                _heartbeatWriteTimer.Change(TimeSpan.FromMilliseconds(200), m_heartbeatTimeSpan);
                _heartbeatReadTimer.Change(TimeSpan.FromMilliseconds(200), m_heartbeatTimeSpan);
#endif
            }
        }

        private void HeartbeatReadTimerCallback(object state)
        {
            lock (_heartBeatReadLock)
            {
                if (m_hasDisposedHeartBeatReadTimer)
                {
                    return;
                }
                bool shouldTerminate = false;
                try
                {
                    if (!m_closed)
                    {
                        if (!m_heartbeatRead.WaitOne(ZERO))
                        {
                            m_missedHeartbeats++;
                        }
                        else
                        {
                            m_missedHeartbeats = ZERO;
                        }

                        // We check against 8 = 2 * 4 because we need to wait for at
                        // least two complete heartbeat setting intervals before
                        // complaining, and we've set the socket timeout to a quarter
                        // of the heartbeat setting in setHeartbeat above.
                        if (m_missedHeartbeats > 2 * 4)
                        {
                            String description = $"Heartbeat missing with heartbeat == {m_heartbeat.ToString()} seconds";
                            var eose = new EndOfStreamException(description);
                            ESLog.Error(description, eose);
                            m_shutdownReport.Add(new ShutdownReportEntry(description, eose));
                            HandleMainLoopException(
                                new ShutdownEventArgs(ShutdownInitiator.Library, USZERO, "End of stream", eose));
                            shouldTerminate = true;
                        }

                    }

                    if (shouldTerminate)
                    {
                        //System.Diagnostics.Debug.WriteLine("Heartbeat Read Failed - Closing");
                        //Console.WriteLine("Heartbeat Read Failed - Closing");

                        FinishClose();
                    }
                    else if (_heartbeatReadTimer != null)
                    {
                        _heartbeatReadTimer.Change(Heartbeat * 1000, Timeout.Infinite);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // timer is already disposed,
                    // e.g. due to shutdown
                }
                catch (NullReferenceException)
                {
                    // timer has already been disposed from a different thread after null check
                    // this event should be rare
                }
            }
        }

        private void HeartbeatWriteTimerCallback(object state)
        {
            lock (_heartBeatWriteLock)
            {
                if (m_hasDisposedHeartBeatWriteTimer)
                {
                    return;
                }
                bool shouldTerminate = false;
                try
                {
                    try
                    {
                        if (!m_closed)
                        {
                            //System.Diagnostics.Debug.WriteLine("Write HeartBeat - " + DateTime.UtcNow.ToString());
                            //Console.WriteLine("Write HeartBeat - " + DateTime.UtcNow.ToString());

                            WriteFrame(m_heartbeatFrame);
                        }
                    }
                    catch (Exception e)
                    {
                        HandleMainLoopException(new ShutdownEventArgs(
                            ShutdownInitiator.Library,
                            USZERO,
                            "End of stream",
                            e));
                        shouldTerminate = true;
                    }

                    if (m_closed || shouldTerminate)
                    {
                        //System.Diagnostics.Debug.WriteLine("Heartbeat Wrie Failed - Closing");
                        //Console.WriteLine("Heartbeat Wrie Failed - Closing");

                        FinishClose();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // timer is already disposed,
                    // e.g. due to shutdown
                } 
            }
        }

        private void MaybeStopHeartbeatTimers()
        {
            lock (_heartBeatReadLock)
            {
                MaybeDisposeTimer(ref _heartbeatReadTimer);
                m_hasDisposedHeartBeatReadTimer = true;
            }

            lock (_heartBeatWriteLock)
            {
                MaybeDisposeTimer(ref _heartbeatWriteTimer);
                m_hasDisposedHeartBeatWriteTimer = true;
            }
        }

        private void MaybeDisposeTimer(ref Timer timer)
        {
            // capture the timer to reduce chance of a null ref exception
            var captured = timer;
            if (captured != null)
            {
                try
                {
                    captured.Change(Timeout.Infinite, Timeout.Infinite);
                    captured.Dispose();
                    timer = null;
                }
                catch (ObjectDisposedException)
                {
                    // we are shutting down, ignore
                }
                catch (NullReferenceException)
                {
                    // this should be very rare but could occur from a race condition
                }
            }
        }

        public sealed override string ToString()
        {
            return $"Connection({m_id.ToString()},{Endpoint})";
        }

        internal void WriteFrame(OutboundFrame f)
        {
            m_frameHandler.WriteFrame(f);
        }

        internal void WriteFrameSet(List<OutboundFrame> f)
        {
            m_frameHandler.WriteFrameSet(f);
        }

        ///<summary>API-side invocation of connection abort.</summary>
        public void Abort()
        {
            Abort(Timeout.Infinite);
        }

        ///<summary>API-side invocation of connection abort.</summary>
        public void Abort(ushort reasonCode, string reasonText)
        {
            Abort(reasonCode, reasonText, Timeout.Infinite);
        }

        ///<summary>API-side invocation of connection abort with timeout.</summary>
        public void Abort(int timeout)
        {
            Abort(Constants.ReplySuccess, "Connection close forced", timeout);
        }

        ///<summary>API-side invocation of connection abort with timeout.</summary>
        public void Abort(ushort reasonCode, string reasonText, int timeout)
        {
            Abort(reasonCode, reasonText, ShutdownInitiator.Application, timeout);
        }

        ///<summary>API-side invocation of connection.close.</summary>
        public void Close()
        {
            Close(Constants.ReplySuccess, "Goodbye", Timeout.Infinite);
        }

        ///<summary>API-side invocation of connection.close.</summary>
        public void Close(ushort reasonCode, string reasonText)
        {
            Close(reasonCode, reasonText, Timeout.Infinite);
        }

        ///<summary>API-side invocation of connection.close with timeout.</summary>
        public void Close(int timeout)
        {
            Close(Constants.ReplySuccess, "Goodbye", timeout);
        }

        ///<summary>API-side invocation of connection.close with timeout.</summary>
        public void Close(ushort reasonCode, string reasonText, int timeout)
        {
            Close(new ShutdownEventArgs(ShutdownInitiator.Application, reasonCode, reasonText), false, timeout);
        }

        public IModel CreateModel()
        {
            EnsureIsOpen();
            ISession session = CreateSession();
            var model =Protocol.CreateModel(session, this.ConsumerWorkService);
            model.ContinuationTimeout = m_factory.ContinuationTimeout;
            ((IFullModel)model)._Private_ChannelOpen(string.Empty);
            return model;
        }

        internal void HandleConnectionBlocked(string reason)
        {
            OnConnectionBlocked(new ConnectionBlockedEventArgs(reason));
        }

        internal void HandleConnectionUnblocked()
        {
            OnConnectionUnblocked();
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                try
                {
                    MaybeStopHeartbeatTimers();
                    Abort();
                    m_frameHandler?.Dispose();
                    m_appContinuation?.Dispose();
                    m_heartbeatRead?.Dispose();
                    
                    AsyncConsumerWorkService cws = ConsumerWorkService as AsyncConsumerWorkService;
                    cws?.Dispose();
                }
                catch (OperationInterruptedException)
                {
                    // ignored, see rabbitmq/rabbitmq-dotnet-client#133
                }
                finally
                {
                    connectionRecoveryFailure = null;
                    m_connectionBlocked = null;
                    m_connectionUnblocked = null;
                    m_callbackException = null;
                    m_recoverySucceeded = null;
                    m_connectionShutdown = null;
                    m_connectionUnblocked = null;
                    m_appContinuation = null;
                    m_heartbeatRead=null;
                }
            }

            // dispose unmanaged resources
        }

        private SendCommand<Impl.ChannelClose> ChannelCloseWrapper(ushort reasonCode, string reasonText)
        {
            Protocol.CreateChannelClose(reasonCode, reasonText, out SendCommand<Impl.ChannelClose> request);
            return request;
        }

        private void StartAndTune()
        {
            m_model0.HandshakeContinuationTimeout = m_factory.HandshakeContinuationTimeout;
            m_model0.CreateConnectionStart();
            m_frameHandler.SendHeader();
            OnConnectionStarted(this, m_model0.WaitForConnection());
        }

        private void OnConnectionStarted(object sender, ConnectionStart connectionStart)
        {
            ServerProperties = connectionStart.ServerProperties;

            var serverVersion = connectionStart.Version;
            if (!serverVersion.Equals(Protocol.Version))
            {
                //System.Diagnostics.Debug.WriteLine("Connection Started - Protocol Error - Closing");
                //Console.WriteLine("Connection Started - Protocol Error - Closing");

                FinishClose();
                throw new ProtocolVersionMismatchException(Protocol.MajorVersion,
                    Protocol.MinorVersion,
                    serverVersion.Major,
                    serverVersion.Minor);
            }

            m_clientProperties = new Dictionary<string, object>(m_factory.ClientProperties)
            {
                { "capabilities", Protocol.Capabilities },
                { "connection_name", this.ClientProvidedName }
            };

            // FIXME: parse out locales properly!
            ConnectionTuneDetails connectionTune = default(ConnectionTuneDetails);
            bool tuned = false;
            try
            {
                string[] mechanisms = connectionStart.Mechanisms.Split(new char[] { ' ' });
                IAuthMechanismFactory mechanismFactory = m_factory.AuthMechanismFactory(mechanisms);
                if (mechanismFactory == null)
                {
                    throw new IOException("No compatible authentication mechanism found - server offered [" + connectionStart.Mechanisms + "]");
                }
                IAuthMechanism mechanism = mechanismFactory.GetInstance();
                string challenge = null;
                const string locale = "en_US";
                ConnectionSecureOrTune res;
                do
                {
                    string response = mechanism.HandleChallenge(challenge, m_factory);
                    if (challenge == null)
                    {
                        res = m_model0.ConnectionStartOk(new ConnectionStartOk(m_clientProperties,mechanismFactory.Name,response, locale));
                    }
                    else
                    {
                        res = m_model0.ConnectionSecureOk(response);
                    }

                    if (!res.HasChallenge())
                    {
                        connectionTune = res.TuneDetails;
                        tuned = true;
                    }
                    else
                    {
                        challenge = res.Challenge;
                    }
                }
                while (!tuned);
            }
            catch (OperationInterruptedException e)
            {
                if (e.ShutdownReason != null && e.ShutdownReason.IsAccessRefused())
                {
                    throw new AuthenticationFailureException(e.ShutdownReason.ReplyText);
                }
                throw new PossibleAuthenticationFailureException(
                    "Possibly caused by authentication failure", e);
            }

            var channelMax = NegotiatedMaxValue(m_factory.RequestedChannelMax, connectionTune.ChannelMax);
            m_sessionManager = new SessionManager(this, channelMax);

            FrameMax = NegotiatedMaxValue(m_factory.RequestedFrameMax, connectionTune.FrameMax);

            Heartbeat = NegotiatedMaxValue(m_factory.RequestedHeartbeat, connectionTune.Heartbeat);

            m_model0.ConnectionTuneOk(new ConnectionTuneOk (channelMax,FrameMax,Heartbeat));

            // now we can start heartbeat timers
            MaybeStartHeartbeatTimers();

        }
        private static uint NegotiatedMaxValue(uint clientValue, uint serverValue)
        {
            return (clientValue == UZERO || serverValue == UZERO) ?
                Math.Max(clientValue, serverValue) :
                Math.Min(clientValue, serverValue);
        }
        private static ushort NegotiatedMaxValue(ushort clientValue, ushort serverValue)
        {
            return (clientValue == USZERO || serverValue == USZERO) ?
                Math.Max(clientValue, serverValue) :
                Math.Min(clientValue, serverValue);
        }
    }
}
