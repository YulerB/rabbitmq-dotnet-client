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
using System.Diagnostics;
using System.IO;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Framing;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Impl
{
    public enum AssemblyState
    {
        ExpectingMethod,
        ExpectingContentHeader,
        ExpectingContentBody,
        Complete
    }

    public class CommandAssembler
    {
        private const int MaxArrayOfBytesSize = 2_147_483_591;
        private const ulong ULZERO = 0UL;
        
        private IMethod m_method;
        private RabbitMQ.Client.Impl.BasicProperties m_header;
        private FrameBuilder frameBuilder;
        private ulong m_remainingBodyBytes;
        private AssemblyState m_state;
      
        public CommandAssembler(ProtocolBase protocol)
        {
            Reset();
        }

        public bool HandleFrame(InboundFrame f, out AssembledCommandBase<FrameBuilder> result)
        {
            switch (m_state)
            {
                case AssemblyState.ExpectingMethod:
                    {
                        if (!f.IsMethod())
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        m_method = f.Method;
                        m_state = m_method.HasContent ? AssemblyState.ExpectingContentHeader : AssemblyState.Complete;
                        result = CompletedCommand();
                        return result != null;
                    }
                case AssemblyState.ExpectingContentHeader:
                    {
                        if (!f.IsHeader())
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        m_header = f.Header;
                        if (f.TotalBodyBytes > MaxArrayOfBytesSize)
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        m_remainingBodyBytes = f.TotalBodyBytes;
                        UpdateContentBodyState();
                        result = CompletedCommand();
                        return result != null;
                    }
                case AssemblyState.ExpectingContentBody:
                    {
                        if (!f.IsBody())
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        var payloadLength = f.Payload.Length;
                        if ((ulong)payloadLength > m_remainingBodyBytes)
                        {
                            throw new MalformedFrameException
                                (string.Format("Overlong content body received - {0} bytes remaining, {1} bytes received",
                                    m_remainingBodyBytes,
                                    payloadLength));
                        }
                        if (frameBuilder == null)
                        {
                            frameBuilder = new FrameBuilder(2);
                            frameBuilder.Write(f.Payload, 0, payloadLength);
                        }
                        else
                        {
                            frameBuilder.Write(f.Payload, 0, payloadLength);
                        }
                        m_remainingBodyBytes -= (ulong)payloadLength;
                        UpdateContentBodyState();
                        result = CompletedCommand();
                        return result != null;
                    }
                case AssemblyState.Complete:

                default:
#if NETFX_CORE
                    Debug.WriteLine("Received frame in invalid state {0}; {1}", m_state, f);
#endif
                    result = null;
                    return false;
            }
        }

        private AssembledCommandBase<FrameBuilder> CompletedCommand()
        {
            if (m_state != AssemblyState.Complete) return null;

            AssembledCommand result = new AssembledCommand(m_method, m_header, frameBuilder ?? new FrameBuilder());
            Reset();
            return result;
        }

        private void Reset()
        {
            m_state = AssemblyState.ExpectingMethod;
            m_method = null;
            m_header = null;
            frameBuilder = null;
            m_remainingBodyBytes = ULZERO;
        }

        private void UpdateContentBodyState()
        {
            m_state = m_remainingBodyBytes > ULZERO ? AssemblyState.ExpectingContentBody : AssemblyState.Complete;
        }
    }
}
