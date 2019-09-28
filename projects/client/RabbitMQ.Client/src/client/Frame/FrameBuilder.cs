﻿// This source code is dual-licensed under the Apache License, version
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

#if !NETFX_CORE
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Impl
{
    public class FrameBuilder  
    {
        public FrameBuilder() {
            data = new List<ArraySegment<byte>>(10);
        }
        public FrameBuilder(byte[] bytes) {

            data = new List<ArraySegment<byte>>
            {
                new ArraySegment<byte>(bytes, 0, bytes.Length)
            };
            len += bytes.Length;
        }

        private long len = 0;
        private List<ArraySegment<byte>> data;

        public  long Length => len;

        public IList<ArraySegment<byte>> ToData (){ return data; }

        public void Write(byte[] buffer, int offset, int count)
        {
            data.Add(new ArraySegment<byte>(buffer, offset, count));
            len += count;
        }
        public void Write(ArraySegment<byte> newData)
        {
            data.Add(newData);
            len += newData.Count;
        }
        public void WriteByte(byte buffer)
        {
            data.Add(new ArraySegment<byte>(new byte[] { buffer }, 0, 1));
            len += 1;
        }
        internal void WriteSegments(IList<ArraySegment<byte>> content, uint written1)
        {
            data.AddRange(content);
            len += written1;
        }

        public byte[] ToByteArray()
        {
            MemoryStream stream = new MemoryStream((int)len);
            foreach(var item in data)
            {
                stream.Write(item.Array, item.Offset, item.Count);
            }
            return stream.ToArray();
        }
    }
}
#endif
