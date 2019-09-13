#if !NETFX_CORE
using System;
using System.Threading;

namespace RabbitMQ.Client
{
    public class StreamRingBuffer
    {
        private readonly ReadOnlyMemory<byte> Memory = null;
        private readonly byte[] bigBuffer = null;
        private int position = 0;
        private int available = 0;
        private readonly int capacity = 0;
        public StreamRingBuffer(int capacity)
        {
            this.capacity = capacity;
            bigBuffer = new byte[capacity];
            Memory = new ReadOnlyMemory<byte>(bigBuffer);
            available = capacity;
        }

        public ArraySegment<byte> Peek()
        {
            var pos = position;
            return new ArraySegment<byte>(bigBuffer, pos, Math.Min(available, capacity - pos));
        }

        public ReadOnlyMemory<byte> Take(int usedSize)
        {
            Interlocked.Add(ref available, -usedSize);
            var pos = position;
            ReadOnlyMemory<byte> mem = Memory.Slice(pos, usedSize);
            pos += usedSize;
            if (pos == capacity) pos = 0;
            position = pos;
            return mem;
        }

        public void Release(int releaseSize)
        {
            Interlocked.Add(ref available , releaseSize);
        }
    }
}
#endif