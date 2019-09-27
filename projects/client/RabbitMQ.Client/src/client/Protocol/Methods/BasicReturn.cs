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

    /// <summary>Autogenerated type. Private implementation class - do not use directly.</summary>
    public struct BasicReturn : IMethod, IBasicReturn
    {
        public const ushort ClassId = 60;
        public const ushort MethodId = 50;

        private ushort m_replyCode;
        private string m_replyText;
        private string m_exchange;
        private string m_routingKey;

        public ushort ReplyCode { get { return m_replyCode; } }
        public string ReplyText { get { return m_replyText; } }
        public string Exchange { get { return m_exchange; } }
        public string RoutingKey { get { return m_routingKey; } }

        public BasicReturn(
          ushort initReplyCode,
          string initReplyText,
          string initExchange,
          string initRoutingKey)
        {
            m_replyCode = initReplyCode;
            m_replyText = initReplyText;
            m_exchange = initExchange;
            m_routingKey = initRoutingKey;
        }

        public ushort ProtocolClassId { get { return 60; } }
        public ushort ProtocolMethodId { get { return 50; } }
        public string ProtocolMethodName { get { return "basic.return"; } }
        public bool HasContent { get { return true; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_replyCode = reader.ReadUInt16();
            m_replyText = reader.ReadShortString();
            m_exchange = reader.ReadShortString();
            m_routingKey = reader.ReadShortString();
        }

        public void WriteArgumentsTo(FrameBuilder writer)
        {
            writer.WriteUInt16(m_replyCode);
            writer.WriteShortString(m_replyText);
            writer.WriteShortString(m_exchange);
            writer.WriteShortString(m_routingKey);
        }

        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_replyCode); sb.Append(",");
            sb.Append(m_replyText); sb.Append(",");
            sb.Append(m_exchange); sb.Append(",");
            sb.Append(m_routingKey);
            sb.Append(")");
        }
    }
}