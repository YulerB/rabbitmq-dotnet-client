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
    public static class NetworkBinaryWriter1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(Span<byte> output, byte buffer, out int written)
        {
            output[0] = buffer;
            written = 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBits(Span<byte> output, bool[] bits, out int written)
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
        public static void Write(Span<byte> output, byte[] buffer, out int written)
        {
            buffer.AsSpan().CopyTo(output);
            written = buffer.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> output, byte[] buffer, int offset, int count, out int written)
        {
            buffer.AsSpan().Slice(offset, count).CopyTo(output);
            written = count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> output, Span<byte> buffer, int count, out int written)
        {
            buffer.Slice(0, count).CopyTo(output);
            written = count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> output, Span<byte> buffer, int offset, int count, out int written)
        {
            buffer.Slice(offset, count).CopyTo(output);
            written = count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(Span<byte> output, short i, out int written)
        {
            BinaryPrimitives.WriteInt16BigEndian(output, i);
            written = 2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(Span<byte> output, ushort i, out int written)
        {
            BinaryPrimitives.WriteUInt16BigEndian(output, i);
            written = 2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(Span<byte> output, int i, out int written)
        {
            BinaryPrimitives.WriteInt32BigEndian(output, i);
            written = 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(Span<byte> output, uint i, out int written)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output, i);
            written = 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(Span<byte> output, long i, out int written)
        {
            BinaryPrimitives.WriteInt64BigEndian(output, i);
            written = 8;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(Span<byte> output, ulong i, out int written)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, i);
            written = 8;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShortString(Span<byte> output, string val, out int written)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            if (bytes.Length > 255)
            {
                throw new WireFormattingException($"Short string too long; UTF-8 encoded length={bytes.Length}, max=255");
            }
            output[0] = Convert.ToByte(bytes.Length);
            bytes.AsSpan().CopyTo(output.Slice(1));
            written = bytes.Length + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(Span<byte> output, float f, out int written)
        {
            var bytes = BitConverter.GetBytes(f);
            new byte[4]{
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                }.AsSpan().CopyTo(output);
            written = 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(Span<byte> output, double d, out int written)
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
            written = 8;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLongString(Span<byte> output, byte[] val, out int written)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output, (uint)val.Length);
            val.AsSpan().CopyTo(output.Slice(4));
            written = val.Length + 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLongString(Span<byte> output, string val, out int written)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            BinaryPrimitives.WriteUInt32BigEndian(output, (uint)bytes.Length);
            bytes.AsSpan().CopyTo(output.Slice(4));
            written = bytes.Length + 4;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(Span<byte> output, sbyte val, out int written)
        {
            output[0] = (byte)val;
            written = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTimestamp(Span<byte> output, AmqpTimestamp val, out int written)
        {
            BinaryPrimitives.WriteUInt64BigEndian(output, (ulong)val.UnixTime);
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
        public static void WriteTable(Span<byte> output, IDictionary<string, object> val, out int written)
        {
            if (val == null)
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, 0U);
                written = 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, (uint)EstimateTableContentSize(val));
                int offset = 4;
                foreach (var entry in val)
                {
                    WriteShortString(output.Slice(offset), entry.Key,  out int written1);
                    WriteFieldValue(output.Slice(offset+written1), entry.Value, out int written2);
                    offset += written1 + written2;
                }
                written = offset;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTable(Span<byte> output, IDictionary<string, bool> val, out int written)
        {
            if (val == null)
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, 0U);
                written = 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, (uint)EstimateTableContentSize(val));
                int offset = 4;
                foreach (var entry in val)
                {
                    WriteShortString(output.Slice(offset), entry.Key, out int written1);
                    WriteFieldValue(output.Slice(offset + written1), entry.Value, out int written2);
                    offset += written1 + written2;
                }
                written = offset;
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
     
        private static void WriteArray(Span<byte> output, IList val, out int written)
        {
            if (val == null)
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, 0U);
                written = 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(output, (uint)EstimateArrayContentSize(val));
                int offset = 4;
                foreach (object entry in val)
                {
                    WriteFieldValue(output.Slice(offset), entry, out int written1);
                    offset += written1;
                }
                written = offset;
            }
        }
        private static void WriteDecimal(Span<byte> output, decimal value, out int written)
        {
            DecimalToAmqp(value, out byte scale, out int mantissa);
            output[0] = scale;
            BinaryPrimitives.WriteUInt32BigEndian(output.Slice(1), (uint)mantissa);
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
        private const byte bZero = 0;
        private const byte bOne = 1;

        private static void WriteFieldValue(Span<byte> output, object value, out int written)
        {
            if (value == null)
            {
                output[0] = V;
                written = 1;
            }
            else if (value is string)
            {
                output[0] = S;
                var val = Encoding.UTF8.GetBytes(value as string);
                WriteLongString(output.Slice(1), val, out int written1);
                written = written1 + 1;
            }
            else if (value is byte[])
            {
                output[0] = S;
                var val = value as byte[];
                WriteLongString(output.Slice(1), val, out int written1);
                written = written1 + 1;
            }
            else if (value is int)
            {
                output[0] = I;
                BinaryPrimitives.WriteInt32BigEndian(output.Slice(1), (int)value);
                written = 5;
            }
            else if (value is decimal)
            {
                output[0] = D;
                WriteDecimal(output.Slice(1), (decimal)value, out int written1);
                written = written1 + 1;
            }
            else if (value is AmqpTimestamp)
            {
                output[0] = T;
                WriteTimestamp(output.Slice(1), (AmqpTimestamp)value, out int written1);
                written = written1 + 1;
            }
            else if (value is IDictionary<string, bool>)
            {
                output[0] = F;
                WriteTable(output.Slice(1), value as IDictionary<string, bool>, out int written1);
                written = written1 + 1;
            }
            else if (value is IDictionary)
            {
                output[0] = F;
                WriteTable(output.Slice(1), value as IDictionary<string, object>, out int written1);
                written = written1 + 1;
            }
            else if (value is IList)
            {
                output[0] = A;
                WriteArray(output.Slice(1), value as IList, out int written1);
                written = written1 + 1;
            }
            else if (value is sbyte)
            {
                output[0] = b;
                WriteSByte(output.Slice(1), (sbyte)value, out int written1);
                written = written1 + 1;
            }
            else if (value is double)
            {
                output[0] = d;
                WriteDouble(output.Slice(1), (double)value, out int written1);
                written = written1 + 1;
            }
            else if (value is float)
            {
                output[0] = f;
                WriteFloat(output.Slice(1), (float)value, out int written1);
                written = written1 + 1;
            }
            else if (value is long)
            {
                output[0] = l;
                WriteInt64(output.Slice(1), (long)value, out int written1);
                written = written1 + 1;
            }
            else if (value is ulong)
            {
                output[0] = l;
                WriteUInt64(output.Slice(1), (ulong)value, out int written1);
                written = written1 + 1;
            }
            else if (value is uint)
            {
                output[0] = I;
                WriteUInt32(output.Slice(1), (uint)value, out int written1);
                written = written1 + 1;
            }
            else if (value is short)
            {
                output[0] = s;
                WriteInt16(output.Slice(1), (short)value, out int written1);
                written = written1 + 1;
            }
            else if (value is ushort)
            {
                output[0] = s;
                WriteUInt16(output.Slice(1), (ushort)value, out int written1);
                written = written1 + 1;
            }
            else if (value is bool)
            {
                output[0] = t;
                output[1] = (bool)value ? bOne : bZero;
                written = 2;
            }
            else if (value is BinaryTableValue)
            {
                output[0] = x;
                var val = ((BinaryTableValue)value).Bytes;
                val.AsSpan().CopyTo(output.Slice(1));
                written = val.Length + 1;
            }
            else
            {
                throw new WireFormattingException("Value cannot appear as table value", value);
            }
        }
        private static void WriteFieldValue(Span<byte> output, bool value, out int written)
        {
            output[0] = t;
            output[1] = (bool)value ? bOne : bZero;
            written = 2;
        }

        public static int EstimateTableSize(IDictionary<string, object> m_arguments)
        {
            if (m_arguments == null) return 4;
            return 4+EstimateTableContentSize(m_arguments);
        }
        private static int EstimateTableSize(IDictionary<string, bool> m_arguments)
        {
            if (m_arguments == null) return 4;
            return 4+EstimateTableContentSize(m_arguments);
        }
        private static int EstimateArraySize(IList m_arguments)
        {
            if (m_arguments == null) return 4;
            return 4+EstimateArrayContentSize(m_arguments);
        }
        private static int EstimateTableContentSize(IDictionary<string, object> val)
        {
            int size = 0;

            foreach (var entry in val)
            {
                size += 1 + System.Text.Encoding.UTF8.GetByteCount(entry.Key) + EstimateFieldValueSize(entry.Value);
            }
            return size;
        }
        private static int EstimateTableContentSize(IDictionary<string, bool> val)
        {
            int size = 0;

            foreach (var entry in val)
            {
                size += 3 + System.Text.Encoding.UTF8.GetByteCount(entry.Key);
            }
            return size;
        }
        private static int EstimateFieldValueSize(object value)
        {
            if (value == null)
            {
                return 1;
            }
            else if (value is string)
            {
                return 5 + Encoding.UTF8.GetByteCount(value as string);
            }
            else if (value is byte[])
            {
                return 5 + (value as byte[]).Length;
            }
            else if (value is int)
            {
                return 5;
            }
            else if (value is decimal)
            {
                return 6;
            }
            else if (value is AmqpTimestamp)
            {
                return 9;
            }
            else if (value is IDictionary<string, bool>)
            {
                return 1 + EstimateTableSize(value as IDictionary<string, bool>);
            }
            else if (value is IDictionary)
            {
                return 1 + EstimateTableSize(value as IDictionary<string, object>);
            }
            else if (value is IList)
            {
                return 1 + EstimateArraySize(value as IList);
            }
            else if (value is sbyte)
            {
                return 2;
            }
            else if (value is double)
            {
                return 9;
            }
            else if (value is float)
            {
                return 5;
            }
            else if (value is long)
            {
                return 9;
            }
            else if (value is ulong)
            {
                return 9;
            }
            else if (value is uint)
            {
                return 5;
            }
            else if (value is short)
            {
                return 3;
            }
            else if (value is ushort)
            {
                return 3;
            }
            else if (value is bool)
            {
                return 2;
            }
            else if (value is BinaryTableValue)
            {
                return 1 + ((BinaryTableValue)value).Bytes.Length;
            }
            else
            {
                throw new WireFormattingException("Value cannot appear as table value", value);
            }
        }
        private static int EstimateArrayContentSize(IList val)
        {
            int size = 0;
            foreach (object entry in val)
            {
                size += EstimateFieldValueSize(entry);
            }
            return size;
        }

    }
}
