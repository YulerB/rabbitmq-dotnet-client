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
    public class ArraySegmentSequence : IDisposable
    {
        private bool addingComplete=false;
        private ConcurrentQueue<ReadOnlyMemory<byte>> data = new ConcurrentQueue<ReadOnlyMemory<byte>>();
        public event EventHandler<BufferUsedEventArgs> BufferUsed;
        private ReadOnlyMemory<byte> top = new ReadOnlyMemory<byte>();
        private readonly ArraySegment<byte> empty = new ArraySegment<byte>();
        private int originalSize = 0;

        #region Constructor
        public ArraySegmentSequence(byte[] buffer)
        {
            data.Enqueue(new ArraySegment<byte>(buffer, 0, buffer.Length));
        }
        public ArraySegmentSequence(ArraySegment<byte> buffer)
        {
            data.Enqueue(buffer);
        }
        public ArraySegmentSequence(IEnumerable<ArraySegment<byte>> buffers)
        {
            foreach (var buffer in buffers) data.Enqueue(buffer);
        }
        public ArraySegmentSequence() { }
        #endregion

        public List<ReadOnlyMemory<byte>> ReadNotExpecting(int count)
        {
            List<ReadOnlyMemory<byte>> result = new List<ReadOnlyMemory<byte>>();

            while (count > 0)
            {
                if (top.Length == 0)
                {
                    lock (data)
                    {
                        while (!addingComplete && !data.TryDequeue(out top))// If we have items remaining in the queue, skip over this. 
                        {
                            Monitor.Wait(data);// Release the lock and block on this line until someone adds something to the queue, resuming once they release the lock again.
                        }
                    }

                    if (top.Length == 0 && addingComplete) throw new EndOfStreamException();

                    originalSize = top.Length;
                }

                if (top.Length > count)
                {
                    result.Add(top.Slice(0, count));
                    top = top.Slice(count, top.Length - count);
                    return result;
                }
                else
                {
                    result.Add(top.Slice(0, top.Length));
                    count -= top.Length;
                    top = empty;
                    BufferUsed?.Invoke(this, new BufferUsedEventArgs(originalSize));
                }
            }
            return result;
        }
        public List<ReadOnlyMemory<byte>> Read(int count)
        {
            List<ReadOnlyMemory<byte>> result = new List<ReadOnlyMemory<byte>>();

            while (count > 0)
            {
                if (top.Length == 0)
                {
                    if (data.Count == 0 && addingComplete == false)
                        SpinWait.SpinUntil(() => addingComplete || data.Count > 0);

                    if (!data.TryDequeue(out top) && addingComplete) throw new EndOfStreamException();

                    originalSize = top.Length;
                }

                if (top.Length > count)
                {
                    result.Add(top.Slice(0, count));
                    top = top.Slice(count, top.Length - count);
                    return result;
                }
                else
                {
                    result.Add(top.Slice(0, top.Length));
                    count -= top.Length;
                    top = empty;
                    BufferUsed?.Invoke(this, new BufferUsedEventArgs(originalSize));
                }
            }
            return result;
        }
        public void Write(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length > 0)
            {
                lock (data)
                {
                    data.Enqueue(buffer);
                    // If the consumer thread is waiting for an item
                    // to be added to the queue, this will move it
                    // to a waiting list, to resume execution
                    // once we release our lock.
                    Monitor.Pulse(data);
                }
            }
        }
        internal void NotifyClosed()
        {
            lock (data)
            {
                addingComplete = true;
                // If the consumer thread is waiting for an item
                // to be added to the queue, this will move it
                // to a waiting list, to resume execution
                // once we release our lock.
                Monitor.Pulse(data);
            }
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) NotifyClosed();
            top = null;
            data = null;
            BufferUsed = null;
        }
        #endregion
    }
}
#endif