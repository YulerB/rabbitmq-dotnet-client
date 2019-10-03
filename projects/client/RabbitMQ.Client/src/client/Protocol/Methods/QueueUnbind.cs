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
    public class QueueUnbind : IMethod, IQueueUnbind
    {
        public const ushort ClassId = 50;
        public const ushort MethodId = 50;

        private ushort m_reserved1;
        private string m_queue;
        private string m_exchange;
        private string m_routingKey;
        private System.Collections.Generic.IDictionary<string, object> m_arguments;

        public ushort Reserved1 { get { return m_reserved1; } }
        public string Queue { get { return m_queue; } }
        public string Exchange { get { return m_exchange; } }
        public string RoutingKey { get { return m_routingKey; } }
        public System.Collections.Generic.IDictionary<string, object> Arguments { get { return m_arguments; } }

        public QueueUnbind() { }
        public QueueUnbind(
          ushort initReserved1,
          string initQueue,
          string initExchange,
          string initRoutingKey,
          System.Collections.Generic.IDictionary<string, object> initArguments)
        {
            m_reserved1 = initReserved1;
            m_queue = initQueue;
            m_exchange = initExchange;
            m_routingKey = initRoutingKey;
            m_arguments = initArguments;
        }

        public ushort ProtocolClassId { get { return 50; } }
        public ushort ProtocolMethodId { get { return 50; } }
        public string ProtocolMethodName { get { return "queue.unbind"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_reserved1 = reader.ReadUInt16();
            m_queue = reader.ReadShortString();
            m_exchange = reader.ReadShortString();
            m_routingKey = reader.ReadShortString();
            m_arguments = reader.ReadTable();
        }

        public void WriteArgumentsTo(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt16(writer, m_reserved1, out int written1);
            NetworkBinaryWriter1.WriteShortString(writer.Slice(written1), m_queue, out int written2);
            NetworkBinaryWriter1.WriteShortString(writer.Slice(written1+ written2), m_exchange, out int written3);
            NetworkBinaryWriter1.WriteShortString(writer.Slice(written1 + written2 + written3), m_routingKey, out int written4);
            NetworkBinaryWriter1.WriteTable(writer.Slice(written1 + written2 + written3 + written4), m_arguments, out int written5);
            written = written1 + written2 + written3 + written4 + written5;
        }
        public int EstimateSize()
        {
            return 5 +
                System.Text.Encoding.UTF8.GetByteCount(m_queue) +
                System.Text.Encoding.UTF8.GetByteCount(m_exchange) +
                System.Text.Encoding.UTF8.GetByteCount(m_routingKey) +
                NetworkBinaryWriter1.EstimateTableSize(m_arguments);
        }
        
        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_reserved1); sb.Append(",");
            sb.Append(m_queue); sb.Append(",");
            sb.Append(m_exchange); sb.Append(",");
            sb.Append(m_routingKey); sb.Append(",");
            sb.Append(m_arguments);
            sb.Append(")");
        }
    }
}