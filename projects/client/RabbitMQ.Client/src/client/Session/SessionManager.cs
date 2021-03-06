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

using System;
using System.Collections.Generic;
using System.Threading;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;
using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client.Impl
{
    public class SessionManager
    {
        private const ushort ZERO = 0;
        private const ushort USZERO = default(ushort);
        public readonly ushort ChannelMax;
        private readonly UIntAllocator UInts;
        private readonly Connection m_connection;
        private readonly Dictionary<ushort, ISession> m_sessionMap = new Dictionary<ushort, ISession>();
        private bool m_autoClose = false;

        public SessionManager(Connection connection, ushort channelMax)
        {
            m_connection = connection;
            ChannelMax = (channelMax == USZERO) ? ushort.MaxValue : channelMax;
            UInts = new UIntAllocator(1, ChannelMax);
        }
        public SessionManager(Connection connection)
        {
            m_connection = connection;
            ChannelMax =  ushort.MaxValue;
            UInts = new UIntAllocator(1, ChannelMax);
        }

        [Obsolete("Please explicitly close connections instead.")]
        public bool AutoClose
        {
            get { return m_autoClose; }
            set
            {
                m_autoClose = value;
                CheckAutoClose();
            }
        }

        public int Count
        {
            get
            {
                lock (m_sessionMap)
                {
                    return m_sessionMap.Count;
                }
            }
        }

        ///<summary>Called from CheckAutoClose, in a separate thread,
        ///when we decide to close the connection.</summary>
        public void AutoCloseConnection()
        {
            m_connection.Abort(Constants.ReplySuccess, "AutoClose", ShutdownInitiator.Library, Timeout.Infinite);
        }

        ///<summary>If m_autoClose and there are no active sessions
        ///remaining, Close()s the connection with reason code
        ///200.</summary>
        public void CheckAutoClose()
        {
            if (m_autoClose)
            {
                lock (m_sessionMap)
                {
                    if (m_sessionMap.Count == ZERO)
                    {
                        // Run this in a background thread, because
                        // usually CheckAutoClose will be called from
                        // HandleSessionShutdown above, which runs in
                        // the thread of the connection. If we were to
                        // attempt to close the connection from within
                        // the connection's thread, we would suffer a
                        // deadlock as the connection thread would be
                        // blocking waiting for its own mainloop to
                        // reply to it.
#if NETFX_CORE
                        Task.Factory.StartNew(AutoCloseConnection, TaskCreationOptions.LongRunning);
#else
                        new Thread(AutoCloseConnection) { Name= "AutoCloseConnection_"+ m_connection.LocalPort.ToString(), IsBackground=true}.Start();
#endif
                        }
                }
            }
        }

        public ISession Create()
        {
            lock (m_sessionMap)
            {
                ushort? channelNumber = UInts.Allocate();
                if (channelNumber.HasValue)
                {
                    return CreateInternal(channelNumber.Value);
                }
                throw new ChannelAllocationException();
            }
        }

        public ISession Create(ushort channelNumber)
        {
            lock (m_sessionMap)
            {
                if (UInts.Reserve(channelNumber))
                {
                    return CreateInternal(channelNumber);
                }
                throw new ChannelAllocationException(channelNumber);
            }
        }

        public ISession CreateInternal(ushort channelNumber)
        {
            lock (m_sessionMap)
            {
                ISession session = new Session(m_connection, channelNumber);
                session.SessionShutdown += HandleSessionShutdown;
                m_sessionMap[channelNumber] = session;
                return session;
            }
        }

        public void HandleSessionShutdown(object sender, ShutdownEventArgs reason)
        {
            lock (m_sessionMap)
            {
                var session = (ISession) sender;
                m_sessionMap.Remove(session.ChannelNumber);
                UInts.Free(session.ChannelNumber);
                CheckAutoClose();
            }
        }

        public ISession Lookup(ushort number)
        {
            lock (m_sessionMap)
            {
                return m_sessionMap[number];
            }
        }

        ///<summary>Replace an active session slot with a new ISession
        ///implementation. Used during channel quiescing.</summary>
        ///<remarks>
        /// Make sure you pass in a channelNumber that's currently in
        /// use, as if the slot is unused, you'll get a null pointer
        /// exception.
        ///</remarks>
        public ISession Swap(ushort channelNumber, ISession replacement)
        {
            lock (m_sessionMap)
            {
                ISession previous = m_sessionMap[channelNumber];
                previous.SessionShutdown -= HandleSessionShutdown;
                m_sessionMap[channelNumber] = replacement;
                replacement.SessionShutdown += HandleSessionShutdown;
                return previous;
            }
        }
    }
}
