#if !NETFX_CORE
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Net.Security;
using System.IO;
using System.Collections.Generic;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client
{
    public class HyperTcpSecureClientAdapter : IHyperTcpClient
    {
        private readonly HyperTcpClientSettings settings;
        public event EventHandler Closed;
        private Socket sock;
        private SslStream baseSSLStream;
        public event EventHandler<ArraySegment<byte>> Receive;
        private readonly object _syncLock = new object();
        private AsyncCallback asyncCallback;
        private StreamRingBuffer ringBuffer;
        public HyperTcpSecureClientAdapter(HyperTcpClientSettings settings)
        {
            this.settings = settings;
            sock = new Socket(settings.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            sock.ReceiveTimeout = Math.Max(sock.ReceiveTimeout, settings.RequestedHeartbeat * 1000);
            sock.SendTimeout = Math.Max(sock.SendTimeout, settings.RequestedHeartbeat * 1000);
            ringBuffer = new StreamRingBuffer(sock.ReceiveBufferSize * 20);
            asyncCallback = new AsyncCallback(SecureRead);
        }

        public virtual void BufferUsed(int size)
        {
            ringBuffer.Release(size);
        }
        public virtual void Close()
        {
            baseSSLStream?.Close();
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
                baseSSLStream?.Dispose();
                sock.Dispose();
                disposed = true;
            }
            ringBuffer = null;
            Receive = null;
            baseSSLStream = null;
            sock = null;
            asyncCallback = null;
        }
        #endregion
        public int ClientLocalEndPointPort => ((IPEndPoint)sock.LocalEndPoint).Port;
        public virtual async Task ConnectAsync()
        {
            var adds = await Dns.GetHostAddressesAsync(settings.EndPoint.HostName);
            var ep = TcpClientAdapterHelper.GetMatchingHost(adds, sock.AddressFamily);

            if (ep == default(IPAddress))
            {
                throw new ArgumentException("No ip address could be resolved for " + settings.EndPoint.HostName);
            }

#if CORECLR
            await sock.ConnectAsync(ep, settings.EndPoint.Port);
#else
            sock.Connect(ep, settings.EndPoint.Port);
#endif

            baseSSLStream = new SslStream(
                new NetworkStream(sock),
                false,
                settings.EndPoint.Ssl.CertificateValidationCallback,
                settings.EndPoint.Ssl.CertificateSelectionCallback);

            await baseSSLStream.AuthenticateAsClientAsync(
                settings.EndPoint.HostName,
                settings.EndPoint.Ssl.Certs,
                settings.EndPoint.Ssl.Version,
                settings.EndPoint.Ssl.CheckCertificateRevocation);

            var peek = ringBuffer.Peek();

            baseSSLStream.BeginRead(
                peek.Array,
                peek.Offset,
                peek.Count,
                asyncCallback,
                null
            );
        }

        public void Write(ArraySegment<byte> data)
        {
            lock (_syncLock)
            {
                baseSSLStream.Write(data.Array, data.Offset, data.Count);
            }
        }

        private void SecureRead(IAsyncResult result)
        {
            try
            {
                if (!disposed)
                {
                    int read = baseSSLStream.EndRead(result);
                    if (read > 0) this.Receive?.Invoke(this, ringBuffer.Take(read));
                    var peek = ringBuffer.Peek();
                    baseSSLStream.BeginRead(
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
            }
            catch (System.IO.FileNotFoundException)
            {
                // Nothing to do here. 
            }
            catch (IOException)
            {
                Close();
            }
        }

        public void Write(IList<ArraySegment<byte>> data)
        {
            using (var ms = MemoryStreamPool.GetObject())
            {
                foreach (var segment in data)
                {
                    ms.Instance.Write(segment.Array, segment.Offset, segment.Count);
                }
                lock (_syncLock) {
                    Write(new ArraySegment<byte>(ms.Instance.GetBuffer(), 0, Convert.ToInt32(ms.Instance.Length)));
                }
            }
        }
    }
}
#endif