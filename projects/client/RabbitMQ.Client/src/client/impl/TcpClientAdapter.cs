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
    public class HyperTcpClientAdapter : IHyperTcpClient
    {
        public event EventHandler Closed;
        private Socket sock;
        private NetworkStream baseStream;
        private SslStream baseSSLStream;
        public event EventHandler<ReadOnlyMemory<byte>> Receive;
        private byte[] bigBuffer = null;
        private ReadOnlyMemory<byte> Memory=null;
        private const int BufferSegments = 20;
        private int bigBufferPosition = 0;
        private SemaphoreSlim buffersInPlay = new SemaphoreSlim(BufferSegments);
        private readonly object _syncLock = new object();

        public HyperTcpClientAdapter(Socket socket, int RequestedHeartbeat)
        {
            socket.ReceiveTimeout = Math.Max(socket.ReceiveTimeout, RequestedHeartbeat * 1000);
            socket.SendTimeout = Math.Max(socket.SendTimeout, RequestedHeartbeat * 1000);
            this.sock = socket ?? throw new InvalidOperationException("socket must not be null");
            bigBuffer= new byte[sock.ReceiveBufferSize * BufferSegments];
            Memory = new ReadOnlyMemory<byte>(bigBuffer);
        }

        public virtual void BufferUsed()
        {
            buffersInPlay.Release();
        }
        public virtual void Close()
        {
            baseStream?.Close();
            baseSSLStream?.Close();
            sock?.Close();
            Closed?.Invoke(this, EventArgs.Empty);
        }
        private bool disposed;
        [Obsolete("Override Dispose(bool) instead.")]
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
            Memory = null;
            bigBuffer = null;
            Receive = null;
            baseStream = null;
            baseSSLStream = null;
            sock = null;
        }
        public int ClientLocalEndPointPort => ((IPEndPoint)sock.LocalEndPoint).Port;
        public virtual async Task SecureConnectAsync(string host, int port, X509CertificateCollection certs, RemoteCertificateValidationCallback remoteCertValidator, LocalCertificateSelectionCallback localCertSelector, bool checkCertRevocation = false)
        {
            AssertSocket();
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

            buffersInPlay.Wait();
            baseSSLStream.BeginRead(bigBuffer, 0, sock.ReceiveBufferSize, new AsyncCallback(SecureRead), null);
        }
        public virtual async Task ConnectAsync(string host, int port)
        {
            AssertSocket();
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

            buffersInPlay.Wait();
            baseStream.BeginRead(bigBuffer, 0, sock.ReceiveBufferSize, new AsyncCallback(Read), null);
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

        private void AssertSocket()
        {
            if (sock == null)
            {
                throw new InvalidOperationException("Cannot perform operation as socket is null");
            }
        }
        private void Read(IAsyncResult result)
        {
            try
            {
                if (!disposed)
                {
                    this.Receive(this, Memory.Slice(bigBufferPosition * sock.ReceiveBufferSize, baseStream.EndRead(result)));
                    bigBufferPosition++;
                    if (bigBufferPosition == BufferSegments) bigBufferPosition = 0;
                    buffersInPlay
                        .WaitAsync()
                        .ContinueWith(async (t) =>
                            {
                                await t;
                                baseStream.BeginRead(
                                  bigBuffer,
                                  bigBufferPosition * sock.ReceiveBufferSize,
                                  sock.ReceiveBufferSize,
                                  new AsyncCallback(Read),
                                  null
                              );
                            }
                        ).ConfigureAwait(false);
                }
            }
            catch (System.Net.Sockets.SocketException) {
                Close();
            }
            catch (System.ObjectDisposedException) {
                // Nothing to do here.
            }
            catch (System.IO.FileNotFoundException) {
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
                    this.Receive(this, Memory.Slice(bigBufferPosition * sock.ReceiveBufferSize, baseSSLStream.EndRead(result)));
                    bigBufferPosition++;
                    if (bigBufferPosition == BufferSegments) bigBufferPosition = 0;
                    buffersInPlay
                        .WaitAsync()
                        .ContinueWith(async (t) =>
                        {
                            await t;
                            baseSSLStream.BeginRead(
                              bigBuffer,
                              bigBufferPosition * sock.ReceiveBufferSize,
                              sock.ReceiveBufferSize,
                              new AsyncCallback(SecureRead),
                              null
                          );
                        }
                        ).ConfigureAwait(false);
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
}
#endif