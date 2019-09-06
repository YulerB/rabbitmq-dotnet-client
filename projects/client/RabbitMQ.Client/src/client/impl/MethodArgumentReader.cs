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

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Impl
{
    public class MethodArgumentReader
    {
        private int m_bit;
        private int m_bits;
        private readonly NetworkBinaryReader BaseReader;

        public MethodArgumentReader(NetworkBinaryReader reader)
        {
            BaseReader = reader;
            ClearBits();
        }


        public virtual bool ReadBit()
        {
            if (m_bit > 0x80)
            {
                m_bits = BaseReader.ReadByte();
                m_bit = 0x01;
            }

            bool result = (m_bits & m_bit) != 0;
            m_bit = m_bit << 1;
            return result;
        }

        public virtual uint ReadLong()
        {
            ClearBits();
            return WireFormatting.ReadLong(BaseReader);
        }

        public virtual ulong ReadLonglong()
        {
            ClearBits();
            return WireFormatting.ReadLonglong(BaseReader);
        }

        public virtual byte[] ReadLongstr()
        {
            ClearBits();
            return WireFormatting.ReadLongstr(BaseReader);
        }

        public virtual byte ReadOctet()
        {
            ClearBits();
            return WireFormatting.ReadOctet(BaseReader);
        }

        public virtual ushort ReadShort()
        {
            ClearBits();
            return WireFormatting.ReadShort(BaseReader);
        }

        public virtual string ReadShortstr()
        {
            ClearBits();
            return WireFormatting.ReadShortstr(BaseReader);
        }

        public virtual IDictionary<string, object> ReadTable()
        {
            ClearBits();
            return WireFormatting.ReadTable(BaseReader);
        }

        public virtual AmqpTimestamp ReadTimestamp()
        {
            ClearBits();
            return WireFormatting.ReadTimestamp(BaseReader);
        }

        private void ClearBits()
        {
            m_bits = 0;
            m_bit = 0x100;
        }

        // TODO: Consider using NotImplementedException (?)
        // This is a completely bizarre consequence of the way the
        // Message.Transfer method is marked up in the XML spec.
    }
    public class MethodArgumentReader2 : MethodArgumentReader
    {
        private int m_bit;
        private int m_bits;
        private readonly NetworkArraySegmentsReader BaseReader;

        public MethodArgumentReader2(NetworkArraySegmentsReader reader) : base(null)
        {
            BaseReader = reader;
            ClearBits();
        }


        public override bool ReadBit()
        {
            if (m_bit > 0x80)
            {
                m_bits = BaseReader.ReadByte();
                m_bit = 0x01;
            }

            bool result = (m_bits & m_bit) != 0;
            m_bit = m_bit << 1;
            return result;
        }

        public override uint ReadLong()
        {
            ClearBits();
            return BaseReader.ReadUInt32();
        }

        public override ulong ReadLonglong()
        {
            ClearBits();
            return BaseReader.ReadUInt64();
        }

        public override byte[] ReadLongstr()
        {
            ClearBits();
            uint byteCount = BaseReader.ReadUInt32();
            if (byteCount > int.MaxValue)
            {
                throw new SyntaxError("Long string too long; " +
                                      "byte length=" + byteCount + ", max=" + int.MaxValue);
            }
            return BaseReader.ReadMemory((int)byteCount).ToArray();
        }

        public override byte ReadOctet()
        {
            ClearBits();
            return BaseReader.ReadByte();
        }
  
        public override ushort ReadShort()
        {
            ClearBits();
            return BaseReader.ReadUInt16();
        }

        public override string ReadShortstr()
        {
            ClearBits();
            int byteCount = BaseReader.ReadByte();
            var readBytes = BaseReader.ReadMemory(byteCount);
            return System.Text.Encoding.UTF8.GetString(readBytes.ToArray());
        }

        public override IDictionary<string, object> ReadTable()
        {
            ClearBits();
            return BaseReader.ReadTable(out long read);
        }

        public override AmqpTimestamp ReadTimestamp()
        {
            ClearBits();
            ulong stamp = BaseReader.ReadUInt64();
            // 0-9 is afaict silent on the signedness of the timestamp.
            // See also MethodArgumentWriter.WriteTimestamp and AmqpTimestamp itself
            return new AmqpTimestamp((long)stamp);
        }

        private void ClearBits()
        {
            m_bits = 0;
            m_bit = 0x100;
        }
    }
}
