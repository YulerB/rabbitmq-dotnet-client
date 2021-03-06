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
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using RabbitMQ.Client.Impl;
using System;
using System.Text;

namespace RabbitMQ.Client
{
    /// <summary>
    /// A decoded AMQP method frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// AMQP methods can be RPC requests, RPC responses, exceptions
    /// (ChannelClose, ConnectionClose), or one-way asynchronous
    /// messages. Currently this information is not recorded in their
    /// type or interface: it is implicit in the way the method is
    /// used, and the way it is defined in the AMQP specification. A
    /// future revision of the RabbitMQ .NET client library may extend
    /// the IMethod interface to represent this information
    /// explicitly.
    /// </para>
    /// </remarks>
    public interface IMethod
    {

        bool HasContent { get; }

        /// <summary>
        /// Retrieves the class ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        ushort ProtocolClassId { get; }

        /// <summary>
        /// Retrieves the method ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        ushort ProtocolMethodId { get; }

        /// <summary>
        /// Retrieves the name of this method - for debugging use.
        /// </summary>
        string ProtocolMethodName { get; }

        void AppendArgumentDebugStringTo(StringBuilder stringBuilder);
        void ReadArgumentsFrom(ArraySegmentSequence reader);
        void WriteArgumentsTo(Span<byte> writer, out int written);
        int EstimateSize();

        bool CompareClassAndMethod(int classId, int methodId);
    }
}
