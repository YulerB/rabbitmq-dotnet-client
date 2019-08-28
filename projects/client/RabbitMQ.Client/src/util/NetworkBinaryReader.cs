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
            return data[0].Array[data[0].Offset];
        }
        public byte[] ReadBytes(int payloadSize)
        {
            byte[] bytes = new byte[payloadSize];
            var data = input.Read(payloadSize);
            if(data.Length == 1)
            {
                Buffer.BlockCopy(data[0].Array, data[0].Offset, bytes, 0, data[0].Count);
                return bytes;
            }
            else
            {
                int offset = 0;
                foreach (var segment in data)
                {
                    Buffer.BlockCopy(segment.Array, segment.Offset, bytes, offset, segment.Count);
                    offset += segment.Count;
                }
            }
            return bytes;
        }
        public ushort ReadUInt16()
        {
            var data = input.Read(2);

            if (data.Length == 1)
            {
                return BitConverter.ToUInt16(new byte[2]{
                    data[0].Array[data[0].Offset+1],
                    data[0].Array[data[0].Offset]
                    }, 0);
            }


            var arrayIndex = 0;
            var offset = 0;
            byte[] bytes = new byte[2];

            var count = data[arrayIndex].Count;
            for (int i = 1; i > -1; i--)
            {
                var segment = data[arrayIndex];
                bytes[i] = segment.Array[segment.Offset + offset];
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
                return BitConverter.ToUInt32(new byte[4]{
                    data[0].Array[data[0].Offset+3],
                    data[0].Array[data[0].Offset+2],
                    data[0].Array[data[0].Offset+1],
                    data[0].Array[data[0].Offset]
                    }, 0);
            }

            byte[] bytes = new byte[4];
            var arrayIndex = 0;
            var offset = 0;
            var count = data[arrayIndex].Count;
            for (int i = 3; i > -1; i--)
            {
                var segment = data[arrayIndex];
                bytes[i] = segment.Array[segment.Offset + offset];
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
    }
}
