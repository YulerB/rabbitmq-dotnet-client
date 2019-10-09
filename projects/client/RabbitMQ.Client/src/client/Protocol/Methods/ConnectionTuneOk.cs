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
    public class ConnectionTuneOk : IMethod, IConnectionTuneOk
    {
        public const ushort ClassId = 10;
        public const ushort MethodId = 31;

        private ushort m_channelMax;
        private uint m_frameMax;
        private ushort m_heartbeat;

        public ushort ChannelMax { get { return m_channelMax; } }
        public uint FrameMax { get { return m_frameMax; } }
        public ushort Heartbeat { get { return m_heartbeat; } }

        public ConnectionTuneOk() { }

        public ConnectionTuneOk(
          ushort initChannelMax,
          uint initFrameMax,
          ushort initHeartbeat)
        {
            m_channelMax = initChannelMax;
            m_frameMax = initFrameMax;
            m_heartbeat = initHeartbeat;
        }

        public ushort ProtocolClassId { get { return 10; } }
        public ushort ProtocolMethodId { get { return 31; } }
        public string ProtocolMethodName { get { return "connection.tune-ok"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_channelMax = reader.ReadUInt16();
            m_frameMax = reader.ReadUInt32();
            m_heartbeat = reader.ReadUInt16();
        }


        public void WriteArgumentsTo(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt16(writer, m_channelMax, out int written1);
            NetworkBinaryWriter1.WriteUInt32(writer.Slice(written1), m_frameMax, out int written2);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1 + written2), m_heartbeat, out int written3);
            written = written1 + written2 + written3;
        }
        public int EstimateSize()
        {
            return 8;
        }
        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_channelMax); sb.Append(",");
            sb.Append(m_frameMax); sb.Append(",");
            sb.Append(m_heartbeat);
            sb.Append(")");
        }
    }
}