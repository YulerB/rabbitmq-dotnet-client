﻿#if !NETFX_CORE
using System;
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
        private readonly object _syncLock = new object();
        private AsyncCallback asyncCallback;

        private StreamRingBuffer ringBuffer;
        SocketAsyncEventArgs sEvent;
        public HyperTcpClientAdapter(HyperTcpClientSettings settings)
        {
            sock = new Socket(settings.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            sock.ReceiveTimeout = Math.Max(sock.ReceiveTimeout, settings.RequestedHeartbeat * 1000);
            sock.SendTimeout = Math.Max(sock.SendTimeout, settings.RequestedHeartbeat * 1000);
            ringBuffer = new StreamRingBuffer(sock.ReceiveBufferSize * 20);
            sEvent = new SocketAsyncEventArgs { AcceptSocket = sock };
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
                sEvent.Dispose();
            }
            ringBuffer = null;
            Receive = null;
            baseStream = null;
            baseSSLStream = null;
            sock = null;
            sEvent = null;
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

            var peek = ringBuffer.Peek();

            sEvent.SetBuffer(
                peek.Array,
                peek.Offset,
                peek.Count
            );
            sEvent.Completed += SEvent_Completed;
            
            ProcessReceive(sEvent);
        }

        private void SEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            do
            {
                if (e.BytesTransferred > 0)
                    this.Receive?.Invoke(this, ringBuffer.Take(e.BytesTransferred));

                if (e.SocketError != SocketError.Success)
                {
                    Close();
                    break;
                }

                var peek = ringBuffer.Peek();
                e.SetBuffer(peek.Offset, peek.Count);
            } while (!sock.ReceiveAsync(e));

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