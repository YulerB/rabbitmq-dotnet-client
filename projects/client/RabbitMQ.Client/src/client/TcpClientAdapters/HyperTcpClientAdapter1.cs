#if !NETFX_CORE
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO;
using System.Collections.Generic;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client
{
    public class HyperTcpClientAdapter1 : IHyperTcpClient
    {
        public event EventHandler Closed;
        private Socket sock;
        private NetworkStream baseStream;
        public event EventHandler<Memory<byte>> Receive;
        private readonly object _syncLock = new object();
        private AsyncCallback asyncCallback;
        private readonly HyperTcpClientSettings settings;
        private StreamRingBuffer ringBuffer;

        public HyperTcpClientAdapter1(HyperTcpClientSettings settings)
        {
            this.settings = settings;
            sock = new Socket(settings.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            sock.ReceiveTimeout = Math.Max(sock.ReceiveTimeout, settings.RequestedHeartbeat * 1000);
            sock.SendTimeout = Math.Max(sock.SendTimeout, settings.RequestedHeartbeat * 1000);
            ringBuffer = new StreamRingBuffer(sock.ReceiveBufferSize * 20);
        }

        public virtual void BufferUsed(int size)
        {
            ringBuffer.Release(size);
        }
        public virtual void Close()
        {
            baseStream?.Close();
            sock?.Close();
            Closed?.Invoke(this, EventArgs.Empty);
        }
        #region IDisposable Support
        private bool disposed;
        public virtual void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                baseStream?.Dispose();
                sock.Dispose();
                disposed = true;
            }
            ringBuffer = null;
            Receive = null;
            baseStream = null;
            sock = null;
            asyncCallback = null;
        }
        #endregion
        public int ClientLocalEndPointPort => ((IPEndPoint)sock.LocalEndPoint).Port;
        public virtual Task ConnectAsync()
        {
            return ConnectAsync(settings.EndPoint.HostName, settings.EndPoint.Port);
        }
        private async Task ConnectAsync(string host, int port)
        {
            var adds = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            var ep = TcpClientAdapterHelper.GetMatchingHost(adds, sock.AddressFamily);
            if (ep == default(IPAddress))
            {
                throw new ArgumentException("No ip address could be resolved for " + host);
            }
#if CORECLR
            await sock.ConnectAsync(ep, port).ConfigureAwait(false);
#else
            sock.Connect(ep, port);
#endif
            baseStream = new NetworkStream(sock);

            asyncCallback = new AsyncCallback(Read);

            var peek = ringBuffer.Peek();
            baseStream.BeginRead(
                peek.Array,
                peek.Offset,
                peek.Count,
                asyncCallback,
                null
            );
        }
        public void Write(ArraySegment<byte> data)
        {
            baseStream.Write(data.Array, data.Offset, data.Count);
        }
        private void Read(IAsyncResult result)
        {
            try
            {
                if (!disposed)
                {
                    int read = baseStream.EndRead(result);

                    if (read > 0) this.Receive?.Invoke(this, ringBuffer.Take(read));

                    var peek = ringBuffer.Peek();

                    baseStream.BeginRead(
                        peek.Array,
                        peek.Offset,
                        peek.Count,
                        asyncCallback,
                        null
                    );
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                Close();
            }
            catch (System.ObjectDisposedException)
            {
                // Nothing to do here.
                Closed?.Invoke(this, EventArgs.Empty);
            }
            catch (System.IO.FileNotFoundException)
            {
                // Nothing to do here.
                Closed?.Invoke(this, EventArgs.Empty);
            }
            catch (IOException)
            {
                Close();
            }
        }

        public void Write(List<ArraySegment<byte>> data)
        {
            using (var ms = MemoryStreamPool.GetObject())
            {
                foreach (var segment in data)
                {
                    ms.Instance.Write(segment.Array, segment.Offset, segment.Count);
                }
                Write(new ArraySegment<byte>(ms.Instance.GetBuffer(), 0, Convert.ToInt32(ms.Instance.Length)));
            }
        }
    }
}
#endif