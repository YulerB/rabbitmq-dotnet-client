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
    public class Command<T> : Command where T: IMethod
    {

        public Command(T method) : this(method, null, null)
        {
        }

        public Command(T method, RabbitMQ.Client.Impl.BasicProperties header, FrameBuilder body) : base(method, header, body) { }
    }
    public class Command
    {
        // EmptyFrameSize, 8 = 1 + 2 + 4 + 1
        // - 1 byte of frame type
        // - 2 bytes of channel number
        // - 4 bytes of frame payload length
        // - 1 byte of payload trailer FrameEnd byte

        static Command()
        {
            CheckEmptyFrameSize();
        }
        private static void CheckEmptyFrameSize()
        {
            long actualLength = 0;
            {
                var x = new EmptyOutboundFrame();
                actualLength = x.EstimatedSize();
            }

            if (Constants.EmptyFrameSize != actualLength)
            {
                string message =
                    string.Format("EmptyFrameSize is incorrect - defined as {0} where the computed value is in fact {1}.",
                        Constants.EmptyFrameSize,
                        actualLength);
                throw new ProtocolViolationException(message);
            }
        }

        public Command(IMethod method) : this(method, null, null)
        {
        }

        public Command(IMethod method, RabbitMQ.Client.Impl.BasicProperties header, FrameBuilder body)
        {
            Method = method;
            Header = header;
            Body = body ?? new FrameBuilder();
        }

        public FrameBuilder Body { get; private set; }

        public RabbitMQ.Client.Impl.BasicProperties Header { get; private set; }

        public IMethod Method { get; private set; }

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
                var frameMaxEqualsZero = frameMax == 0;
                var bodyPayloadMax = frameMaxEqualsZero ? body.Length : frameMax - Constants.EmptyFrameSize;

                var frames = new List<OutboundFrame>(2 + Convert.ToInt32(body.Length / bodyPayloadMax) )
                {
                    new MethodOutboundFrame(channelNumber, Method),
                    new HeaderOutboundFrame(channelNumber, Header, body.Length)
                };

                using (ArraySegmentSequence sequence = new ArraySegmentSequence(body.ToData()))
                {
                    for (long offset = 0; offset < body.Length; offset += bodyPayloadMax)
                    {
                        var remaining = body.Length - offset;
                        var count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;

                        frames.Add(new BodySegmentOutboundFrame(channelNumber, new ArraySegment<byte>(sequence.ReadBytes((int)count), 0, (int)count)));
                    }
                }
                connection.WriteFrameSet(frames);
            }
            else
            {
                connection.WriteFrameSet(new List<OutboundFrame>
                {
                    new MethodOutboundFrame(channelNumber, Method)
                });
            }
        }

        public static List<OutboundFrame> CalculateFrames(ushort channelNumber, Connection connection, IList<Command> commands)
        {
            var frameMax = Math.Min(uint.MaxValue, connection.FrameMax);
            var frames = new List<OutboundFrame>(commands.Count*3);
            var frameMaxEqualsZero = frameMax == 0;
            foreach (var cmd in commands)
            {
                frames.Add(new MethodOutboundFrame(channelNumber, cmd.Method));
                if (cmd.Method.HasContent)
                {
                    var body = cmd.Body;
                    using (ArraySegmentSequence sequence = new ArraySegmentSequence(body.ToData()))
                    {
                        frames.Add(new HeaderOutboundFrame(channelNumber, cmd.Header, body.Length));
                        var bodyPayloadMax = frameMaxEqualsZero ? body.Length : frameMax - Constants.EmptyFrameSize;
                        for (long offset = 0; offset < body.Length; offset += bodyPayloadMax)
                        {
                            var remaining = body.Length - offset;
                            var count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;
                            frames.Add(new BodySegmentOutboundFrame(channelNumber, new ArraySegment<byte>(sequence.ReadBytes((int)count), 0, (int)count)));
                        }
                    }
                }
            }

            return frames;
        }
    }
}
