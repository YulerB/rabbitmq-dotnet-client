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
    public class BasicQos : IMethod, IBasicQos
    {
        public const ushort ClassId = 60;
        public const ushort MethodId = 10;

        private uint m_prefetchSize;
        private ushort m_prefetchCount;
        private bool m_global;

        public uint PrefetchSize { get { return m_prefetchSize; } }
        public ushort PrefetchCount { get { return m_prefetchCount; } }
        public bool Global { get { return m_global; } }

        public BasicQos() { }

        public BasicQos(
          uint initPrefetchSize,
          ushort initPrefetchCount,
          bool initGlobal)
        {
            m_prefetchSize = initPrefetchSize;
            m_prefetchCount = initPrefetchCount;
            m_global = initGlobal;
        }

        public ushort ProtocolClassId { get { return 60; } }
        public ushort ProtocolMethodId { get { return 10; } }
        public string ProtocolMethodName { get { return "basic.qos"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_prefetchSize = reader.ReadUInt32();
            m_prefetchCount = reader.ReadUInt16();
            m_global = Convert.ToBoolean(reader.ReadByte());
        }
        
        public void WriteArgumentsTo(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt32(writer, m_prefetchSize, out int written1);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1), m_prefetchCount, out int written2);
            NetworkBinaryWriter1.WriteByte(writer.Slice(written1 + written2), Convert.ToByte(m_global), out int written3);
            written = written1 + written2 + written3;
        }
        public int EstimateSize()
        {
            return 7;
        }
        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_prefetchSize); sb.Append(",");
            sb.Append(m_prefetchCount); sb.Append(",");
            sb.Append(m_global);
            sb.Append(")");
        }
    }
}