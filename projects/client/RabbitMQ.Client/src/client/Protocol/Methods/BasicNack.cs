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
    public class BasicNack : IMethod, IBasicNack
    {
        public const ushort ClassId = 60;
        public const ushort MethodId = 120;

        private ulong m_deliveryTag;
        private BasicNackFlags m_settings;

        public ulong DeliveryTag { get { return m_deliveryTag; } }
        public BasicNackFlags Settings { get {return m_settings; } }

        public BasicNack() { }

        public BasicNack(
          ulong initDeliveryTag,
          BasicNackFlags settings)
        {
            m_deliveryTag = initDeliveryTag;
            m_settings = settings;
        }

        public ushort ProtocolClassId { get { return 60; } }
        public ushort ProtocolMethodId { get { return 120; } }
        public string ProtocolMethodName { get { return "basic.nack"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_deliveryTag = reader.ReadUInt64();
            m_settings= (BasicNackFlags) reader.ReadByte();
        }


        public void WriteArgumentsTo(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt64(writer, m_deliveryTag, out int written1);
            NetworkBinaryWriter1.WriteByte(writer.Slice(written1), (byte)m_settings, out int written2);
            written = written1 + written2;
        }
        public int EstimateSize()
        {
            return 9;
        }
        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_deliveryTag); sb.Append(",");
            sb.Append(m_settings); 
            sb.Append(")");
        }
    }
}