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

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Impl;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RabbitMQ.Util
{
    /// <summary>
    /// Subclass of BinaryWriter that writes integers etc in correct network order.
    /// </summary>
    ///
    /// <remarks>
    /// <p>
    /// Kludge to compensate for .NET's broken little-endian-only BinaryWriter.
    /// </p><p>
    /// See also NetworkBinaryReader.
    /// </p>
    /// </remarks>
    public static class NetworkBinaryWriter 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBits1(this FrameBuilder output, bool[] bits)
        {
            int totalBits = Convert.ToInt32(16D * Math.Ceiling(bits.Length == 0 ? 1 : bits.Length / 15D));
            BitArray arr = new BitArray(totalBits);
            int kick = totalBits - 1;
            for (int i = 0; i < bits.Length; i++)
            {
                arr.Set(kick--, bits[i]);
                if (kick > 0 && kick % 16 == 0)
                {
                    arr.Set(kick--, true);
                }
            }

            byte[] bytes = new byte[arr.Count / 8];

            arr.CopyTo(bytes, 0);

            Array.Reverse(bytes);

            output.Write(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBits(this FrameBuilder output, bool[] bits)
        {
            int totalBits = Convert.ToInt32(16D * Math.Ceiling(bits.Length == 0 ? 1 : bits.Length / 15D));
            BitArray arr = new BitArray(totalBits);
            int kick = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                arr.Set(kick++, bits[i]);
                if (kick > 0 && kick % 15 == 0)
                {
                    arr.Set(kick++, true);
                }
            }

            byte[] bytes = new byte[arr.Count / 8];

            arr.CopyTo(bytes, 0);

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Reverse(bytes[i]);
            }

            output.Write(bytes);
        }
        private static byte Reverse(byte b)
        {
            int a = 0;
            for (int i = 0; i < 8; i++)
                if ((b & (1 << i)) != 0)
                    a |= 1 << (7 - i);
            return (byte)a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this FrameBuilder output, byte[] buffer)
        {
            output.Write(
                buffer,
                0,
                buffer.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this FrameBuilder output, byte[] buffer, int offset, int count)
        {
            output.Write(
                buffer,
                offset,
                count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(this FrameBuilder output, short i)
        {
            var bytes = new byte[2];
            BinaryPrimitives.WriteInt16BigEndian(bytes, i);
            output.Write(bytes,0,2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(this FrameBuilder output, ushort i)
        {
            var bytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, i);
            output.Write(bytes, 0, 2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(this FrameBuilder output, int i)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, i);
            output.Write(bytes, 0, 4);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(this FrameBuilder output, uint i)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, i);
            output.Write(bytes, 0, 4);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(this FrameBuilder output, long i)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(bytes, i);
            output.Write(bytes, 0, 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(this FrameBuilder output, ulong i)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, i);
            output.Write(bytes, 0, 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShortString(this FrameBuilder output, string val)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            if (bytes.Length > 255)
            {
                throw new WireFormattingException($"Short string too long; UTF-8 encoded length={bytes.Length}, max=255");
            }
            output.WriteByte((byte)bytes.Length);
            output.Write(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(this FrameBuilder output, float f)
        {
            var bytes = BitConverter.GetBytes(f);
            output.Write(
                new byte[4]{
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                },
                0,
                4);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this FrameBuilder output, double d)
        {
            var bytes = BitConverter.GetBytes(d);
            output.Write(
                new byte[8]{
                    bytes[7],
                    bytes[6],
                    bytes[5],
                    bytes[4],
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                },
                0,
                8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLongstr(this FrameBuilder output, byte[] val)
        {
            output.WriteUInt32((uint)val.Length);
            output.Write(val);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLongString(this FrameBuilder output, string val)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            output.WriteUInt32((uint)bytes.Length);
            output.Write(bytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(this FrameBuilder output, sbyte val)
        {
            output.WriteByte((byte)val);
        }
        ///<summary>Writes an AMQP "table" to the writer.</summary>
        ///<remarks>
        ///<para>
        /// In this method, we assume that the stream that backs our
        /// NetworkBinaryWriter is a positionable stream - which it is
        /// currently (see Frame.m_accumulator, Frame.GetWriter and
        /// Command.Transmit).
        ///</para>
        ///<para>
        /// Supports the AMQP 0-8/0-9 standard entry types S, I, D, T
        /// and F, as well as the QPid-0-8 specific b, d, f, l, s, t
        /// x and V types and the AMQP 0-9-1 A type.
        ///</para>
        ///</remarks>
        private static IList<ArraySegment<byte>> GetTableContent(IDictionary<string, object> val, out uint written)
        {
            var stream1 = new FrameBuilder(val.Count * 4);
            foreach (var entry in val)
            {
                stream1.WriteShortString(entry.Key);
                stream1.WriteFieldValue(entry.Value);
            }
            written = Convert.ToUInt32(stream1.Length);
            return stream1.ToData();
        }
        private static IList<ArraySegment<byte>> GetTableContent(IDictionary<string, bool> val, out uint written)
        {
            var stream1 = new FrameBuilder(val.Count * 4);
            foreach (var entry in val)
            {
                stream1.WriteShortString(entry.Key);
                stream1.WriteFieldValue(entry.Value);
            }
            written = Convert.ToUInt32(stream1.Length);
            return stream1.ToData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTimestamp(this FrameBuilder output, AmqpTimestamp val)
        {
            // 0-9 is afaict silent on the signedness of the timestamp.
            // See also MethodArgumentReader.ReadTimestamp and AmqpTimestamp itself
            output.WriteUInt64((ulong)val.UnixTime);
        }

        ///<summary>Writes an AMQP "table" to the writer.</summary>
        ///<remarks>
        ///<para>
        /// In this method, we assume that the stream that backs our
        /// NetworkBinaryWriter is a positionable stream - which it is
        /// currently (see Frame.m_accumulator, Frame.GetWriter and
        /// Command.Transmit).
        ///</para>
        ///<para>
        /// Supports the AMQP 0-8/0-9 standard entry types S, I, D, T
        /// and F, as well as the QPid-0-8 specific b, d, f, l, s, t
        /// x and V types and the AMQP 0-9-1 A type.
        ///</para>
        ///</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTable(this FrameBuilder output, IDictionary<string, object> val)
        {
            if (val == null)
            {
                output.WriteUInt32(0U);
            }
            else
            {
                var content = GetTableContent(val, out uint written1);
                output.WriteUInt32(written1);
                output.WriteSegments(content, written1);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTable(this FrameBuilder output, IDictionary<string, bool> val)
        {
            if (val == null)
            {
                output.WriteUInt32(0U);
            }
            else
            {
                var content = GetTableContent(val, out uint written1);
                output.WriteUInt32(written1);
                output.WriteSegments(content, written1);
            }
        }

        private static void DecimalToAmqp(decimal value, out byte scale, out int mantissa)
        {
            // According to the documentation :-
            //  - word 0: low-order "mantissa"
            //  - word 1, word 2: medium- and high-order "mantissa"
            //  - word 3: mostly reserved; "exponent" and sign bit
            // In one way, this is broader than AMQP: the mantissa is larger.
            // In another way, smaller: the exponent ranges 0-28 inclusive.
            // We need to be careful about the range of word 0, too: we can
            // only take 31 bits worth of it, since the sign bit needs to
            // fit in there too.
            int[] bitRepresentation = decimal.GetBits(value);
            if (bitRepresentation[1] != 0 || // mantissa extends into middle word
                bitRepresentation[2] != 0 || // mantissa extends into top word
                bitRepresentation[0] < 0) // mantissa extends beyond 31 bits
            {
                throw new WireFormattingException("Decimal overflow in AMQP encoding", value);
            }
            scale = (byte)((((uint)bitRepresentation[3]) >> 16) & 0xFF);
            mantissa = (int)((((uint)bitRepresentation[3]) & 0x80000000) |
                             (((uint)bitRepresentation[0]) & 0x7FFFFFFF));
        }

        private static IList<ArraySegment<byte>> GetArrayContent(IList val, out uint written)
        {
            var stream1 = new FrameBuilder(val.Count * 4);
            foreach (object entry in val)
            {
                stream1.WriteFieldValue(entry);
            }
            written = Convert.ToUInt32(stream1.Length);
            return stream1.ToData();
        }

        private static void WriteArray(this FrameBuilder output, IList val)
        {
            if (val == null)
            {
                output.WriteUInt32(0U); // length of table - will be backpatched
            }
            else
            {
                var content = GetArrayContent(val, out uint written1);
                output.WriteUInt32(written1); // length of table - will be backpatched
                output.WriteSegments(content, written1);
            }
        }

        private static void WriteDecimal(this FrameBuilder output, decimal value)
        {
            DecimalToAmqp(value, out byte scale, out int mantissa);
            output.WriteByte(scale);
            output.WriteUInt32((uint)mantissa);
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

        private static void WriteFieldValue(this FrameBuilder output, object value)
        {
            if (value == null)
            {
                output.WriteByte(V);
            }
            else if (value is string)
            {
                output.WriteByte(S);
                output.WriteLongstr(Encoding.UTF8.GetBytes(value as string));
            }
            else if (value is byte[])
            {
                output.WriteByte(S);
                output.WriteLongstr(value as byte[]);
            }
            else if (value is int)
            {
                output.WriteByte(I);
                output.WriteInt32((int)value);
            }
            else if (value is decimal)
            {
                output.WriteByte(D);
                output.WriteDecimal((decimal)value);
            }
            else if (value is AmqpTimestamp)
            {
                output.WriteByte(T);
                output.WriteTimestamp((AmqpTimestamp) value);
            }
            else if (value is IDictionary<string, bool>)
            {
                output.WriteByte(F);
                output.WriteTable(value as IDictionary<string, bool>);
            }
            else if (value is IDictionary)
            {
                output.WriteByte(F);
                output.WriteTable(value as IDictionary<string, object>);
            }
            else if (value is IList)
            {
                output.WriteByte(A);
                output.WriteArray(value as IList);
            }
            else if (value is sbyte)
            {
                output.WriteByte(b);
                output.WriteSByte((sbyte)value);
            }
            else if (value is double)
            {
                output.WriteByte(d);
                output.WriteDouble((double)value);
            }
            else if (value is float)
            {
                output.WriteByte(f);
                output.WriteFloat((float)value);
            }
            else if (value is long)
            {
                output.WriteByte(l);
                output.WriteInt64((long)value);
            }
            else if (value is ulong)
            {
                output.WriteByte(l);
                output.WriteUInt64((ulong)value);
            }
            else if (value is uint)
            {
                output.WriteByte(I);
                output.WriteUInt32((uint)value);
            }
            else if (value is short)
            {
                output.WriteByte(s);
                output.WriteInt16((short)value);
            }
            else if (value is ushort)
            {
                output.WriteByte(s);
                output.WriteUInt16((ushort)value);
            }
            else if (value is bool)
            {
                output.WriteByte(t);
                output.WriteByte((byte)(((bool)value) ? 1 : 0));
            }
            else if (value is BinaryTableValue)
            {
                output.WriteByte(x);
                output.Write(((BinaryTableValue)value).Bytes);
            }
            else
            {
                throw new WireFormattingException("Value cannot appear as table value", value);
            }
        }
    }
}

