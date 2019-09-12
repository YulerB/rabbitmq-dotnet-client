#if !NETFX_CORE
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO;
using System.Collections.Generic;

namespace RabbitMQ.Client
{
    public class StreamRingBuffer
    {
        private ReadOnlyMemory<byte> Memory = null;
        private readonly byte[] bigBuffer = null;
        private int position = 0;
        private int available = 0;
        private readonly int capacity = 0;
        private readonly int end = 0;
        public StreamRingBuffer(int capacity)
        {
            this.capacity = capacity;
            bigBuffer = new byte[capacity];
            Memory = new ReadOnlyMemory<byte>(bigBuffer);
            available = capacity;
            end = capacity - 1;
        }

        public ArraySegment<byte> Peek()
        {
            return new ArraySegment<byte>(bigBuffer, position, Math.Min(available, capacity - position));
        }

        public ReadOnlyMemory<byte> Take(int usedSize)
        {
            int change = Math.Abs(usedSize);
            ReadOnlyMemory<byte> mem = Memory.Slice(position, change);
            position += change;
            Interlocked.Add(ref available, -change);
            if (position > end) position = 0;
            return mem;
        }

        public void Release(int releaseSize)
        {
            Interlocked.Add(ref available , releaseSize);
        }

    }
    public class HyperTcpClientAdapter : IHyperTcpClient
    {
        public event EventHandler Closed;
        private Socket sock;
        private NetworkStream baseStream;
        private SslStream baseSSLStream;
        public event EventHandler<ReadOnlyMemory<byte>> Receive;
        private readonly object _syncLock = new object();
        private AsyncCallback asyncCallback;

        private StreamRingBuffer ringBuffer;

        public HyperTcpClientAdapter(HyperTcpClientSettings settings)
        {
            sock = new Socket(settings.AddressFamily, SocketType.Stream, ProtocolType.Tcp){NoDelay = true};
            sock.ReceiveTimeout = Math.Max(sock.ReceiveTimeout, settings.RequestedHeartbeat * 1000);
            sock.SendTimeout = Math.Max(sock.SendTimeout, settings.RequestedHeartbeat * 1000);
            ringBuffer = new StreamRingBuffer(sock.ReceiveBufferSize * 10);
        }

        public virtual void BufferUsed(int size)
        {
            ringBuffer.Release(size);
        }
        public virtual void Close()
        {
            baseStream?.Close();
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
                baseStream?.Dispose();
                baseSSLStream?.Dispose();
                sock.Dispose();
                disposed = true;
            }
            ringBuffer = null;
            Receive = null;
            baseStream = null;
            baseSSLStream = null;
            sock = null;
            asyncCallback = null;
        }
        #endregion
        public int ClientLocalEndPointPort => ((IPEndPoint)sock.LocalEndPoint).Port;
        public virtual async Task SecureConnectAsync(string host, int port, X509CertificateCollection certs, RemoteCertificateValidationCallback remoteCertValidator, LocalCertificateSelectionCallback localCertSelector, bool checkCertRevocation = false)
        {
            var adds = await Dns.GetHostAddressesAsync(host);
            var ep = TcpClientAdapterHelper.GetMatchingHost(adds, sock.AddressFamily);
            if (ep == default(IPAddress))
            {
                throw new ArgumentException("No ip address could be resolved for " + host);
            }

#if CORECLR
            await sock.ConnectAsync(ep, port);
#else
            sock.Connect(ep, port);
#endif
            baseStream = new NetworkStream(sock);
            baseSSLStream = new SslStream(baseStream, true, remoteCertValidator, localCertSelector);

            await baseSSLStream.AuthenticateAsClientAsync(host, certs, Convert(System.Net.ServicePointManager.SecurityProtocol), checkCertRevocation);

            asyncCallback = new AsyncCallback(SecureRead);

            var peek = ringBuffer.Peek();
                baseSSLStream.BeginRead(
                    peek.Array,
                    peek.Offset,
                    peek.Count,
                    asyncCallback,
                    null
                );
        }
        public virtual async Task ConnectAsync(string host, int port)
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
            if (baseSSLStream != null)
            {
                lock (_syncLock)
                {
                    baseSSLStream.Write(data.Array, data.Offset, data.Count);
                }
            }
            else
            {
                baseStream.Write(data.Array, data.Offset, data.Count);
            }
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
            catch (System.Net.Sockets.SocketException) {
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
        private SslProtocols Convert(SecurityProtocolType securityProtocol)
        {
            SslProtocols protocols = SslProtocols.Default;

            if ((securityProtocol & SecurityProtocolType.Ssl3) == SecurityProtocolType.Ssl3) protocols |= SslProtocols.Ssl3;
            if ((securityProtocol & SecurityProtocolType.Tls) == SecurityProtocolType.Tls) protocols |= SslProtocols.Tls;
            if ((securityProtocol & SecurityProtocolType.Tls11) == SecurityProtocolType.Tls11) protocols |= SslProtocols.Tls11;
            if ((securityProtocol & SecurityProtocolType.Tls12) == SecurityProtocolType.Tls12) protocols |= SslProtocols.Tls12;

            if (protocols == SslProtocols.None) protocols = SslProtocols.Default;

            return protocols;
        }
    }
    public class HyperTcpClientSettings
    {
        public HyperTcpClientSettings(AddressFamily addressFamily, int requestedHeartbeat)
        {
            this.AddressFamily = addressFamily;
            this.RequestedHeartbeat = requestedHeartbeat;
        }
        public AddressFamily AddressFamily { get;private set; }
        public int RequestedHeartbeat { get; private set; }
    }
}
#endif