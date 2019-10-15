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
        public event EventHandler<Memory<byte>> Receive;
        private readonly HyperTcpClientSettings settings;
        private Socket sock;
        private StreamRingBuffer ringBuffer;
        private SocketAsyncEventArgs sEvent;

        public HyperTcpClientAdapter(HyperTcpClientSettings settings)
        {
            this.settings = settings;
            sock = new Socket(settings.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            sock.ReceiveTimeout = Math.Max(sock.ReceiveTimeout, settings.RequestedHeartbeat * 1000);
            sock.SendTimeout = Math.Max(sock.SendTimeout, settings.RequestedHeartbeat * 1000);
            ringBuffer = new StreamRingBuffer(sock.ReceiveBufferSize * 4);
            sEvent = new SocketAsyncEventArgs { AcceptSocket = sock };
            sEvent.Completed += SEvent_Completed;
        }

        public virtual void BufferUsed(int size)
        {
            ringBuffer.Release(size);
        }
        public virtual void Close()
        {
            //System.Diagnostics.Debug.WriteLine("HyperTcpClientAdapter.Close");
            //Console.WriteLine("HyperTcpClientAdapter.Close");
            sock?.Close();
        }
        private void InternalClose(string reason)
        {
            //System.Diagnostics.Debug.WriteLine("HyperTcpClientAdapter.InternalClose - " + reason);
            //Console.WriteLine("HyperTcpClientAdapter.InternalClose - " + reason);

            sock?.Close();
            Closed?.Invoke(this, EventArgs.Empty);
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


            ringBuffer.InitialFill(sEvent.SetBuffer);

            await Task.Run(async () =>
            {
                SEvent_Completed(this, sEvent);
                await Task.FromResult(0);
            }).ConfigureAwait(false);
        }
        private void SEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                do
                {
                    if (e.BytesTransferred > 0) this.Receive?.Invoke(this, ringBuffer.Take(e.BytesTransferred));
                    var peeked = ringBuffer.Peek();
                    e.SetBuffer(peeked.Array, peeked.Offset, peeked.Count);
                } while (sock.Connected && e.SocketError == SocketError.Success && !sock.ReceiveAsync(e));

                if (!sock.Connected || e.SocketError != SocketError.Success)
                {
                    InternalClose(e.SocketError.ToString());
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore, we need to stop reading when the socket is disposed.
                InternalClose("SEvent_Completed.ObjectDisposedException");
            }
            catch (SocketException)
            {
                // Ignore, we need to stop reading when the socket is disposed.
                InternalClose("SEvent_Completed.SocketException");
            }
            catch (NullReferenceException)
            {
                // Ignore, we need to stop reading when the any related disposable is set to null.
                //InternalClose("SEvent_Completed.NullReferenceException");
            }
        }
        public void Write(ArraySegment<byte> data)
        {
            try
            {
                if(sock.Connected) sock.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
            }
            catch (System.Net.Sockets.SocketException se)
            {
                InternalClose("Write.SocketException - " + se.ToString());
            }
            catch (System.ObjectDisposedException ode)
            {
                InternalClose("Write.ObjectDisposedException - " + ode.ToString());
            }
            catch (NullReferenceException)
            {
                // Ignore, we need to stop reading when the any related disposable is set to null.
                InternalClose("Write.NullReferenceException");
            }
        }
        public void Write(IList<ArraySegment<byte>> data)
        {
            try
            {
                if (sock.Connected) sock.Send(data);
            }
            catch (System.Net.Sockets.SocketException se)
            {
                InternalClose("Write.SocketException - " + se.ToString());
            }
            catch (System.ObjectDisposedException ode)
            {
                InternalClose("Write.ObjectDisposedException - " + ode.ToString());
            }
            catch (NullReferenceException)
            {
                // Ignore, we need to stop reading when the any related disposable is set to null.
                InternalClose("Write.NullReferenceException");
            }
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
                sEvent.Completed -= SEvent_Completed;
                sEvent.Dispose();
            }
            Receive = null;
            sock = null;
            sEvent = null;
            ringBuffer = null;
            Closed = null;
        }
        #endregion
    }

}
#endif