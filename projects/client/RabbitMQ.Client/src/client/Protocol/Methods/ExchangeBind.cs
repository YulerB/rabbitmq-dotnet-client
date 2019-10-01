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
    public class ExchangeBind : IMethod, IExchangeBind
    {
        public const ushort ClassId = 40;
        public const ushort MethodId = 30;

        private ushort m_reserved1;
        private string m_destination;
        private string m_source;
        private string m_routingKey;
        private bool m_nowait;
        private System.Collections.Generic.IDictionary<string, object> m_arguments;

        public ushort Reserved1 { get { return m_reserved1; } }
        public string Destination { get { return m_destination; } }
        public string Source { get { return m_source; } }
        public string RoutingKey { get { return m_routingKey; } }
        public bool Nowait { get { return m_nowait; } }
        public System.Collections.Generic.IDictionary<string, object> Arguments { get { return m_arguments; } }

        public ExchangeBind() { }
        public ExchangeBind(
          ushort initReserved1,
          string initDestination,
          string initSource,
          string initRoutingKey,
          bool initNowait,
          System.Collections.Generic.IDictionary<string, object> initArguments)
        {
            m_reserved1 = initReserved1;
            m_destination = initDestination;
            m_source = initSource;
            m_routingKey = initRoutingKey;
            m_nowait = initNowait;
            m_arguments = initArguments;
        }

        public ushort ProtocolClassId { get { return 40; } }
        public ushort ProtocolMethodId { get { return 30; } }
        public string ProtocolMethodName { get { return "exchange.bind"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_reserved1 = reader.ReadUInt16();
            m_destination = reader.ReadShortString();
            m_source = reader.ReadShortString();
            m_routingKey = reader.ReadShortString();
            m_nowait = Convert.ToBoolean(reader.ReadByte());
            m_arguments = reader.ReadTable();
        }

        public void WriteArgumentsTo(FrameBuilder writer)
        {
            writer.WriteUInt16(m_reserved1);
            writer.WriteShortString(m_destination);
            writer.WriteShortString(m_source);
            writer.WriteShortString(m_routingKey);
            writer.WriteByte(Convert.ToByte(m_nowait));
            writer.WriteTable(m_arguments);
        }

        public void WriteArgumentsTo(ref Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt16(ref writer, m_reserved1, out int written1);
            NetworkBinaryWriter1.WriteShortString(ref writer, m_destination, out int written2);
            NetworkBinaryWriter1.WriteShortString(ref writer, m_source, out int written3);
            NetworkBinaryWriter1.WriteShortString(ref writer, m_routingKey, out int written4);
            NetworkBinaryWriter1.WriteByte(ref writer, Convert.ToByte(m_nowait), out int written5);
            NetworkBinaryWriter1.WriteTable(ref writer, m_arguments, out int written6);
            written = written1 + written2 + written3 + written4 + written5 + written6;
        }

        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_reserved1); sb.Append(",");
            sb.Append(m_destination); sb.Append(",");
            sb.Append(m_source); sb.Append(",");
            sb.Append(m_routingKey); sb.Append(",");
            sb.Append(m_nowait); sb.Append(",");
            sb.Append(m_arguments);
            sb.Append(")");
        }
    }
}