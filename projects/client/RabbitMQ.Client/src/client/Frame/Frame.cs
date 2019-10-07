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

using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using RabbitMQ.Util;
using System;
using System.Buffers;
using System.IO;

#if NETFX_CORE
using Windows.Networking.Sockets;
#else

using System.Net.Sockets;

#endif

namespace RabbitMQ.Client.Impl
{
    public class HeaderOutboundFrame : OutboundFrame
    {
        private readonly RabbitMQ.Client.Impl.BasicProperties header;
        private readonly ulong bodyLength;

        public HeaderOutboundFrame(ushort channel, RabbitMQ.Client.Impl.BasicProperties header, ulong bodyLength) : base(FrameType.FrameHeader, channel)
        {
            this.header = header;
            this.bodyLength = bodyLength;
        }

        public override void WritePayload(Span<byte> writer, out int written)
        {
            var total = 2 + header.EstimateSize();
            NetworkBinaryWriter1.WriteUInt32(writer, (uint)total, out int written1);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1), header.ProtocolClassId, out int written2);
            header.WriteTo(writer.Slice(written1 + written2),bodyLength, out int written3);
            written = written1 + written2 + written3;
        }
        internal override int EstimatePayloadSize()
        {
            return 6 + header.EstimateSize();
        }
    }
    public class BodySegmentOutboundFrame : OutboundFrame
    {
        private readonly ArraySegment<byte> data;

        public BodySegmentOutboundFrame(ushort channel, ArraySegment<byte> data) : base(FrameType.FrameBody, channel)
        {
            this.data = data;
        }

        public override void WritePayload(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt32(writer, (uint)data.Count, out int written1);
            NetworkBinaryWriter1.Write(writer.Slice(written1), data.Array, data.Offset, data.Count, out int written2);
            written = written1 + written2;
        }
        internal override int EstimatePayloadSize()
        {
            return 4 + data.Count;
        }
    }

    public class MethodOutboundFrame : OutboundFrame
    {
        private readonly IMethod method;

        public MethodOutboundFrame(ushort channel, IMethod method) : base(FrameType.FrameMethod, channel)
        {
            this.method = method;
        }


        public override void WritePayload(Span<byte> writer, out int written)
        {
            var total = 4 + method.EstimateSize();
            NetworkBinaryWriter1.WriteUInt32(writer, (uint)total, out int written1);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1), method.ProtocolClassId, out int written2);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1 + written2), method.ProtocolMethodId, out int written3);
            method.WriteArgumentsTo(writer.Slice(written1 + written2 + written3), out int written4);
            written = written1 + written2 + written3 + written4;
        }
        internal override int EstimatePayloadSize()
        {
            return 8 + method.EstimateSize();
        }
    }

    public class EmptyOutboundFrame : OutboundFrame
    {
        public EmptyOutboundFrame() : base(FrameType.FrameHeartbeat, 0)
        {
        }


        public override void WritePayload(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt32(writer, 0U, out int written1);
            written = written1;
        }

        internal override int EstimatePayloadSize()
        {
            return 4;
        }
    }

    public abstract class OutboundFrame : Frame
    {
        public OutboundFrame(FrameType type, ushort channel) : base(type, channel)
        {
        }

        public void WriteTo(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteByte(writer, (byte)Type, out int written1);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1), Channel, out int written2);
            WritePayload(writer.Slice(written1 + written2), out int written3);
            NetworkBinaryWriter1.WriteByte(writer.Slice(written1 + written2 + written3), Constants.FrameEnd, out int written4);
            written = written1 + written2 + written3 + written4;
        }
        public int EstimatedSize()
        {
            return 4 + EstimatePayloadSize();
        }
        internal abstract int EstimatePayloadSize();

        public abstract void WritePayload(Span<byte> writer, out int written);
    }

    public class InboundFrame : Frame
    {
        internal InboundFrame(FrameType type, ushort channel, byte[] payload, IMethod method, RabbitMQ.Client.Impl.BasicProperties header, ulong totalBodyBytes)
            : base(type, channel, payload)
        {
            this.Method = method;
            this.Header = header;
            this.TotalBodyBytes = totalBodyBytes;
        }

        public IMethod Method
        {
            get;
            private set;
        }
        public RabbitMQ.Client.Impl.BasicProperties Header
        {
            get;
            private set;
        }
        public ulong TotalBodyBytes { get; private set; }
    }
    public static class FrameReader
    {
        private static readonly Protocol m_protocol = new Protocol();

        private static void ProcessProtocolHeader(ArraySegmentSequence reader)
        {
            try
            {
                byte b1 = reader.ReadByte();
                byte b2 = reader.ReadByte();
                byte b3 = reader.ReadByte();
                if (b1 != 'M' || b2 != 'Q' || b3 != 'P')
                {
                    throw new MalformedFrameException("Invalid AMQP protocol header from server");
                }

                byte transportHigh = reader.ReadByte();
                byte transportLow = reader.ReadByte();
                byte serverMajor = reader.ReadByte();
                byte serverMinor = reader.ReadByte();
                throw new PacketNotRecognizedException(transportHigh,
                    transportLow,
                    serverMajor,
                    serverMinor);
            }
            catch (EndOfStreamException)
            {
                // Ideally we'd wrap the EndOfStreamException in the
                // MalformedFrameException, but unfortunately the
                // design of MalformedFrameException's superclass,
                // ProtocolViolationException, doesn't permit
                // this. Fortunately, the call stack in the
                // EndOfStreamException is largely irrelevant at this
                // point, so can safely be ignored.
                throw new MalformedFrameException("Invalid AMQP protocol header from server");
            }
        }
        public static InboundFrame ReadFrom(ArraySegmentSequence reader)
        {
            byte type;

            try
            {
                type = reader.ReadFirstByte();
            }
            catch (IOException ioe)
            {
#if NETFX_CORE
                if (ioe.InnerException != null
                    && SocketError.GetStatus(ioe.InnerException.HResult) == SocketErrorStatus.ConnectionTimedOut)
                {
                    throw ioe.InnerException;
                }

                throw;
#else
                // If it's a WSAETIMEDOUT SocketException, unwrap it.
                // This might happen when the limit of half-open connections is
                // reached.
                if (ioe.InnerException == null ||
                    !(ioe.InnerException is SocketException) ||
                    ((SocketException)ioe.InnerException).SocketErrorCode != SocketError.TimedOut)
                {
                    throw ioe;
                }
                throw ioe.InnerException;
#endif
            }

            if (type == 'A')
            {
                // Probably an AMQP protocol header, otherwise meaningless
                ProcessProtocolHeader(reader);
            }

            ushort channel = reader.ReadUInt16();
            uint payloadSize = reader.ReadUInt32(); // FIXME - throw exn on unreasonable value

            RabbitMQ.Client.Impl.BasicProperties m_content = null;
            byte[] payload = EmptyByteArray;
            IMethod m_method = null;
            ulong totalBodyBytes = 0UL;

            if (type == (int)FrameType.FrameMethod)
            {
                m_method = m_protocol.DecodeMethodFrom(reader);
            }
            else if (type == (int)FrameType.FrameHeader)
            {
                m_content = m_protocol.DecodeContentHeaderFrom(reader);
                totalBodyBytes = m_content.ReadFrom(reader);
            }
            else if (type == (int)FrameType.FrameBody)
            {
                payload = reader.ReadBytes(Convert.ToInt32(payloadSize));

                if (payload.Length != payloadSize)
                {
                    // Early EOF.
                    throw new MalformedFrameException("Short frame - expected " +
                                                      payloadSize + " bytes, got " +
                                                      payload.Length + " bytes");
                }
            }

            byte frameEndMarker = reader.ReadByte();
            if (frameEndMarker != Constants.FrameEnd)
            {
                throw new MalformedFrameException("Bad frame end marker: " + frameEndMarker);
            }

            return new InboundFrame((FrameType)type, channel, payload, m_method, m_content, totalBodyBytes);
        }
        private static readonly byte[] EmptyByteArray = new byte[] { };
    }
    public class Frame
    {
        public Frame(FrameType type, ushort channel)
        {
            Type = type;
            Channel = channel;
        }

        public Frame(FrameType type, ushort channel, byte[] payload)
        {
            Type = type;
            Channel = channel;
            Payload = payload;
        }

        public ushort Channel { get; private set; }

        public byte[] Payload { get; private set; }

        public FrameType Type { get; private set; }

        public override string ToString()
        {
            return string.Format("(type={0}, channel={1}, {2} bytes of payload)",
                Type,
                Channel,
                Payload == null
                    ? "(null)"
                    : Payload.Length.ToString());
        }

        public bool IsMethod()
        {
            return this.Type == FrameType.FrameMethod;
        }
        public bool IsHeader()
        {
            return this.Type == FrameType.FrameHeader;
        }
        public bool IsBody()
        {
            return this.Type == FrameType.FrameBody;
        }
        public bool IsHeartbeat()
        {
            return this.Type == FrameType.FrameHeartbeat;
        }
    }

    public enum FrameType : int
    {
        FrameMethod = 1,
        FrameHeader = 2,
        FrameBody = 3,
        FrameHeartbeat = 8
    }

}
