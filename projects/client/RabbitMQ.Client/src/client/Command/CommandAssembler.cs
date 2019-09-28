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
        
        public IMethod m_method;
        public RabbitMQ.Client.Impl.BasicProperties m_header;
        public FrameBuilder frameBuilder;
        public ulong m_remainingBodyBytes;
        public AssemblyState m_state;
      
        public CommandAssembler(ProtocolBase protocol)
        {
            Reset();
        }

        public Command HandleFrame(InboundFrame f)
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
                        m_state = f.Method.HasContent ? AssemblyState.ExpectingContentHeader : AssemblyState.Complete;
                        return CompletedCommand();
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
                        return CompletedCommand();
                    }
                case AssemblyState.ExpectingContentBody:
                    {
                        if (!f.IsBody())
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        if ((ulong)f.Payload.Length > m_remainingBodyBytes)
                        {
                            throw new MalformedFrameException
                                (string.Format("Overlong content body received - {0} bytes remaining, {1} bytes received",
                                    m_remainingBodyBytes,
                                    f.Payload.Length));
                        }
                        if (frameBuilder == null)
                        {
                            frameBuilder = new FrameBuilder();
                            frameBuilder.Write(f.Payload, 0, f.Payload.Length);
                            //m_bodyStream = new MemoryStream(f.Payload, true);
                        }
                        else
                        {
                            frameBuilder.Write(f.Payload, 0, f.Payload.Length);
                            //m_bodyStream.Write(f.Payload, 0, f.Payload.Length);
                        }
                        m_remainingBodyBytes -= (ulong)f.Payload.Length;
                        UpdateContentBodyState();
                        return CompletedCommand();
                    }
                case AssemblyState.Complete:

                default:
#if NETFX_CORE
                    Debug.WriteLine("Received frame in invalid state {0}; {1}", m_state, f);
#endif
                    return null;
            }
        }

        private Command CompletedCommand()
        {
            if (m_state == AssemblyState.Complete)
            {
                if (frameBuilder == null)
                {
                    Command result = new Command(m_method, m_header, new FrameBuilder());
                    Reset();
                    return result;
                }
                else
                {
                    Command result = new Command(m_method, m_header, frameBuilder);
                    Reset();
                    return result;
                }
            }
            else
            {
                return null;
            }
        }

        private void Reset()
        {
            m_state = AssemblyState.ExpectingMethod;
            m_method = null;
            m_header = null;
            frameBuilder = null;
            m_remainingBodyBytes = 0;
        }

        private void UpdateContentBodyState()
        {
            m_state = (m_remainingBodyBytes > 0)
                ? AssemblyState.ExpectingContentBody
                : AssemblyState.Complete;
        }
    }
}
