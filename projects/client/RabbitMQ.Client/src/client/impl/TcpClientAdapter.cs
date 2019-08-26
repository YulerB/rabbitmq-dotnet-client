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

namespace RabbitMQ.Client
{
    /// <summary>
    /// Simple wrapper around TcpClient.
    /// </summary>
    public class TcpClientAdapter : ITcpClient
    {
        private readonly Socket sock;

        public TcpClientAdapter(Socket socket)
        {
            if (socket == null)
                throw new InvalidOperationException("socket must not be null");

            this.sock = socket;
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
        }

        public virtual void Close()
        {
            if (sock != null)
            {
                sock.Dispose();
            }
        }

        [Obsolete("Override Dispose(bool) instead.")]
        public virtual void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                Close();
            }

            // dispose unmanaged resources
        }

        public virtual NetworkStream GetStream()
        {
            AssertSocket();
            return new NetworkStream(sock);
        }

        public virtual bool Connected
        {
            get
            {
                if(sock == null) return false;
                return sock.Connected;
            }
        }

        public virtual int ReceiveTimeout
        {
            get
            {
                AssertSocket();
                return sock.ReceiveTimeout;
            }
            set
            {
                AssertSocket();
                sock.ReceiveTimeout = value;
            }
        }

        public EndPoint ClientLocalEndPoint => sock.LocalEndPoint;

        public EndPoint ClientRemoteEndPoint => sock.RemoteEndPoint;

        public int ClientReceiveBufferSize => sock.ReceiveBufferSize;

        public int ClientSendTimeout { set { sock.SendTimeout = value; } }

        private void AssertSocket()
        {
            if(sock == null)
            {
                throw new InvalidOperationException("Cannot perform operation as socket is null");
            }
        }

        public bool ClientPollCanWrite(int m_writeableStateTimeout)
        {
            return sock.Poll(m_writeableStateTimeout, SelectMode.SelectWrite);
        }
    }
    public class HyperTcpClientAdapter : IHyperTcpClient
    {
        private Socket sock;
        private NetworkStream baseStream;
        private SslStream baseSSLStream;
        public event EventHandler<ArraySegment<byte>> Receive;

        public HyperTcpClientAdapter(Socket socket)
        {
            if (socket == null)
                throw new InvalidOperationException("socket must not be null");

            this.sock = socket;
        }


        public virtual void Close()
        {
            baseStream?.Close();
            baseSSLStream?.Close();
            sock?.Close();
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
            Receive = null;
            baseStream = null;
            baseSSLStream = null;
            sock = null;
        }

        public virtual bool Connected
        {
            get
            {
                if (sock == null) return false;
                return sock.Connected;
            }
        }

        public virtual int ReceiveTimeout
        {
            get
            {
                AssertSocket();
                return sock.ReceiveTimeout;
            }
            set
            {
                AssertSocket();
                sock.ReceiveTimeout = value;
            }
        }

        public EndPoint ClientLocalEndPoint => sock.LocalEndPoint;

        public EndPoint ClientRemoteEndPoint => sock.RemoteEndPoint;

        public int ClientReceiveBufferSize => sock.ReceiveBufferSize;

        public int ClientSendTimeout { set { sock.SendTimeout = value; } }

        private void AssertSocket()
        {
            if (sock == null)
            {
                throw new InvalidOperationException("Cannot perform operation as socket is null");
            }
        }

        public bool ClientPollCanWrite(int m_writeableStateTimeout)
        {
            return true;// sock.Poll(m_writeableStateTimeout, SelectMode.SelectWrite);
        }

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

            var buffer = new ArraySegment<byte>(new byte[sock.ReceiveBufferSize]);
            baseSSLStream.BeginRead(buffer.Array, buffer.Offset, buffer.Count, new AsyncCallback(Read), buffer);
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

            var buffer = new byte[sock.ReceiveBufferSize];
            baseStream.BeginRead(buffer, 0,buffer.Length, new AsyncCallback(Read), buffer);
        }

        private void Read(IAsyncResult result)
        {
            try
            {
                if (!disposed)
                {
                    {
                        int read = baseStream.EndRead(result);
                        byte[] buffer = result.AsyncState as byte[];
                        if (this.Receive != null) this.Receive(this, new ArraySegment<byte>(buffer, 0, read));
                    }

                    var buffer1 = new byte[sock.ReceiveBufferSize];
                    baseStream.BeginRead(buffer1, 0, buffer1.Length, new AsyncCallback(Read), buffer1);
                }
            }
            catch (System.Net.Sockets.SocketException) { }
            catch (System.ObjectDisposedException) { }
            catch (System.IO.FileNotFoundException) { }
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
                    {
                        int read = baseSSLStream.EndRead(result);
                        byte[] buffer = result.AsyncState as byte[];
                        if (this.Receive != null) this.Receive(this, new ArraySegment<byte>(buffer, 0, read));
                    }
                    var buffer1 = new byte[sock.ReceiveBufferSize];
                    baseSSLStream.BeginRead(buffer1, 0, buffer1.Length, new AsyncCallback(SecureRead), buffer1);
                }
            }
            catch (System.Net.Sockets.SocketException) { }
            catch (System.ObjectDisposedException) { }
            catch (System.IO.FileNotFoundException) { }
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

        public void Write(ArraySegment<byte> data)
        {
            if(baseSSLStream != null)
            {
                lock (this)
                {
                    baseSSLStream.WriteAsync(data.Array, data.Offset, data.Count).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            else
            {
                baseStream.WriteAsync(data.Array, data.Offset, data.Count).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
    }
}
#endif