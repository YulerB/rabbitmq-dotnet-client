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
using System.IO;
using System.Text;
using System.Threading;
using System.Buffers.Binary;
using RabbitMQ.Client;
using System.Collections.Generic;

namespace RabbitMQ.Util
{
    /// <summary>
    /// Subclass of BinaryReader that reads integers etc in correct network order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kludge to compensate for .NET's broken little-endian-only BinaryReader.
    /// Relies on BinaryReader always being little-endian.
    /// </para>
    /// </remarks>
    public class NetworkBinaryReader : BinaryReader
    {
        // Not particularly efficient. To be more efficient, we could
        // reuse BinaryReader's implementation details: m_buffer and
        // FillBuffer, if they weren't private
        // members. Private/protected claim yet another victim, film
        // at 11. (I could simply cut-n-paste all that good code from
        // BinaryReader, but two wrongs do not make a right)

        /// <summary>
        /// Construct a NetworkBinaryReader over the given input stream.
        /// </summary>
        public NetworkBinaryReader(Stream input) : base(input)
        {
        }
        
        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override double ReadDouble()
        {
            byte[] bytes = ReadBytes(8);
            byte temp = bytes[0];
            bytes[0] = bytes[7];
            bytes[7] = temp;
            temp = bytes[1];
            bytes[1] = bytes[6];
            bytes[6] = temp;
            temp = bytes[2];
            bytes[2] = bytes[5];
            bytes[5] = temp;
            temp = bytes[3];
            bytes[3] = bytes[4];
            bytes[4] = temp;
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override short ReadInt16()
        {
            uint i = base.ReadUInt16();
            return (short)(((i & 0xFF00) >> 8) |
                           ((i & 0x00FF) << 8));
        }

        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override int ReadInt32()
        {
            uint i = base.ReadUInt32();
            return (int)(((i & 0xFF000000) >> 24) |
                         ((i & 0x00FF0000) >> 8) |
                         ((i & 0x0000FF00) << 8) |
                         ((i & 0x000000FF) << 24));
        }

        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override long ReadInt64()
        {
            ulong i = base.ReadUInt64();
            return (long)(((i & 0xFF00000000000000) >> 56) |
                          ((i & 0x00FF000000000000) >> 40) |
                          ((i & 0x0000FF0000000000) >> 24) |
                          ((i & 0x000000FF00000000) >> 8) |
                          ((i & 0x00000000FF000000) << 8) |
                          ((i & 0x0000000000FF0000) << 24) |
                          ((i & 0x000000000000FF00) << 40) |
                          ((i & 0x00000000000000FF) << 56));
        }

        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override float ReadSingle()
        {
            byte[] bytes = ReadBytes(4);
            byte temp = bytes[0];
            bytes[0] = bytes[3];
            bytes[3] = temp;
            temp = bytes[1];
            bytes[1] = bytes[2];
            bytes[2] = temp;
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override ushort ReadUInt16()
        {
            uint i = base.ReadUInt16();
            return (ushort)(((i & 0xFF00) >> 8) |
                            ((i & 0x00FF) << 8));
        }

        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override uint ReadUInt32()
        {
            uint i = base.ReadUInt32();
            return (((i & 0xFF000000) >> 24) |
                    ((i & 0x00FF0000) >> 8) |
                    ((i & 0x0000FF00) << 8) |
                    ((i & 0x000000FF) << 24));
        }

        /// <summary>
        /// Override BinaryReader's method for network-order.
        /// </summary>
        public override ulong ReadUInt64()
        {
            ulong i = base.ReadUInt64();
            return (((i & 0xFF00000000000000) >> 56) |
                    ((i & 0x00FF000000000000) >> 40) |
                    ((i & 0x0000FF0000000000) >> 24) |
                    ((i & 0x000000FF00000000) >> 8) |
                    ((i & 0x00000000FF000000) << 8) |
                    ((i & 0x0000000000FF0000) << 24) |
                    ((i & 0x000000000000FF00) << 40) |
                    ((i & 0x00000000000000FF) << 56));
        }
    }

    public class NetworkArraySegmentsReader 
    {
        private readonly ArraySegmentStream input;

        public NetworkArraySegmentsReader(ArraySegmentStream input) 
        {
            this.input = input;
        }

        public byte ReadByte()
        {
            var data = input.Read(1);
            return data[0].Span[0];
        }
        public byte[] ReadBytes(int payloadSize)
        {
            var data = input.Read(payloadSize);
            if(data.Length == 1)
            {
                byte[] bytes = new byte[payloadSize];
                Memory<byte> memory = new Memory<byte>(bytes);
                data[0].CopyTo(memory);
                return bytes;
            }
            else
            {
                byte[] bytes = new byte[payloadSize];
                int offset = 0;
                foreach (var segment in data)
                {
                    var arr = segment.ToArray();
                    Buffer.BlockCopy(arr, 0, bytes, offset, segment.Length);
                    offset += segment.Length;
                }
                return bytes;
            }
        }
        public ReadOnlyMemory<byte> ReadMemory(int payloadSize)
        {
            var data = input.Read(payloadSize);
            if (data.Length == 1)
            {
                return data[0];
            }
            else
            {
                byte[] bytes = new byte[payloadSize];
                int offset = 0;
                foreach (var segment in data)
                {
                    var arr = segment.ToArray();
                    Buffer.BlockCopy(arr, 0, bytes, offset, segment.Length);
                    offset += segment.Length;
                }
                return new ReadOnlyMemory<byte>(bytes);
            }
        }
        public ushort ReadUInt16()
        {
            var data = input.Read(2);

            if (data.Length == 1)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(data[0].Span);
            }


            var arrayIndex = 0;
            var offset = 0;
            byte[] bytes = new byte[2];


            var count = data[arrayIndex].Length;
            for (int i = 1; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToUInt16(bytes, 0);
        }
        public uint ReadUInt32()
        {
            var data = input.Read(4);
            if (data.Length == 1)
            {
                return BinaryPrimitives.ReadUInt32BigEndian(data[0].Span);
            }

            byte[] bytes = new byte[4];
            var arrayIndex = 0;
            var offset = 0;
            var count = data[arrayIndex].Length;
            for (int i = 3; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToUInt32(bytes, 0);
        }
        public ulong ReadUInt64()
        {
            var data = input.Read(8);
            if (data.Length == 1)
            {
                return BinaryPrimitives.ReadUInt64BigEndian(data[0].Span);
            }

            byte[] bytes = new byte[8];
            var arrayIndex = 0;
            var offset = 0;
            var count = data[arrayIndex].Length;
            for (int i = 7; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToUInt64(bytes, 0);
        }
        public short ReadInt16()
        {
            var data = input.Read(2);

            if (data.Length == 1)
            {
                return BinaryPrimitives.ReadInt16BigEndian(data[0].Span);
            }


            var arrayIndex = 0;
            var offset = 0;
            byte[] bytes = new byte[2];


            var count = data[arrayIndex].Length;
            for (int i = 1; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToInt16(bytes, 0);
        }
        public int ReadInt32()
        {
            var data = input.Read(4);
            if (data.Length == 1)
            {
                return BinaryPrimitives.ReadInt32BigEndian(data[0].Span);
            }

            byte[] bytes = new byte[4];
            var arrayIndex = 0;
            var offset = 0;
            var count = data[arrayIndex].Length;
            for (int i = 3; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToInt32(bytes, 0);
        }
        public long ReadInt64()
        {
            var data = input.Read(8);
            if (data.Length == 1)
            {
                return BinaryPrimitives.ReadInt64BigEndian(data[0].Span);
            }

            byte[] bytes = new byte[8];
            var arrayIndex = 0;
            var offset = 0;
            var count = data[arrayIndex].Length;
            for (int i = 7; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToInt64(bytes, 0);
        }
        public float ReadSingle()
        {
            var data = input.Read(4);
            if (data.Length == 1)
            {
                return (float) BinaryPrimitives.ReadUInt32BigEndian(data[0].Span);
            }

            byte[] bytes = new byte[4];
            var arrayIndex = 0;
            var offset = 0;
            var count = data[arrayIndex].Length;
            for (int i = 3; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToSingle(bytes, 0);
        }
        public double ReadDouble()
        {
            var data = input.Read(8);
            if (data.Length == 1)
            {
                return (double)BinaryPrimitives.ReadUInt64BigEndian(data[0].Span);
            }

            byte[] bytes = new byte[8];
            var arrayIndex = 0;
            var offset = 0;
            var count = data[arrayIndex].Length;
            for (int i = 7; i > -1; i--)
            {
                var segment = data[arrayIndex].ToArray();
                bytes[i] = segment[offset];
                offset++;
                count--;

                if (count == 0)
                {
                    arrayIndex++;
                    offset = 0;
                }
            }
            return BitConverter.ToDouble(bytes, 0);
        }
        public string ReadLongString(out long read)
        {
            int size = (int)ReadUInt32();
            read = size + 4;
            return System.Text.Encoding.UTF8.GetString(ReadMemory(size).ToArray());
        }
        public string ReadShortString(out long read)
        {
            int size = (int)ReadByte();
            read = size + 1;
            return Encoding.UTF8.GetString(ReadMemory(size).ToArray());
        }
        public decimal ReadDecimal(out long read)
        {
            byte scale = ReadByte();
            uint unsignedMantissa = ReadUInt32();
            read = 5;
            if (scale > 28)
            {
                throw new SyntaxError("Unrepresentable AMQP decimal table field: " +
                                      "scale=" + scale);
            }
            return new decimal((int)(unsignedMantissa & 0x7FFFFFFF),
                0,
                0,
                ((unsignedMantissa & 0x80000000) == 0) ? false : true,
                scale);
        }
        public AmqpTimestamp ReadTimestamp()
        {
            return new AmqpTimestamp(ReadInt64());
        }
        public IDictionary<string, object> ReadTable(out long read)
        {
            IDictionary<string, object> table = new Dictionary<string, object>();
            UInt32 tableLength = ReadUInt32();
            long left = tableLength;
            while (left> 0)
            {
                string key = ReadShortString(out long read1);
                left -= read1;
                object value = ReadFieldValue(out long read2);
                left -= read2;

                if (!table.ContainsKey(key))
                {
                    table[key] = value;
                }
            }
            read = tableLength + 4;
            return table;
        }
        public IList<object> ReadArray(out long read)
        {
            IList<object> array = new List<object>();
            long arrayLength = ReadUInt32();
            long left = arrayLength;
            while (left > 0)
            {
                array.Add(ReadFieldValue(out long read1));
                left -= read1;
            }
            read = arrayLength + 4;
            return array;
        }

        private const byte S = 83;
        private const byte T = 84;
        private const byte I = 73;
        private const byte D = 68;
        private const byte F = 70;
        private const byte A = 65;
        private const byte V = 86;
        private const byte b = 98;
        private const byte d = 100;
        private const byte f = 102;
        private const byte l = 108;
        private const byte s = 115;
        private const byte t = 116;
        private const byte x = 120;
        public object ReadFieldValue(out long read)
        {
            byte discriminator = ReadByte();
            object value;
            switch (discriminator)
            {
                case S:
                    value =  ReadLongString(out read);
                    break;
                case I:
                    value = ReadInt32();
                    read = 4;
                    break;
                case D:
                    value = ReadDecimal(out read);
                    read = 4;
                    break;
                case T:
                    value = ReadTimestamp();
                    read = 8;
                    break;
                case F:
                    value = ReadTable(out read);
                    break;
                case A:
                    value = ReadArray(out read);
                    break;
                case b:
                    value = (sbyte)ReadByte();
                    read = 1;
                    break;
                case d:
                    value = ReadDouble();
                    read = 8;
                    break;
                case f:
                    value = ReadSingle();
                    read = 4;
                    break;
                case l:
                    value = ReadInt64();
                    read = 8;
                    break;
                case s:
                    value = ReadInt16();
                    read = 2;
                    break;
                case t:
                    value = (ReadByte() != 0);
                    read = 1;
                    break;
                case x:
                    int size = Convert.ToInt32(ReadUInt32()) ;
                    value = new BinaryTableValue(ReadMemory(size).ToArray());
                    read = 4 + size;
                    break;
                case V:
                    value = null;
                    read = 0;
                    break;
                default:
                    throw new SyntaxError("Unrecognised type in table: " + (char)discriminator);
            }
            read++;
            return value;
        }
    }
}
