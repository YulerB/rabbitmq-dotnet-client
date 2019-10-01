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
        public static void WriteLongString(this FrameBuilder output, byte[] val)
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
            var stream1 = new FrameBuilder(val.Count * 2);
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

            //var data = new byte[5];
            //Span<byte> span = new Span<byte>(data);
            //span[0] = scale;
            //BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), (uint)mantissa);
            //output.Write(data, 0, 5);

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

        public static void WriteFieldValue(this FrameBuilder output, object value)
        {
            if (value == null)
            {
                output.WriteByte(V);
            }
            else if (value is string)
            {
                output.WriteByte(S);
                output.WriteLongString(Encoding.UTF8.GetBytes(value as string));
            }
            else if (value is byte[])
            {
                output.WriteByte(S);
                output.WriteLongString(value as byte[]);
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

    public static class NetworkBinaryWriter1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(this ref Span<byte> output, byte buffer, out int written)
        {
            output[0]=buffer;
            output = output.Slice(1);
            written = 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBits(this ref Span<byte> output, bool[] bits, out int written)
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

            bytes.AsSpan().CopyTo(output);
            output = output.Slice(bytes.Length);
            written = bytes.Length;
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
        public static void Write(this ref Span<byte> output, byte[] buffer, out int written)
        {
            buffer.AsSpan().CopyTo(output);
            output = output.Slice(buffer.Length);
            written = buffer.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this ref Span<byte> output, byte[] buffer, int offset, int count, out int written)
        {
            buffer.AsSpan().Slice(offset, count).CopyTo(output);
            output = output.Slice(count);
            written = count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(this ref Span<byte> output, short i, out int written)
        {
            BinaryPrimitives.WriteInt16BigEndian(output, i);
            output = output.Slice(2);
            written = 2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(this ref Span<byte> output, ushort i, out int written)
        {
            BinaryPrimitives.WriteUInt16BigEndian(output, i);
            output = output.Slice(2);
            written = 2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(this ref Span<byte> output, int i, out int written)
        {
            BinaryPrimitives.WriteInt32BigEndian(output, i);
            output = output.Slice(4);
            written = 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(this ref Span<byte> output, uint i, out int written)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output, i);
            output = output.Slice(4);
            written = 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(this ref Span<byte> output, long i, out int written)
        {
            BinaryPrimitives.WriteInt64BigEndian(output, i);
            output = output.Slice(8);
            written = 8;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(this ref Span<byte> output, ulong i, out int written)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, i);
            output = output.Slice(8);
            written = 8;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShortString(this ref Span<byte> output, string val, out int written)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            if (bytes.Length > 255)
            {
                throw new WireFormattingException($"Short string too long; UTF-8 encoded length={bytes.Length}, max=255");
            }
            output[0] = Convert.ToByte(bytes.Length);
            output = output.Slice(1);
            bytes.AsSpan().CopyTo(output);
            output = output.Slice(bytes.Length);
            written = bytes.Length + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(this ref Span<byte> output, float f, out int written)
        {
            var bytes = BitConverter.GetBytes(f);
            new byte[4]{
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                }.AsSpan().CopyTo(output);
            output = output.Slice(4);
            written = 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this ref Span<byte> output, double d, out int written)
        {
            var bytes = BitConverter.GetBytes(d);
            new byte[8]{
                    bytes[7],
                    bytes[6],
                    bytes[5],
                    bytes[4],
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                }.AsSpan().CopyTo(output);
            output = output.Slice(8);
            written = 8;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLongString(this ref Span<byte> output, byte[] val, out int written)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output, (uint)val.Length);
            output = output.Slice(4);
            val.AsSpan().CopyTo(output);
            output = output.Slice(val.Length);
            written = val.Length+4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLongString(this ref Span<byte> output, string val, out int written)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            BinaryPrimitives.WriteUInt32BigEndian(output, (uint)bytes.Length);
            output = output.Slice(4);
            bytes.AsSpan().CopyTo(output);
            output = output.Slice(bytes.Length);
            written = bytes.Length + 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(this ref Span<byte> output, sbyte val, out int written)
        {
            output[0]=(byte)val;
            output = output.Slice(1);
            written = 1;
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
        private static IList<ArraySegment<byte>> GetTableContent(IDictionary<string, object> val, out int written)
        {
            var stream1 = new FrameBuilder(val.Count * 4);
            foreach (var entry in val)
            {
                stream1.WriteShortString(entry.Key);
                stream1.WriteFieldValue(entry.Value);
            }
            written = Convert.ToInt32(stream1.Length);
            return stream1.ToData();
        }
        private static IList<ArraySegment<byte>> GetTableContent(IDictionary<string, bool> val, out int written)
        {
            var stream1 = new FrameBuilder(val.Count * 4);
            foreach (var entry in val)
            {
                stream1.WriteShortString(entry.Key);
                stream1.WriteFieldValue(entry.Value);
            }
            written = Convert.ToInt32(stream1.Length);
            return stream1.ToData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTimestamp(this ref Span<byte> output, AmqpTimestamp val, out int written)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, (ulong)val.UnixTime);
            output = output.Slice(8);
            written = 8;
            // 0-9 is afaict silent on the signedness of the timestamp.
            // See also MethodArgumentReader.ReadTimestamp and AmqpTimestamp itself
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
        public static void WriteTable(this ref Span<byte> output, IDictionary<string, object> val, out int written)
        {
            if (val == null)
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, 0U);
                output = output.Slice(4);
                written = 4;
            }
            else
            {
                var content = GetTableContent(val, out int written1);
                BinaryPrimitives.WriteUInt32BigEndian(output,(uint) written1);
                output = output.Slice(4);
                foreach(var item in content)
                {
                    item.AsSpan().Slice(item.Offset, item.Count).CopyTo(output);
                    output = output.Slice(item.Count);
                }
                written = written1+4;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTable(this ref Span<byte> output, IDictionary<string, bool> val, out int written)
        {
            if (val == null)
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, 0U);
                output = output.Slice(4);
                written = 4;
            }
            else
            {
                var content = GetTableContent(val, out int written1);
                BinaryPrimitives.WriteUInt32BigEndian(output, (uint)written1);
                output = output.Slice(4);
                foreach (var item in content)
                {
                    item.AsSpan().Slice(item.Offset, item.Count).CopyTo(output);
                    output = output.Slice(item.Count);
                }
                written = written1 + 4;
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

        private static IList<ArraySegment<byte>> GetArrayContent(IList val, out int written)
        {
            var stream1 = new FrameBuilder(val.Count * 2);
            foreach (object entry in val)
            {
                stream1.WriteFieldValue(entry);
            }
            written = Convert.ToInt32(stream1.Length);
            return stream1.ToData();
        }

        private static void WriteArray(this ref Span<byte> output, IList val, out int written)
        {
            if (val == null)
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, 0U);
                output = output.Slice(4);
                written = 4;
            }
            else
            {
                var content = GetArrayContent(val, out int written1);
                BinaryPrimitives.WriteUInt32BigEndian(output,(uint) written1);
                output = output.Slice(4);
                foreach (var item in content)
                {
                    item.AsSpan().Slice(item.Offset, item.Count).CopyTo(output);
                    output = output.Slice(item.Count);
                }
                written = written1 + 4;
            }
        }

        private static void WriteDecimal(this ref Span<byte> output, decimal value, out int written)
        {
            DecimalToAmqp(value, out byte scale, out int mantissa);
            output[0]=scale;
            output = output.Slice(1);
            BinaryPrimitives.WriteUInt32BigEndian(output,(uint) mantissa);
            output = output.Slice(4);
            written = 5;
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

        private static void WriteFieldValue(this ref Span<byte> output, object value, out int written)
        {
            if (value == null)
            {
                output[0] = V;
                output = output.Slice(1);
                written = 1;
            }
            else if (value is string)
            {
                output[0] = S;
                output = output.Slice(1);
                var val = Encoding.UTF8.GetBytes(value as string);
                WriteLongString(ref output, val, out int written1);
                written = written1 + 1;
            }
            else if (value is byte[])
            {
                output[0] = S;
                output = output.Slice(1);
                var val = value as byte[];
                WriteLongString(ref output, val, out int written1);
                written = written1 + 1;
            }
            else if (value is int)
            {
                output[0] = I;
                output = output.Slice(1);
                BinaryPrimitives.WriteInt32BigEndian(output, (int)value);
                output = output.Slice(4);
                written = 5;
            }
            else if (value is decimal)
            {
                output[0] = D;
                output = output.Slice(1);
                WriteDecimal(ref output, (decimal)value, out int written1);
                written = written1 + 1;
            }
            else if (value is AmqpTimestamp)
            {
                output[0] = T;
                output = output.Slice(1);
                WriteTimestamp(ref output, (AmqpTimestamp)value, out int written1);
                written = written1 + 1;
            }
            else if (value is IDictionary<string, bool>)
            {
                output[0] = F;
                output = output.Slice(1);
                WriteTable(ref output, value as IDictionary<string, bool>, out int written1);
                written = written1 + 1;
            }
            else if (value is IDictionary)
            {
                output[0] = F;
                output = output.Slice(1);
                WriteTable(ref output, value as IDictionary<string, object>, out int written1);
                written = written1 + 1;
            }
            else if (value is IList)
            {
                output[0] = A;
                output = output.Slice(1);
                WriteArray(ref output, value as IList, out int written1);
                written = written1 + 1;
            }
            else if (value is sbyte)
            {
                output[0] = b;
                output = output.Slice(1);
                WriteSByte(ref output, (sbyte)value, out int written1);
                written = written1 + 1;
            }
            else if (value is double)
            {
                output[0] = d;
                output = output.Slice(1);
                WriteDouble(ref output, (double)value, out int written1);
                written = written1 + 1;
            }
            else if (value is float)
            {
                output[0] = f;
                output = output.Slice(1);
                WriteFloat(ref output, (float)value, out int written1);
                written = written1 + 1;
            }
            else if (value is long)
            {
                output[0] = l;
                output = output.Slice(1);
                WriteInt64(ref output, (long)value, out int written1);
                written = written1 + 1;
            }
            else if (value is ulong)
            {
                output[0] = l;
                output = output.Slice(1);
                WriteUInt64(ref output, (ulong)value, out int written1);
                written = written1 + 1;
            }
            else if (value is uint)
            {
                output[0] = I;
                output = output.Slice(1);
                WriteUInt32(ref output, (uint)value, out int written1);
                written = written1 + 1;
            }
            else if (value is short)
            {
                output[0] = s;
                output = output.Slice(1);
                WriteInt16(ref output, (short)value, out int written1);
                written = written1 + 1;
            }
            else if (value is ushort)
            {
                output[0] = s;
                output = output.Slice(1);
                WriteUInt16(ref output, (ushort)value, out int written1);
                written = written1 + 1;
            }
            else if (value is bool)
            {
                output[0] = t;
                output[1] = (bool)value ? bOne : bZero;
                output = output.Slice(2);
                written = 2;
            }
            else if (value is BinaryTableValue)
            {
                output[0] = x;
                output = output.Slice(1);
                var val = ((BinaryTableValue)value).Bytes;
                val.AsSpan().CopyTo(output);
                output = output.Slice(val.Length);
                written = val.Length + 1;
            }
            else
            {
                throw new WireFormattingException("Value cannot appear as table value", value);
            }
        }
        private const byte bZero = 0;
        private const byte bOne = 1;
    }    
}
