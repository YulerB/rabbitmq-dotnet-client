﻿// Autogenerated code. Do not edit.

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
//   The contents of this file are subject to the Mozilla Public License
//   Version 1.1 (the "License"); you may not use this file except in
//   compliance with the License. You may obtain a copy of the License at
//   http://www.rabbitmq.com/mpl.html
//
//   Software distributed under the License is distributed on an "AS IS"
//   basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//   License for the specific language governing rights and limitations
//   under the License.
//
//   The Original Code is RabbitMQ.
//
//   The Initial Developer of the Original Code is Pivotal Software, Inc.
//   Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
namespace RabbitMQ.Client.Framing.Impl
{
    using RabbitMQ.Client.Framing;
    using System;

    /// <summary>Autogenerated type. Private implementation class - do not use directly.</summary>
    public class ConnectionOpen : IMethod, IConnectionOpen
    {
        public const ushort ClassId = 10;
        public const ushort MethodId = 40;

        private string m_virtualHost;
        private string m_capabilities;
        private bool m_insist;

        public string VirtualHost { get { return m_virtualHost; } }
        public string Capabilities { get { return m_capabilities; } }
        public bool Insist { get { return m_insist; } }

        public ConnectionOpen(){ }
        public ConnectionOpen(
          string initVirtualHost,
          string initCapabilities,
          bool initInsist)
        {
            m_virtualHost = initVirtualHost;
            m_capabilities = initCapabilities;
            m_insist = initInsist;
        }

        public ushort ProtocolClassId { get { return 10; } }
        public ushort ProtocolMethodId { get { return 40; } }
        public string ProtocolMethodName { get { return "connection.open"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_virtualHost = reader.ReadShortString();
            m_capabilities = reader.ReadShortString();
            m_insist = Convert.ToBoolean(reader.ReadByte());
        }

        public void WriteArgumentsTo(FrameBuilder writer)
        {
            writer.WriteShortString(m_virtualHost);
            writer.WriteShortString(m_capabilities);
            writer.WriteByte(System.Convert.ToByte(m_insist));
        }
        public void WriteArgumentsTo(ref Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteShortString(ref writer, m_virtualHost, out int written1);
            NetworkBinaryWriter1.WriteShortString(ref writer, m_capabilities, out int written2);
            NetworkBinaryWriter1.WriteByte(ref writer, System.Convert.ToByte(m_insist), out int written3);
            written = written1 + written2 + written3;
        }

        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_virtualHost); sb.Append(",");
            sb.Append(m_capabilities); sb.Append(",");
            sb.Append(m_insist);
            sb.Append(")");
        }
    }
}