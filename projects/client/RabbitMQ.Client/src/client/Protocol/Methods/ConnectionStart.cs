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
    public struct ConnectionStart : IMethod, IConnectionStart
    {
        public const ushort ClassId = 10;
        public const ushort MethodId = 10;

        private byte m_versionMajor;
        private byte m_versionMinor;
        private System.Collections.Generic.IDictionary<string, object> m_serverProperties;
        private string m_mechanisms;
        private string m_locales;

        public byte VersionMajor { get { return m_versionMajor; } }
        public byte VersionMinor { get { return m_versionMinor; } }
        public System.Collections.Generic.IDictionary<string, object> ServerProperties { get { return m_serverProperties; } }
        public string Mechanisms { get { return m_mechanisms; } }
        public string Locales { get { return m_locales; } }

        public ConnectionStart(
          byte initVersionMajor,
          byte initVersionMinor,
          System.Collections.Generic.IDictionary<string, object> initServerProperties,
          string initMechanisms,
          string initLocales)
        {
            m_versionMajor = initVersionMajor;
            m_versionMinor = initVersionMinor;
            m_serverProperties = initServerProperties;
            m_mechanisms = initMechanisms;
            m_locales = initLocales;
        }

        public ushort ProtocolClassId { get { return 10; } }
        public ushort ProtocolMethodId { get { return 10; } }
        public string ProtocolMethodName { get { return "connection.start"; } }
        public bool HasContent { get { return false; } }

        public void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_versionMajor = reader.ReadByte();
            m_versionMinor = reader.ReadByte();
            m_serverProperties = reader.ReadTable();
            m_mechanisms = reader.ReadLongString();
            m_locales = reader.ReadLongString();
        }

        public void WriteArgumentsTo(FrameBuilder writer)
        {
            writer.WriteByte(m_versionMajor);
            writer.WriteByte(m_versionMinor);
            writer.WriteTable(m_serverProperties);
            writer.WriteLongString(m_mechanisms);
            writer.WriteLongString(m_locales);
        }

        public void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_versionMajor); sb.Append(",");
            sb.Append(m_versionMinor); sb.Append(",");
            sb.Append(m_serverProperties); sb.Append(",");
            sb.Append(m_mechanisms); sb.Append(",");
            sb.Append(m_locales);
            sb.Append(")");
        }
    }
}