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
        private Stream stream;
        /// <summary>
        /// Construct a NetworkBinaryWriter over the given input stream.
        /// </summary>
        public NetworkBinaryWriter(Stream output) //: base(output)
        {
            this.stream = output;
        }

        public Stream BaseStream { get { return stream; } }

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


        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
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


        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
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

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
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

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
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

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
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
        public void WriteShortstr(string val, out int written)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            int len = bytes.Length;
            if (len > 255)
            {
                throw new WireFormattingException("Short string too long; " +
                                                  "UTF-8 encoded length=" + len + ", max=255");
            }
            Write((byte)len);
            Write(bytes);
            written = len + 1;
        }
        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
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

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
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

        public void WriteLongstr(string val, out int written)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            int len = bytes.Length;
            Write((uint)len);
            Write(bytes);
            written = len + 4;
        }

        public void Write(byte val)
        {
            stream.WriteByte(val);
        }
        public void Write(sbyte val)
        {
            stream.WriteByte((byte)val);
        }
        #region Todo
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
        public IList<ArraySegment<byte>> GetTableContent(IDictionary<string, dynamic> val, out int written)
        {
            written = 0;
            var stream1 = new ArraySegmentStream();
            NetworkBinaryWriter bw = new NetworkBinaryWriter(stream1);
            foreach (var entry in val)
            {
                bw.WriteShortstr(entry.Key, out int written1);
                written += written1;
                object value = entry.Value;
                bw.WriteFieldValue(value, out int written2);
                written += written2;
            }
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
        public void WriteTable(IDictionary<string, dynamic> val, out int written)
        {
            if (val == null)
            {
                Write((uint)0);
                written = 4;
                return;
            }

            var content = GetTableContent(val, out int written1);
            Write((uint)written1);
            foreach (var item in content)
            {
                Write(item);
            }
            written = written1 + 4;
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

        public IList<ArraySegment<byte>> GetArrayContent(IList val, out int written)
        {
            written = 0;
            var stream1 = new ArraySegmentStream();
            NetworkBinaryWriter bw = new NetworkBinaryWriter(stream1);
            if (val != null)
            {
                foreach (object entry in val)
                {
                    bw.WriteFieldValue(entry, out int written1);
                    written += written1;
                }
            }
            return stream1.Data;
        }

        public void WriteArray(IList val, out int written)
        {
            if (val == null)
            {
                Write((uint)0); // length of table - will be backpatched
                written = 4;
                return;
            }

            var content = GetArrayContent(val, out int written1);
            Write((uint)written1); // length of table - will be backpatched
            foreach (var item in content)
            {
                Write(item);
            }
            written = written1 + 4;
        }

        public void WriteDecimal(decimal value)
        {
            byte scale;
            int mantissa;
            DecimalToAmqp(value, out scale, out mantissa);
            Write(scale);
            Write((uint)mantissa);
        }

        public void WriteFieldValue(object value, out int written)
        {
            if (value == null)
            {
                Write((byte)'V');
                written = 1;
            }
            else if (value is string)
            {
                Write((byte)'S');
                WriteLongstr((string)value, out int written1);
                written = written1 + 1;
            }
            else if (value is byte[])
            {
                byte[] val = (byte[])value;
                Write((byte)'S');
                Write(val);
                written = val.Length + 1;
            }
            else if (value is int)
            {
                Write((byte)'I');
                Write((int)value);
                written = 5;
            }
            else if (value is decimal)
            {
                Write((byte)'D');
                WriteDecimal((decimal)value);
                written = 6;
            }
            else if (value is AmqpTimestamp)
            {
                Write((byte)'T');
                WriteTimestamp((AmqpTimestamp)value);
                written = 9;
            }
            else if (value is IDictionary)
            {
                Write((byte)'F');
                WriteTable((IDictionary<string, dynamic>)value, out int written1);
                written = written1 + 1;
            }
            else if (value is IList)
            {
                Write((byte)'A');
                WriteArray((IList)value, out int written1);
                written = written1 + 1;
            }
            else if (value is sbyte)
            {
                Write((byte)'b');
                Write((sbyte)value);
                written = 2;
            }
            else if (value is double)
            {
                Write((byte)'d');
                Write((double)value);
                written = 9;
            }
            else if (value is float)
            {
                Write((byte)'f');
                Write((float)value);
                written = 5;
            }
            else if (value is long)
            {
                Write((byte)'l');
                Write((long)value);
                written = 9;
            }
            else if (value is short)
            {
                Write((byte)'s');
                Write((short)value);
                written = 3;
            }
            else if (value is bool)
            {
                Write((byte)'t');
                Write((byte)(((bool)value) ? 1 : 0));
                written = 2;
            }
            else if (value is BinaryTableValue)
            {
                var val = ((BinaryTableValue)value).Bytes;
                Write((byte)'x');
                Write(val);
                written = val.Length + 1;
            }
            else
            {
                throw new WireFormattingException("Value cannot appear as table value",
                    value);
            }
        }

        public void Write(ArraySegment<byte> segment)
        {
            stream.Write(segment.Array, segment.Offset, segment.Count);
        }
        #endregion
    }
}

