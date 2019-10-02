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
    public struct ChannelOpenOk : IMethod, IChannelOpenOk
    {
        public const ushort ClassId = 20;
        public const ushort MethodId = 11;

        private string m_reserved1;

        public string Reserved1 { get { return m_reserved1; } }

        public ChannelOpenOk(
          string initReserved1)
        {
            m_reserved1 = initReserved1;
        }

        public ushort ProtocolClassId { get { return 20; } }
        public ushort ProtocolMethodId { get { return 11; } }
        public string ProtocolMethodName { get { return "channel.open-ok"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_reserved1 = reader.ReadLongString();
        }

        public void WriteArgumentsTo(FrameBuilder writer)
        {
            writer.WriteLongString(m_reserved1);
        }
        public void WriteArgumentsTo(ref Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteLongString(ref writer, m_reserved1, out int written1);
            written = written1;
        }
        public int EstimateSize()
        {
            return 4 +
                System.Text.Encoding.UTF8.GetByteCount(m_reserved1 );
        }
        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_reserved1);
            sb.Append(")");
        }
    }
}