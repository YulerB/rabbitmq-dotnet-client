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

#if !NETFX_CORE
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Impl
{
    public class ArraySegmentStream 
    {
        private BlockingCollection<ReadOnlyMemory<byte>> data = new BlockingCollection<ReadOnlyMemory<byte>>();
        public event EventHandler<BufferUsedEventArgs> BufferUsed;
        public ArraySegmentStream(byte[] buffer) {
            data.Add(new ArraySegment<byte>(buffer, 0, buffer.Length));
        }
        public ArraySegmentStream(ArraySegment<byte> buffer)
        {
            data.Add(buffer);
        }
        public ArraySegmentStream(IEnumerable<ArraySegment<byte>> buffers)
        {
            foreach(var buffer in buffers)
                data.Add(buffer);
        }
        public ArraySegmentStream() { }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                data.Dispose();
            }
            BufferUsed = null;
            data = null;
        }

        private ReadOnlyMemory<byte> top = new ReadOnlyMemory<byte>();
        private readonly ArraySegment<byte> empty = new ArraySegment<byte>();
        private int originalSize = 0;
        public ReadOnlyMemory<byte>[] Read(int count)
        {
            List<ReadOnlyMemory<byte>> result = new List<ReadOnlyMemory<byte>>();

            while (count > 0)
            {
                if (top.Length == 0)
                {
                    top = data.Take();

                    if (data.IsCompleted && top.IsEmpty)
                        throw new EndOfStreamException();

                    originalSize = top.Length;
                }

                if (top.Length > count)
                {
                    var read = top.Slice(0, count);
                    top = top.Slice(count, top.Length - count);
                    result.Add(read);
                    return result.ToArray();
                }
                else
                {
                    var read = top.Slice(0, top.Length);
                    count -= top.Length;
                    top = empty;
                    BufferUsed?.Invoke(this, new BufferUsedEventArgs { Size = originalSize });
                    result.Add(read);
                }
            }
            return result.ToArray();
        }
        public void Write(ReadOnlyMemory<byte> buffer)
        {
            data.Add(buffer);
        }
        internal void NotifyClosed()
        {
            data.CompleteAdding();
        }
    }
}
#endif
