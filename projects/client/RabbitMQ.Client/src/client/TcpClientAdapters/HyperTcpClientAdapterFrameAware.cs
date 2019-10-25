#if !NETFX_CORE
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace RabbitMQ.Client
{
    public class HyperTcpClientAdapterFrameAware : IHyperTcpClient
    {
        public event EventHandler Closed;
        private Socket sock;
        public event EventHandler<Memory<byte>> Receive;
        private readonly HyperTcpClientSettings settings;
        private SocketAsyncEventArgs sEvent;
        private Memory<byte> top = Memory<byte>.Empty;
        private ConcurrentQueue<Memory<byte>> receivedSegments = new ConcurrentQueue<Memory<byte>>();
        private volatile bool closed;
        private const uint UZERO = 0U;

        public HyperTcpClientAdapterFrameAware(HyperTcpClientSettings settings)
        {
            this.settings = settings;
            sock = new Socket(settings.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            sock.ReceiveTimeout = Math.Max(sock.ReceiveTimeout, settings.RequestedHeartbeat * 1000);
            sock.SendTimeout = Math.Max(sock.SendTimeout, settings.RequestedHeartbeat * 1000);
            sEvent = new SocketAsyncEventArgs { AcceptSocket = sock };
            sEvent.Completed += SEvent_Completed;
        }
        public virtual void BufferUsed(int size)
        {
            //ringBuffer.Release(size);
        }
        public virtual void Close()
        {
            sock?.Close();
            Closed?.Invoke(this, EventArgs.Empty);
            lock (receivedSegments)
            {
                closed = true;
                Monitor.Pulse(receivedSegments);
            }
        }
        public int ClientLocalEndPointPort => ((IPEndPoint)sock.LocalEndPoint).Port;
        public virtual async Task ConnectAsync()
        {
            var adds = await Dns.GetHostAddressesAsync(settings.EndPoint.HostName).ConfigureAwait(false);
            var ep = TcpClientAdapterHelper.GetMatchingHost(adds, sock.AddressFamily);
            if (ep == default(IPAddress))
            {
                throw new ArgumentException("No ip address could be resolved for " + settings.EndPoint.HostName);
            }

#if CORECLR
            await sock.ConnectAsync(ep, settings.EndPoint.Port).ConfigureAwait(false);
#else
            sock.Connect(ep, settings.EndPoint.Port);
#endif

            sEvent.SetBuffer(new byte[sock.ReceiveBufferSize * 2], 0, sock.ReceiveBufferSize * 2);

            Thread t = new Thread(new ThreadStart(BeginReceive))
            {
                IsBackground = true
            };
            Thread t1 = new Thread(new ThreadStart(Process))
            {
                IsBackground = true
            };
            t.Start();
            t1.Start();
        }

        private void BeginReceive()
        {
            SEvent_Completed(this, sEvent);
        }
        private void SEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                do
                {
                    if (e.BytesTransferred > 0)
                    {
                        lock (receivedSegments)
                        {
                            byte[] received = new byte[e.BytesTransferred];
                            e.Buffer.AsMemory(e.Offset, e.BytesTransferred).CopyTo(received);
                            receivedSegments.Enqueue(received.AsMemory());
                            Monitor.Pulse(receivedSegments);
                        }
                    }
                }
                while (e.SocketError == SocketError.Success && !sock.ReceiveAsync(e));
                if (e.SocketError != SocketError.Success)
                    Close();
            }
            catch (ObjectDisposedException)
            {
                // Ignore, we need to stop reading when the socket is disposed.
            }
            catch (SocketException)
            {
                // Ignore, we need to stop reading when the socket is disposed.
            }
        }
        
        private void Process()
        {
            byte[] header = new byte[8];
            uint payloadSize = UZERO;

            while (!closed)
            {
                Fill(header);
                payloadSize = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan().Slice(3));
                byte[] frameData = new byte[payloadSize + 8];
                header.AsSpan().CopyTo(frameData.AsSpan());
                if (payloadSize > UZERO) Fill(frameData, 8);
                this.Receive?.Invoke(this, new ArraySegment<byte>(frameData, 0, frameData.Length));
            }
        }
        private void Fill(byte[] payload)
        {
            Fill(payload, 0);
        }
        private void Fill(byte[] payload, int offset)
        {
            var plm = payload.AsMemory();
            if (offset > 0) plm = plm.Slice(offset);

            while(plm.Length > 0)
            {
                if (top.IsEmpty)
                {
                    lock (receivedSegments)
                    {
                        while (!receivedSegments.TryDequeue(out top))
                        {
                            Monitor.Wait(receivedSegments);// Release the lock and block on this line until someone adds something to the queue, resuming once they release the lock again.
                        }
                    }
                }

                if (top.Length == plm.Length)
                {
                    top.CopyTo(plm);
                    top = Memory<byte>.Empty;
                    plm = Memory<byte>.Empty;
                }
                else if (top.Length > plm.Length)
                {
                    top.Slice(0, plm.Length).CopyTo(plm);
                    top = top.Slice(plm.Length);
                    plm = Memory<byte>.Empty; ;
                }
                else
                {
                    top.CopyTo(plm);
                    plm = plm.Slice(top.Length);
                    top = Memory<byte>.Empty;
                }
            }
        }

        public void Write(ArraySegment<byte> data)
        {
            sock.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
        }
        public void Write(List<ArraySegment<byte>> data)
        {
            sock.Send(data);
        }

        #region IDisposable Support
        public virtual void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                sock.Dispose();
                sEvent.Dispose();
            }
            Receive = null;
            sock = null;
            sEvent = null;
            Closed = null;
        }
        #endregion
    }
}
#endif