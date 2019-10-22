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
using System.Runtime.CompilerServices;
using RabbitMQ.Client.Framing;

namespace RabbitMQ.Util
{
    public static class NetworkArraySegmentsReader 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(this ArraySegmentSequence input)
        {
            var data = input.Read(1);
            return data[ZERO].Span[ZERO];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadFirstByte(this ArraySegmentSequence input)
        {
            return input.Read(1)[ZERO].Span[ZERO];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBytes(this ArraySegmentSequence input,int payloadSize)
        {
            //Think of ways to remove memory copying

            var data = input.Read(payloadSize);
            if(data.Count == 1)
            {
                byte[] bytes = new byte[payloadSize];
                data[ZERO].CopyTo(bytes.AsMemory());
                return bytes;
            }
            else
            {
                byte[] bytes = new byte[payloadSize];
                int offset = ZERO;
                foreach (var segment in data)
                {
                    var arr = segment.ToArray();
                    Buffer.BlockCopy(arr, ZERO, bytes, offset, segment.Length);
                    offset += segment.Length;
                }
                return bytes;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<byte> ReadMemory(this ArraySegmentSequence input, int payloadSize)
        {
            var data = input.Read(payloadSize);
            if (data.Count == 1)
            {
                return data[ZERO];
            }
            else
            {
                //Think of ways to remove memory copying

                byte[] bytes = new byte[payloadSize];
                int offset = ZERO;
                foreach (var segment in data)
                {
                    var arr = segment.ToArray();
                    Buffer.BlockCopy(arr, ZERO, bytes, offset, segment.Length);
                    offset += segment.Length;
                }
                return new ReadOnlyMemory<byte>(bytes);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBytesMaybeZeroCopy(this ArraySegmentSequence input, int payloadSize)
        {
            var data = input.Read(payloadSize);
            if (data.Count == 1)
            {
                return data[ZERO].ToArray();
            }
            else
            {
                //Think of ways to remove memory copying
                byte[] bytes = new byte[payloadSize];
                int offset = ZERO;
                foreach (var segment in data)
                {
                    var arr = segment.ToArray();
                    Buffer.BlockCopy(arr, ZERO, bytes, offset, segment.Length);
                    offset += segment.Length;
                }
                return bytes;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this ArraySegmentSequence input)
        {
            var data = input.Read(2);

            if (data.Count == 1)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(data[ZERO].Span);
            }
            
            byte[] bytes = new byte[2]{
                data[ZERO].Span[ZERO],
                data[1].Span[ZERO]
            };

            return BinaryPrimitives.ReadUInt16BigEndian(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipUInt16(this ArraySegmentSequence input)
        {
            input.Skip(2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this ArraySegmentSequence input)
        {
            var data = input.Read(4);
            if (data.Count == 1)
            {
                return BinaryPrimitives.ReadUInt32BigEndian(data[ZERO].Span);
            }

            byte[] bytes = new byte[4];
            int index = ZERO;
            foreach(var item in data)
            {
                for (int i = ZERO; i < item.Length; i++)
                {
                    bytes[index++] = item.Span[i];
                }
            }

            return BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(this ArraySegmentSequence input)
        {
            var data = input.Read(8);
            if (data.Count == 1)
            {
                return BinaryPrimitives.ReadUInt64BigEndian(data[ZERO].Span);
            }

            byte[] bytes = new byte[8];
            int index = ZERO;
            foreach (var item in data)
            {
                for (int i = ZERO; i < item.Length; i++)
                {
                    bytes[index++] = item.Span[i];
                }
            }
            return BinaryPrimitives.ReadUInt64BigEndian(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this ArraySegmentSequence input)
        {
            var data = input.Read(2);

            if (data.Count == 1)
            {
                return BinaryPrimitives.ReadInt16BigEndian(data[ZERO].Span);
            }

            byte[] bytes = new byte[2]{
                data[ZERO].Span[ZERO],
                data[1].Span[ZERO]
            };
            return BinaryPrimitives.ReadInt16BigEndian(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this ArraySegmentSequence input)
        {
            var data = input.Read(4);
            if (data.Count == 1)
            {
                return BinaryPrimitives.ReadInt32BigEndian(data[ZERO].Span);
            }


            byte[] bytes = new byte[4];
            int index = ZERO;
            foreach (var item in data)
            {
                for (int i = ZERO; i < item.Length; i++)
                {
                    bytes[index++] = item.Span[i];
                }
            }
            return BinaryPrimitives.ReadInt32BigEndian(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this ArraySegmentSequence input)
        {
            var data = input.Read(8);
            if (data.Count == 1)
            {
                return BinaryPrimitives.ReadInt64BigEndian(data[ZERO].Span);
            }

            byte[] bytes = new byte[8];
            int index = ZERO;
            foreach (var item in data)
            {
                for (int i = ZERO; i < item.Length; i++)
                {
                    bytes[index++] = item.Span[i];
                }
            }
            return BinaryPrimitives.ReadInt64BigEndian(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingle(this ArraySegmentSequence input)
        {
            var data = input.Read(4);
            if (data.Count == 1)
            {
                return (float) BinaryPrimitives.ReadUInt32BigEndian(data[ZERO].Span);
            }

            byte[] bytes = new byte[4];
            int index = ZERO;
            foreach (var item in data)
            {
                for (int i = ZERO; i < item.Length; i++)
                {
                    bytes[index++] = item.Span[i];
                }
            }
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, ZERO);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(this ArraySegmentSequence input)
        {
            var data = input.Read(8);
            if (data.Count == 1)
            {
                return (double)BinaryPrimitives.ReadUInt64BigEndian(data[ZERO].Span);
            }

            byte[] bytes = new byte[8];
            int index = ZERO;
            foreach (var item in data)
            {
                for (int i = ZERO; i < item.Length; i++)
                {
                    bytes[index++] = item.Span[i];
                }
            }
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, ZERO);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadLongString(this ArraySegmentSequence input, out long read)
        {
            int size = Convert.ToInt32(ReadUInt32(input));
            read = size + 4;
            if (size == ZERO) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(ReadMemory(input,size).ToArray());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadLongString(this ArraySegmentSequence input)
        {
            int size = Convert.ToInt32(ReadUInt32(input));
            if (size == ZERO) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(ReadMemory(input, size).ToArray());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadShortString(this ArraySegmentSequence input,out long read)
        {
            int size = (int)ReadByte(input);
            read = size + 1;
            if (size == ZERO) return string.Empty;
            return Encoding.UTF8.GetString(ReadMemory(input,size).ToArray());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadShortString(this ArraySegmentSequence input)
        {
            int size = (int)ReadByte(input);
            if (size == ZERO) return string.Empty;
            return Encoding.UTF8.GetString(ReadMemory(input, size).ToArray());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal ReadDecimal(this ArraySegmentSequence input, out long read)
        {
            byte scale = ReadByte(input);
            if (scale > TwentyEight)
            {
                throw new SyntaxError("Unrepresentable AMQP decimal table field: scale=" + scale.ToString());
            }
            uint unsignedMantissa = ReadUInt32(input);
            read = 5;

            return new decimal((int)(unsignedMantissa & 0x7FFFFFFF),
                ZERO,
                ZERO,
                ((unsignedMantissa & 0x80000000) != UZERO),
                scale);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AmqpTimestamp ReadTimestamp(this ArraySegmentSequence input)
        {
            return new AmqpTimestamp(ReadInt64(input));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<string, object> ReadTable(this ArraySegmentSequence input, out long read)
        {
            uint tableLength = ReadUInt32(input);

            if(tableLength == UZERO)
            {
                read = 4;
                return null;
            }

            Dictionary<string, object> table = new Dictionary<string, object>(Convert.ToInt32(tableLength / 4));
            long left = tableLength;
            while (left> 0)
            {
                string key = ReadShortString(input,out long read1);
                object value = ReadFieldValue(input, out long read2);
                left -= read1+read2;

                if (!table.ContainsKey(key))
                {
                    table[key] = value;
                }
            }
            read = tableLength + 4U;
            return table;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<string, object> ReadTable(this ArraySegmentSequence input)
        {
            return ReadTable(input, out long read);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<object> ReadArray(this ArraySegmentSequence input, out long read)
        {
            uint arrayLength = ReadUInt32(input);
            if (arrayLength == UZERO)
            {
                read = 4;
                return null;
            }

            IList<object> array = new List<object>(Convert.ToInt32(arrayLength/3));
            long left = arrayLength;
            while (left > UZERO)
            {
                array.Add(ReadFieldValue(input,out long read1));
                left -= read1;
            }
            read = arrayLength + 4U;
            return array;
        }

        private const byte TwentyEight = 28;
        private const uint UZERO = 0U;
        private const int ZERO = 0;
        private const long LZERO = 0L;
        private const byte BZERO = default(byte);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object ReadFieldValue(this ArraySegmentSequence input, out long read)
        {
            byte discriminator = ReadByte(input);
            object value;
            switch (discriminator)
            {
                case S:
                    value =  ReadLongString(input,out read);
                    break;
                case I:
                    value = ReadInt32(input);
                    read = 4L;
                    break;
                case D:
                    value = ReadDecimal(input, out read);
                    read = 5L;
                    break;
                case T:
                    value = ReadTimestamp(input);
                    read = 8L;
                    break;
                case F:
                    value = ReadTable(input, out read);
                    break;
                case A:
                    value = ReadArray(input, out read);
                    break;
                case b:
                    value = (sbyte)ReadByte(input);
                    read = 1L;
                    break;
                case d:
                    value = ReadDouble(input);
                    read = 8L;
                    break;
                case f:
                    value = ReadSingle(input);
                    read = 4L;
                    break;
                case l:
                    value = ReadInt64(input);
                    read = 8L;
                    break;
                case s:
                    value = ReadInt16(input);
                    read = 2L;
                    break;
                case t:
                    value = (ReadByte(input) != BZERO);
                    read = 1L;
                    break;
                case x:
                    int size = Convert.ToInt32(ReadUInt32(input));
                    value = new BinaryTableValue(ReadMemory(input, size).ToArray());
                    read = 4L + size;
                    break;
                case V:
                    value = null;
                    read = LZERO;
                    break;
                default:
                    throw new SyntaxError("Unrecognised type in table: " + (char)discriminator);
            }
            read++;
            return value;
        }
    }
}
