//// This source code is dual-licensed under the Apache License, version
//// 2.0, and the Mozilla Public License, version 1.1.
////
//// The APL v2.0:
////
////---------------------------------------------------------------------------
////   Copyright (c) 2007-2016 Pivotal Software, Inc.
////
////   Licensed under the Apache License, Version 2.0 (the "License");
////   you may not use this file except in compliance with the License.
////   You may obtain a copy of the License at
////
////       http://www.apache.org/licenses/LICENSE-2.0
////
////   Unless required by applicable law or agreed to in writing, software
////   distributed under the License is distributed on an "AS IS" BASIS,
////   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
////   See the License for the specific language governing permissions and
////   limitations under the License.
////---------------------------------------------------------------------------
////
//// The MPL v1.1:
////
////---------------------------------------------------------------------------
////  The contents of this file are subject to the Mozilla Public License
////  Version 1.1 (the "License"); you may not use this file except in
////  compliance with the License. You may obtain a copy of the License
////  at http://www.mozilla.org/MPL/
////
////  Software distributed under the License is distributed on an "AS IS"
////  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
////  the License for the specific language governing rights and
////  limitations under the License.
////
////  The Original Code is RabbitMQ.
////
////  The Initial Developer of the Original Code is Pivotal Software, Inc.
////  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
////---------------------------------------------------------------------------

//using System;
//using System.Collections.Generic;
//using RabbitMQ.Util;

//namespace RabbitMQ.Client.Impl
//{
//    public class ContentHeaderPropertyReader2
//    {
//        private ArraySegmentSequence BaseReader;

//        public ContentHeaderPropertyReader2(ArraySegmentSequence reader)
//        {
//            BaseReader = reader;
//        }
        

//        public uint ReadUInt32()
//        {
//            return BaseReader.ReadUInt32();
//        }

//        public ulong ReadUInt64()
//        {
//            return BaseReader.ReadUInt64();
//        }

//        public string ReadLongstr()
//        {
//            return BaseReader.ReadLongString(out long read);
//        }

//        public byte ReadOctet()
//        {
//            return BaseReader.ReadByte();
//        }

//        public ushort ReadShort()
//        {
//            return BaseReader.ReadUInt16();
//        }

//        public string ReadShortstr()
//        {
//            return BaseReader.ReadShortString(out long read);
//        }

//        /// <returns>A type of <seealso cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.</returns>
//        public IDictionary<string, object> ReadTable()
//        {
//            return BaseReader.ReadTable(out long read);
//        }

//        public AmqpTimestamp ReadTimestamp()
//        {
//            return BaseReader.ReadTimestamp();
//        }
//    }
//}
