#if !NETFX_CORE
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
        public event EventHandler<ReadOnlyMemory<byte>> Receive;
        private readonly HyperTcpClientSettings settings;
        private StreamRingBuffer ringBuffer;
        private SocketAsyncEventArgs sEvent;
        public HyperTcpClientAdapter(HyperTcpClientSettings settings)
        {
            this.settings = settings;
            sock = new Socket(settings.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            sock.ReceiveTimeout = Math.Max(sock.ReceiveTimeout, settings.RequestedHeartbeat * 1000);
            sock.SendTimeout = Math.Max(sock.SendTimeout, settings.RequestedHeartbeat * 1000);
            ringBuffer = new StreamRingBuffer(sock.ReceiveBufferSize * 20);
            sEvent = new SocketAsyncEventArgs { AcceptSocket = sock };
            sEvent.Completed += SEvent_Completed;
        }
        public virtual void BufferUsed(int size)
        {
            ringBuffer.Release(size);
        }
        public virtual void Close()
        {
            sock?.Close();
            Closed?.Invoke(this, EventArgs.Empty);
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
            ringBuffer = null;
            Receive = null;
            sock = null;
            sEvent = null;
            Closed = null;
        }
        #endregion
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

            var peek = ringBuffer.Peek();

            sEvent.SetBuffer(
                peek.Array,
                peek.Offset,
                peek.Count
            );

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
            sock.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
        }

        public void Write(IList<ArraySegment<byte>> data)
        {
            sock.Send(data);
        } 
    }

}
#endif