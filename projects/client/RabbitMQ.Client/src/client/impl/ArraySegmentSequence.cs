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
    public class ArraySegmentSequence : IDisposable
    {
        public event EventHandler<int> BufferUsed;
        public event EventHandler<EventArgs> EndOfStreamEvent;

        private bool addingComplete=false;
        private ConcurrentQueue<Memory<byte>> data = new ConcurrentQueue<Memory<byte>>();
        private Memory<byte> top = new Memory<byte>();
        private readonly ArraySegment<byte> empty = new ArraySegment<byte>();
        private const int ZERO = 0;
        private const long LZERO = 0L;
        private long length = LZERO;
        private int originalSize = ZERO;
        private readonly List<Memory<byte>> result = new List<Memory<byte>>(10);

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

        public bool Peek(int count, out byte[] peeked)
        {
            if (count == ZERO)
            {
                peeked = new byte[ZERO];
                return true;
            }
            if (count > length)
            {
                peeked = new byte[ZERO];
                return false;
            }

            peeked = new byte[count];
            int i = ZERO;
            if (top.Length > ZERO)
            {
                int diff = Math.Min(top.Length, count);
                top.Slice(ZERO, diff).CopyTo(peeked);
                count -= diff;
                i += diff;

                if (count == ZERO) return true;
            }

            if (data.TryPeek(out Memory<byte> p))
            {
                int diff = Math.Min(p.Length, count);
                p.Slice(ZERO, diff).CopyTo(peeked.AsMemory().Slice(i));
                count -= diff;
                i += diff;

                if (count == ZERO) return true;
            }
            else
            {
                return false;
            }

            var contents = data.ToArray();
            if (contents.Length > 1)
            {
                for (int j = 1; j < data.Count; j++)
                {
                    int diff = Math.Min(contents[j].Length, count);
                    contents[j].Slice(ZERO, diff).CopyTo(peeked.AsMemory().Slice(i));
                    count -= diff;
                    i += diff;

                    if (count == ZERO) return true;
                }
            }

            return false;
        }
        public long Length => length;

        public List<Memory<byte>> ReadNotExpecting(int count)
        {
            result.Clear();
            while (count > ZERO)
            {
                if (top.IsEmpty)
                {
                    lock (data)
                    {
                        while (!addingComplete && !data.TryDequeue(out top))// If we have items remaining in the queue, skip over this. 
                        {
                            Monitor.Wait(data);// Release the lock and block on this line until someone adds something to the queue, resuming once they release the lock again.
                        }
                    }

                    if (top.IsEmpty && addingComplete)
                    {
                        EndOfStreamEvent?.Invoke(this, EventArgs.Empty);
                        throw new EndOfStreamException();
                    }
                    originalSize = top.Length;
                }

                if (top.Length > count)
                {
                    result.Add(top.Slice(ZERO, count));
                    top = top.Slice(count, top.Length - count);
                    Interlocked.Add(ref length, -count);
                    return result;
                }
                else if (top.Length == count)
                {
                    result.Add(top.Slice(ZERO, top.Length));
                    top = empty;
                    BufferUsed?.Invoke(this, originalSize);
                    Interlocked.Add(ref length, -count);
                    return result;
                }
                else
                {
                    result.Add(top.Slice(ZERO, top.Length));
                    count -= top.Length;
                    top = empty;
                    Interlocked.Add(ref length, -top.Length);
                    BufferUsed?.Invoke(this, originalSize);
                }
            }
            return result;
        }
        public List<Memory<byte>> Read(int count)
        {
            result.Clear();
            while (count > ZERO)
            {
                if (top.IsEmpty)
                {
                    if (data.IsEmpty && !addingComplete)
                        SpinWait.SpinUntil(() => addingComplete || data.Count > ZERO);

                    if (!data.TryDequeue(out top) && addingComplete)
                    {
                        EndOfStreamEvent?.Invoke(this, EventArgs.Empty);
                        throw new EndOfStreamException();
                    }

                    originalSize = top.Length;
                }

                if (top.Length > count)
                {
                    result.Add(top.Slice(ZERO, count));
                    top = top.Slice(count, top.Length - count);
                    Interlocked.Add(ref length, -count);
                    return result;
                }
                else if (top.Length == count)
                {
                    result.Add(top.Slice(ZERO, top.Length));
                    top = empty;
                    BufferUsed?.Invoke(this, originalSize);
                    Interlocked.Add(ref length, -count);
                    return result;
                }
                else
                {
                    result.Add(top.Slice(ZERO, top.Length));
                    count -= top.Length;
                    top = empty;
                    BufferUsed?.Invoke(this, originalSize);
                    Interlocked.Add(ref length, -top.Length);
                }
            }
            return result;
        }
        public void Skip(int count)
        {
            while (count > ZERO)
            {
                if (top.IsEmpty)
                {
                    if (data.IsEmpty && !addingComplete)
                        SpinWait.SpinUntil(() => addingComplete || data.Count > ZERO);

                    if (!data.TryDequeue(out top) && addingComplete)
                    {
                        EndOfStreamEvent?.Invoke(this, EventArgs.Empty);
                        throw new EndOfStreamException();
                    }

                    originalSize = top.Length;
                }

                if (top.Length > count)
                {
                    top = top.Slice(count, top.Length - count);
                    Interlocked.Add(ref length, -count);
                    return;
                }
                else if (top.Length == count)
                {
                    top = empty;
                    BufferUsed?.Invoke(this, originalSize);
                    Interlocked.Add(ref length, -count);
                    return;
                }
                else
                {
                    count -= top.Length;
                    top = empty;
                    BufferUsed?.Invoke(this, originalSize);
                    Interlocked.Add(ref length, -top.Length);
                }
            }
        }
        public void Write(Memory<byte> buffer)
        {
            if (buffer.Length > ZERO)
            {
                lock (data)
                {
                    data.Enqueue(buffer);
                    Interlocked.Add(ref length , buffer.Length);
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