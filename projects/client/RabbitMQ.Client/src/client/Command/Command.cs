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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Framing;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Impl
{
    public abstract class SendCommandBase<T> where T : IMethod
    {
        public SendCommandBase(T method)
        {
            Method = method;
            Body = default(byte[]);
        }
        public SendCommandBase(T method, RabbitMQ.Client.Impl.BasicProperties header, byte[] body)
        {
            Method = method;
            Header = header;
            Body = body ?? default(byte[]);
        }

        public byte[] Body { get; private set; }

        public RabbitMQ.Client.Impl.BasicProperties Header { get; private set; }

        public T Method { get; private set; }
    }

    public class SendCommand<T> : SendCommandBase<T> where T : IMethod
    {
        private const uint UZERO = 0U;
        private const long LZERO = 0L;
        public SendCommand(T method) : base(method)
        {
        }

        public SendCommand(T method, RabbitMQ.Client.Impl.BasicProperties header, byte[] body) : base(method, header, body)
        {
        }

        public void Transmit(ushort channelNumber, Connection connection)
        {
            if (Method.HasContent)
            {
                TransmitAsFrameSet(channelNumber, connection);
            }
            else
            {
                TransmitAsSingleFrame(channelNumber, connection);
            }
        }

        private void TransmitAsSingleFrame(ushort channelNumber, Connection connection)
        {
            connection.WriteFrame(new MethodOutboundFrame(channelNumber, Method));
        }

        private void TransmitAsFrameSet(ushort channelNumber, Connection connection)
        {
            if (Method.HasContent)
            {
                var body = Body;
                var frameMax = Math.Min(uint.MaxValue, connection.FrameMax);
                var frameMaxEqualsZero = frameMax == UZERO;
                var bodyPayloadMax = frameMaxEqualsZero ? body.Length : frameMax - Constants.EmptyFrameSize;

                var frames = new List<OutboundFrame>(2 + Convert.ToInt32(body.Length / bodyPayloadMax))
                {
                    new MethodOutboundFrame(channelNumber, Method),
                    new HeaderOutboundFrame(channelNumber, Header, (ulong) body.Length)
                };

                var bodyLength = body.Length;
                for (long offset = LZERO; offset < bodyLength; offset += bodyPayloadMax)
                {
                    var remaining = bodyLength - offset;
                    var count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;

                    frames.Add(new BodySegmentOutboundFrame(channelNumber, new ArraySegment<byte>(body, Convert.ToInt32(offset), Convert.ToInt32(count))));
                }

                connection.WriteFrameSet(frames);
            }
            else
            {
                connection.WriteFrame(new MethodOutboundFrame(channelNumber, Method));
            }
        }
    }

    public static class CommandHelpers
    {
        private const uint UZERO = 0U;
        private const long LZERO = 0L;
        public static List<OutboundFrame> CalculateFrames<T>(ushort channelNumber, Connection connection, IList<SendCommand<T>> commands) where T : IMethod
        {
            var frameMax = Math.Min(uint.MaxValue, connection.FrameMax);
            var frames = new List<OutboundFrame>(commands.Count * 3);
            var frameMaxEqualsZero = frameMax == UZERO;
            foreach (var cmd in commands)
            {
                frames.Add(new MethodOutboundFrame(channelNumber, cmd.Method));
                if (cmd.Method.HasContent)
                {
                    var body = cmd.Body;

                    frames.Add(new HeaderOutboundFrame(channelNumber, cmd.Header, (ulong)body.Length));
                    var bodyPayloadMax = frameMaxEqualsZero ? body.Length : frameMax - Constants.EmptyFrameSize;


                    var bodyLength = body.Length;
                    for (long offset = LZERO; offset < bodyLength; offset += bodyPayloadMax)
                    {
                        var remaining = bodyLength - offset;
                        var count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;
                        frames.Add(new BodySegmentOutboundFrame(channelNumber, new ArraySegment<byte>(body, (int)offset, (int)count)));
                    }

                }
            }

            return frames;
        }
    }

    public abstract class AssembledCommandBase<T> where T : class
    {
        public AssembledCommandBase(IMethod method)
        {
            Method = method;
            Body = default(T);
        }
        public AssembledCommandBase(IMethod method, RabbitMQ.Client.Impl.BasicProperties header, T body)
        {
            Method = method;
            Header = header;
            Body = body ?? default(T);
        }

        public T Body { get; private set; }

        public RabbitMQ.Client.Impl.BasicProperties Header { get; private set; }

        public IMethod Method { get; private set; }
    }
    public class AssembledCommand : AssembledCommandBase<FrameBuilder>
    {
        public AssembledCommand(IMethod method) : base(method)
        {
        }
        public AssembledCommand(IMethod method, RabbitMQ.Client.Impl.BasicProperties header, FrameBuilder body) : base(method, header, body)
        {
        }
    }
}