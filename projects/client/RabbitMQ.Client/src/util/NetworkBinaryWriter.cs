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
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public class NetworkBinaryWriter //: BinaryWriter
    {
        private ArraySegmentStream stream;
        /// <summary>
        /// Construct a NetworkBinaryWriter over the given input stream.
        /// </summary>
        public NetworkBinaryWriter(ArraySegmentStream output) //: base(output)
        {
            this.stream = output;
        }

        public void WriteBits(bool[] bits)
        {
            int totalBits = Convert.ToInt32(16D * Math.Ceiling(bits.Length == 0 ? 1 : bits.Length / 15D));
            System.Diagnostics.Debug.WriteLine(totalBits);
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

            Write(bytes);
        }
        public void WriteBits1(bool[] bits)
        {
            int totalBits = Convert.ToInt32(16D * Math.Ceiling(bits.Length == 0 ? 1 : bits.Length / 15D));
            System.Diagnostics.Debug.WriteLine(totalBits);
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

            Write(bytes);
        }

        private byte Reverse(byte b)
        {
            int a = 0;
            for (int i = 0; i < 8; i++)
                if ((b & (1 << i)) != 0)
                    a |= 1 << (7 - i);
            return (byte)a;
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public void Write(short i)
        {
            var bytes = BitConverter.GetBytes(i);
            stream.Write(
                new byte[2]{
                    bytes[1],
                    bytes[0]
                },
                0,
                2);
        }
        public void Write(byte[] buffer)
        {
            stream.Write(
                buffer,
                0,
                buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(
                buffer,
                offset,
                count);
        }
        public void Write(ushort i)
        {
            var bytes = BitConverter.GetBytes(i);
            stream.Write(
                new byte[2]{
                    bytes[1],
                    bytes[0]
                },
                0,
                2);
        }
        public void Write(int i)
        {
            var bytes = BitConverter.GetBytes(i);
            stream.Write(
                new byte[4]{
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                },
                0,
                4);
        }
        public void Write(uint i)
        {
            var bytes = BitConverter.GetBytes(i);
            stream.Write(
                new byte[4]{
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                },
                0,
                4);
        }
        public void Write(long i)
        {
            var bytes = BitConverter.GetBytes(i);
            stream.Write(
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
        public void Write(ulong i)
        {
            var bytes = BitConverter.GetBytes(i);
            stream.Write(
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
        public void WriteShortstr(string val)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            if (bytes.Length > 255)
            {
                throw new WireFormattingException("Short string too long; " +
                                                  "UTF-8 encoded length=" + bytes.Length + ", max=255");
            }
            Write((byte)bytes.Length);
            Write(bytes);
        }
        public void Write(float f)
        {
            var bytes = BitConverter.GetBytes(f);
            stream.Write(
                new byte[4]{
                    bytes[3],
                    bytes[2],
                    bytes[1],
                    bytes[0]
                },
                0,
                4);
        }
        public void Write(double d)
        {
            var bytes = BitConverter.GetBytes(d);
            stream.Write(
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
        public void WriteLongstr(byte[] val)
        {
            Write((uint)val.Length);
            Write(val);
        }
        public void Write(byte val)
        {
            stream.WriteByte(val);
        }
        public void Write(sbyte val)
        {
            stream.WriteByte((byte)val);
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
        public IList<ArraySegment<byte>> GetTableContent(IDictionary<string, object> val, out int written)
        {
            var stream1 = new ArraySegmentStream();
            NetworkBinaryWriter bw = new NetworkBinaryWriter(stream1);
            foreach (var entry in val)
            {
                bw.WriteShortstr(entry.Key);
                bw.WriteFieldValue(entry.Value);
            }
            written = Convert.ToInt32(stream1.Length);
            return stream1.Data;
        }
        public IList<ArraySegment<byte>> GetTableContent(IDictionary<string, bool> val, out int written)
        {
            var stream1 = new ArraySegmentStream();
            NetworkBinaryWriter bw = new NetworkBinaryWriter(stream1);
            foreach (var entry in val)
            {
                bw.WriteShortstr(entry.Key);
                bw.WriteFieldValue(entry.Value);
            }
            written = Convert.ToInt32(stream1.Length);
            return stream1.Data;
        }

        public void WriteTimestamp(AmqpTimestamp val)
        {
            // 0-9 is afaict silent on the signedness of the timestamp.
            // See also MethodArgumentReader.ReadTimestamp and AmqpTimestamp itself
            Write((ulong)val.UnixTime);
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
        public void WriteTable(IDictionary<string, object> val)
        {
            if (val == null)
            {
                Write((uint)0);
            }
            else
            {
                var content = GetTableContent(val, out int written1);
                Write((uint)written1);
                foreach (var item in content)
                {
                    Write(item);
                }
            }
        }
        public void WriteTable(IDictionary<string, bool> val)
        {
            if (val == null)
            {
                Write((uint)0);
            }
            else
            {
                var content = GetTableContent(val, out int written1);
                Write((uint)written1);
                foreach (var item in content)
                {
                    Write(item);
                }
            }
        }

        public void DecimalToAmqp(decimal value, out byte scale, out int mantissa)
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

        private IList<ArraySegment<byte>> GetArrayContent(IList val, out int written)
        {
            var stream1 = new ArraySegmentStream();
            NetworkBinaryWriter bw = new NetworkBinaryWriter(stream1);
            foreach (object entry in val)
            {
                bw.WriteFieldValue(entry);
            }
            written = Convert.ToInt32(stream1.Length);
            return stream1.Data;
        }

        private void WriteArray(IList val)
        {
            if (val == null)
            {
                Write((uint)0); // length of table - will be backpatched
            }
            else
            {
                var content = GetArrayContent(val, out int written1);
                Write((uint)written1); // length of table - will be backpatched
                foreach (var item in content)
                {
                    Write(item);
                }
            }
        }

        private void WriteDecimal(decimal value)
        {
            byte scale;
            int mantissa;
            DecimalToAmqp(value, out scale, out mantissa);
            Write(scale);
            Write((uint)mantissa);
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

        private void WriteFieldValue(object value)
        {
            if (value == null)
            {
                Write(V);
            }
            else if (value is string)
            {
                Write(S);
                WriteLongstr(Encoding.UTF8.GetBytes((string)value));
            }
            else if (value is byte[])
            {
                Write(S);
                WriteLongstr((byte[])value);
            }
            else if (value is int)
            {
                Write(I);
                Write((int)value);
            }
            else if (value is decimal)
            {
                Write(D);
                WriteDecimal((decimal)value);
            }
            else if (value is AmqpTimestamp)
            {
                Write(T);
                WriteTimestamp((AmqpTimestamp)value);
            }
            else if (value is IDictionary<string, bool>)
            {
                Write(F);
                WriteTable((IDictionary<string, bool>)value);
            }
            else if (value is IDictionary)
            {
                Write(F);
                WriteTable((IDictionary<string, object>)value);
            }
            else if (value is IList)
            {
                Write(A);
                WriteArray((IList)value);
            }
            else if (value is sbyte)
            {
                Write(b);
                Write((sbyte)value);
            }
            else if (value is double)
            {
                Write(d);
                Write((double)value);
            }
            else if (value is float)
            {
                Write(f);
                Write((float)value);
            }
            else if (value is long)
            {
                Write(l);
                Write((long)value);
            }
            else if (value is ulong)
            {
                Write(l);
                Write((ulong)value);
            }
            else if (value is uint)
            {
                Write(I);
                Write((uint)value);
            }
            else if (value is short)
            {
                Write(s);
                Write((short)value);
            }
            else if (value is ushort)
            {
                Write(s);
                Write((ushort)value);
            }
            else if (value is bool)
            {
                Write(t);
                Write((byte)(((bool)value) ? 1 : 0));
            }
            else if (value is BinaryTableValue)
            {
                Write(x);
                Write(((BinaryTableValue)value).Bytes);
            }
            else
            {
                throw new WireFormattingException("Value cannot appear as table value", value);
            }
        }

        public void Write(ArraySegment<byte> segment)
        {
            stream.Write(segment);
        }
    }
}

