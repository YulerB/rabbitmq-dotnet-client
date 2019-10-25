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

        public sealed override void WritePayload(Span<byte> writer, out int written)
        {
            var total = 2 + header.EstimateSize();
            NetworkBinaryWriter1.WriteUInt32(writer, (uint)total, out int written1);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1), header.ProtocolClassId, out int written2);
            header.WriteTo(writer.Slice(written1 + written2),bodyLength, out int written3);
            written = written1 + written2 + written3;
        }
        internal sealed override int EstimatePayloadSize()
        {
            return 6 + header.EstimateSize();
        }

        public sealed override string ToString()
        {
            return string.Format(
                "( type={0}, channel={1}, ProtocolClassId={2}, bodyLength={3} )", 
                new object[] {
                    Type,
                    Channel,
                    header.ProtocolClassId,
                    bodyLength
                }
            );
        }

    }
    public class BodySegmentOutboundFrame : OutboundFrame
    {
        private readonly ArraySegment<byte> data;

        public BodySegmentOutboundFrame(ushort channel, ArraySegment<byte> data) : base(FrameType.FrameBody, channel)
        {
            this.data = data;
        }

        public sealed override void WritePayload(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt32(writer, (uint)data.Count, out int written1);
            NetworkBinaryWriter1.Write(writer.Slice(written1), data.Array, data.Offset, data.Count, out int written2);
            written = written1 + written2;
        }
        internal sealed override int EstimatePayloadSize()
        {
            return 4 + data.Count;
        }

        public override string ToString()
        {
            return $"( type={Type}, channel={Channel.ToString()}, bodyLength={data.Count.ToString()} )";
        }
    }

    public class MethodOutboundFrame<T> : OutboundFrame where T: IMethod
    {
        private readonly T method;

        public MethodOutboundFrame(ushort channel, T method) : base(FrameType.FrameMethod, channel)
        {
            this.method = method;
        }


        public sealed override void WritePayload(Span<byte> writer, out int written)
        {
            var total = 4 + method.EstimateSize();
            NetworkBinaryWriter1.WriteUInt32(writer, (uint)total, out int written1);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1), method.ProtocolClassId, out int written2);
            NetworkBinaryWriter1.WriteUInt16(writer.Slice(written1 + written2), method.ProtocolMethodId, out int written3);
            method.WriteArgumentsTo(writer.Slice(written1 + written2 + written3), out int written4);
            written = written1 + written2 + written3 + written4;
        }
        internal sealed override int EstimatePayloadSize()
        {
            return 8 + method.EstimateSize();
        }
        public sealed override string ToString()
        {
            return $"( type={Type}, channel={Channel.ToString()}, method={method} )";
        }
    }

    public class EmptyOutboundFrame : OutboundFrame
    {
        public EmptyOutboundFrame() : base(FrameType.FrameHeartbeat, 0)
        {
        }


        public sealed override void WritePayload(Span<byte> writer, out int written)
        {
            NetworkBinaryWriter1.WriteUInt32(writer, 0U, out int written1);
            written = written1;
        }

        internal sealed override int EstimatePayloadSize()
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

        public override string ToString()
        {
            if (Type == FrameType.FrameMethod)
            {
                return $"( type={Type}, channel={Channel.ToString()}, method={Method} )";
            }
            else if (Type == FrameType.FrameBody)
            {
                var len = Payload == null ? "null" : Payload.Length.ToString();
                return $"( type={Type}, channel={Channel.ToString()}, Payload={len} )";
            }
            else if (Type == FrameType.FrameHeader)
            {
                return $"( type={Type}, channel={Channel.ToString()}, TotalBodyBytes={TotalBodyBytes.ToString()} )";
            }
            else if (Type == FrameType.FrameHeartbeat)
            {
                return "( Heartbeat )";
            }
            else
            {
                return base.ToString();
            }
        }

    }
    public static class FrameReader
    {
        private const ulong ULZERO = 0UL;
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

            if (type == Constants.AMQPFrameBegin )
            {
                // Probably an AMQP protocol header, otherwise meaningless
                ProcessProtocolHeader(reader);
            }

            ushort channel = reader.ReadUInt16();
            uint payloadSize = reader.ReadUInt32(); // FIXME - throw exn on unreasonable value

            RabbitMQ.Client.Impl.BasicProperties m_content = null;
            byte[] payload = EmptyByteArray;
            IMethod m_method = null;
            ulong totalBodyBytes = ULZERO;

            if (type == Constants.FrameMethod)
            {
                m_method = m_protocol.DecodeMethodFrom(reader);
            }
            else if (type == Constants.FrameHeader)
            {
                m_content = m_protocol.DecodeContentHeaderFrom(reader);
                totalBodyBytes = m_content.ReadFrom(reader);
            }
            else if (type == Constants.FrameBody)
            {
                payload = reader.ReadBytes(Convert.ToInt32(payloadSize));

                if (payload.Length != payloadSize)
                {
                    // Early EOF.
                    throw new MalformedFrameException(String.Concat(new string[]{"Short frame - expected " ,
                                                      payloadSize.ToString(), " bytes, got " ,
                                                      payload.Length.ToString() , " bytes" }));
                }
            }

            byte frameEndMarker = reader.ReadByte();
            if (frameEndMarker != Constants.FrameEnd)
            {
                throw new MalformedFrameException("Bad frame end marker: " + frameEndMarker.ToString());
            }

            return new InboundFrame((FrameType)type, channel, payload, m_method, m_content, totalBodyBytes);
        }
        private static readonly byte[] EmptyByteArray = new byte[] { };
    }
    public class Frame
    {
        private const ushort USZERO = default(ushort);
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


        public bool IsMainChannel()
        {
            return Channel == USZERO;
        }
        public byte[] Payload { get; private set; }

        public FrameType Type { get; private set; }

        public override string ToString()
        {
            var len = Payload == null ? "(null)" : Payload.Length.ToString();
            return $"(type={Type}, channel={Channel.ToString()}, {len} bytes of payload)";
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

    public enum FrameType : byte
    {
        FrameMethod = 1,
        FrameHeader = 2,
        FrameBody = 3,
        FrameHeartbeat = 8
    }
}
