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
    public class ConnectionStartOk : IMethod, IConnectionStartOk
    {
        public const ushort ClassId = 10;
        public const ushort MethodId = 11;

        private System.Collections.Generic.IDictionary<string, object> m_clientProperties;
        private string m_mechanism;
        private string m_response;
        private string m_locale;

        public System.Collections.Generic.IDictionary<string, object> ClientProperties { get { return m_clientProperties; } }
        public string Mechanism { get { return m_mechanism; } }
        public string Response { get { return m_response; } }
        public string Locale { get { return m_locale; } }
        public ConnectionStartOk() { }

        public ConnectionStartOk(
          System.Collections.Generic.IDictionary<string, object> initClientProperties,
          string initMechanism,
          string initResponse,
          string initLocale)
        {
            m_clientProperties = initClientProperties;
            m_mechanism = initMechanism;
            m_response = initResponse;
            m_locale = initLocale;
        }

        public ushort ProtocolClassId { get { return 10; } }
        public ushort ProtocolMethodId { get { return 11; } }
        public string ProtocolMethodName { get { return "connection.start-ok"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_clientProperties = reader.ReadTable();
            m_mechanism = reader.ReadShortString();
            m_response = reader.ReadLongString();
            m_locale = reader.ReadShortString();
        }

        public void WriteArgumentsTo(FrameBuilder writer)
        {
            writer.WriteTable(m_clientProperties);
            writer.WriteShortString(m_mechanism);
            writer.WriteLongString(m_response);
            writer.WriteShortString(m_locale);
        }
        public void WriteArgumentsTo(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteTable(writer, m_clientProperties, out int written1);
            NetworkBinaryWriter1.WriteShortString(writer.Slice(written1), m_mechanism, out int written2);
            NetworkBinaryWriter1.WriteLongString(writer.Slice(written1+ written2), m_response, out int written3);
            NetworkBinaryWriter1.WriteShortString(writer.Slice(written1 + written2 + written3), m_locale, out int written4);
            written = written1 + written2 + written3 + written4;
        }
        public int EstimateSize()
        {
            return 6 +
                System.Text.Encoding.UTF8.GetByteCount(m_mechanism) +
                System.Text.Encoding.UTF8.GetByteCount(m_response) +
                System.Text.Encoding.UTF8.GetByteCount(m_locale) +
                NetworkBinaryWriter1.EstimateTableSize(m_clientProperties);
        }
        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_clientProperties); sb.Append(",");
            sb.Append(m_mechanism); sb.Append(",");
            sb.Append(m_response); sb.Append(",");
            sb.Append(m_locale);
            sb.Append(")");
        }
    }
}