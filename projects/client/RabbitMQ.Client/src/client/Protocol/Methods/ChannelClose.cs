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
    public class ChannelClose : IMethod, IChannelClose
    {
        public const ushort ClassId = 20;
        public const ushort MethodId = 40;

        private ushort m_replyCode;
        private string m_replyText;
        private ushort m_classId;
        private ushort m_methodId;

        public ushort ReplyCode { get { return m_replyCode; } }
        public string ReplyText { get { return m_replyText; } }

        public ChannelClose() { }
        public ChannelClose(
          ushort initReplyCode,
          string initReplyText,
          ushort initClassId,
          ushort initMethodId)
        {
            m_replyCode = initReplyCode;
            m_replyText = initReplyText;
            m_classId = initClassId;
            m_methodId = initMethodId;
        }

        public ChannelClose(ushort initReplyCode, string initReplyText)
        {
            m_replyCode = initReplyCode;
            m_replyText = initReplyText;
        }


        public ushort ProtocolClassId { get { return 20; } }
        public ushort ProtocolMethodId { get { return 40; } }
        public string ProtocolMethodName { get { return "channel.close"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_replyCode = reader.ReadUInt16();
            m_replyText = reader.ReadShortString();
            m_classId = reader.ReadUInt16();
            m_methodId = reader.ReadUInt16();
        }
        public void WriteArgumentsTo(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt16(writer, m_replyCode, out int written1);
            NetworkBinaryWriter1.WriteShortString(writer.Slice(written1), m_replyText, out int written2);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1 + written2), m_classId, out int written3);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1 + written2 + written3), m_methodId, out int written4);
            written = written1 + written2 + written3 + written4;
        }
        public int EstimateSize()
        {
            return 7 +
               System.Text.Encoding.UTF8.GetByteCount(m_replyText );
        }
        public bool CompareClassAndMethod(int classId, int methodId){return ClassId == classId && MethodId == methodId;}  public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_replyCode); sb.Append(",");
            sb.Append(m_replyText); sb.Append(",");
            sb.Append(m_classId); sb.Append(",");
            sb.Append(m_methodId);
            sb.Append(")");
        }
    }
}