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
    public class ChannelOpen : RabbitMQ.Client.Impl.MethodBase, IChannelOpen
    {
        public const ushort ClassId = 20;
        public const ushort MethodId = 10;

        public string m_reserved1;

        string IChannelOpen.Reserved1 { get { return m_reserved1; } }

        public ChannelOpen() { }
        public ChannelOpen(
          string initReserved1)
        {
            m_reserved1 = initReserved1;
        }

        public override ushort ProtocolClassId { get { return 20; } }
        public override ushort ProtocolMethodId { get { return 10; } }
        public override string ProtocolMethodName { get { return "channel.open"; } }
        public override bool HasContent { get { return false; } }

        public override void ReadArgumentsFrom(ArraySegmentSequence reader)
        {
            m_reserved1 = reader.ReadShortString();
        }

        public override void WriteArgumentsTo(FrameBuilder writer)
        {
            writer.WriteShortString(m_reserved1);
        }

        public override void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        {
            sb.Append("(");
            sb.Append(m_reserved1);
            sb.Append(")");
        }
    }
}