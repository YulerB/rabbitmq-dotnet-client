// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2016 Pivotal Software, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

#if !NETFX_CORE
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Impl
{
    static class TaskExtensions
    {
        public static readonly Task CompletedTask = Task.FromResult(0);

        public static async Task TimeoutAfter(this Task task, int millisecondsTimeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)).ConfigureAwait(false))
                await task;
            else
            {
                var supressErrorTask = task.ContinueWith(t => t.Exception.Handle(e => true), TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException();
            }
        }
    }

    public class SocketFrameHandler : IFrameHandler
    {
        // Socket poll timeout in ms. If the socket does not
        // become writeable in this amount of time, we throw
        // an exception.
        private int m_writeableStateTimeout = 30000;
        private NetworkBinaryReader m_reader;
        private ITcpClient m_socket;
        private NetworkBinaryWriter m_writer;
        private readonly object _semaphore = new object();
        private readonly object _sslStreamLock = new object();
        private bool _closed;
        private readonly bool _ssl;
        public SocketFrameHandler(AmqpTcpEndpoint endpoint,
            Func<AddressFamily, int, ITcpClient> socketFactory,
            int connectionTimeout, int readTimeout, int writeTimeout)
        {
            Endpoint = endpoint;

            if (ShouldTryIPv6(endpoint))
            {
                try
                {
                    m_socket = ConnectUsingIPv6(endpoint, socketFactory, connectionTimeout);
                }
                catch (ConnectFailureException)
                {
                    m_socket = ConnectUsingIPv4(endpoint, socketFactory, connectionTimeout);
                }
            }
            if (m_socket == null && endpoint.AddressFamily != AddressFamily.InterNetworkV6)
            {
                m_socket = ConnectUsingIPv4(endpoint, socketFactory, connectionTimeout);
            }

            Stream netstream = m_socket.GetStream();
            netstream.ReadTimeout = readTimeout;
            netstream.WriteTimeout = writeTimeout;

            if (endpoint.Ssl.Enabled)
            {
                try
                {
                    netstream = SslHelper.TcpUpgrade(netstream, endpoint.Ssl);
                    _ssl = true;
                }
                catch (Exception)
                {
                    Close();
                    throw;
                }
            }
            m_reader = new NetworkBinaryReader(new BufferedStream(netstream, m_socket.ClientReceiveBufferSize));
            m_writer = new NetworkBinaryWriter(netstream);

            m_writeableStateTimeout = writeTimeout;
        }
        public AmqpTcpEndpoint Endpoint { get; set; }

        public EndPoint LocalEndPoint
        {
            get { return m_socket.ClientLocalEndPoint; }
        }

        public int LocalPort
        {
            get { return ((IPEndPoint)LocalEndPoint).Port; }
        }

        public EndPoint RemoteEndPoint
        {
            get { return m_socket.ClientRemoteEndPoint; }
        }

        public int RemotePort
        {
            get { return ((IPEndPoint)LocalEndPoint).Port; }
        }

        public int ReadTimeout
        {
            set
            {
                try
                {
                    if (m_socket.Connected)
                    {
                        m_socket.ReceiveTimeout = value;
                    }
                }
                catch (SocketException)
                {
                    // means that the socket is already closed
                }
            }
        }

        public int WriteTimeout
        {
            set
            {
                m_writeableStateTimeout = value;
                m_socket.ClientSendTimeout = value;
            }
        }

        public void Close()
        {
            lock (_semaphore)
            {
                if (!_closed)
                {
                    try
                    {
                        m_socket.Close();
                    }
                    catch (Exception)
                    {
                        // ignore, we are closing anyway
                    }
                    finally
                    {
                        _closed = true;
                    }
                }
            }
        }

        public InboundFrame ReadFrame()
        {
            return RabbitMQ.Client.Impl.InboundFrame.ReadFrom(m_reader);
        }

        private static readonly byte[] amqp = Encoding.ASCII.GetBytes("AMQP");
        public void SendHeader()
        {
            byte[] header = Endpoint.Protocol.Revision != 0 ?
                    new byte[8] { amqp[0], amqp[1], amqp[2], amqp[3], (byte)0, (byte)Endpoint.Protocol.MajorVersion, (byte)Endpoint.Protocol.MinorVersion, (byte)Endpoint.Protocol.Revision }
                    :
                    new byte[8] { amqp[0], amqp[1], amqp[2], amqp[3], (byte)1, (byte)1, (byte)Endpoint.Protocol.MajorVersion, (byte)Endpoint.Protocol.MinorVersion };

            if (_ssl)
            {
                lock (_sslStreamLock)
                {
                    m_writer.Write(header);
                }
            }
            else
            {
                m_writer.Write(header);
            }
        }

        public void WriteFrame(OutboundFrame frame)
        {
            using (var ms = MemoryStreamPool.GetObject())
            {
                var nbw = new NetworkBinaryWriter(ms.Instance);
                frame.WriteTo(nbw);
                m_socket.ClientPollCanWrite(m_writeableStateTimeout);
                Write(ms.Instance.GetBufferSegment());
            }
        }

        public void WriteFrameSet(IList<OutboundFrame> frames)
        {
            using (var ms = MemoryStreamPool.GetObject())
            {
                var nbw = new NetworkBinaryWriter(ms.Instance);
                foreach (var f in frames) f.WriteTo(nbw);
                m_socket.ClientPollCanWrite(m_writeableStateTimeout);
                Write(ms.Instance.GetBufferSegment());
            }
        }

        private void Write(ArraySegment<byte> bufferSegment)
        {
            if (_ssl)
            {
                lock (_sslStreamLock)
                {
                    m_writer.Write(bufferSegment.Array, bufferSegment.Offset, bufferSegment.Count);
                }
            }
            else
            {
                m_writer.Write(bufferSegment.Array, bufferSegment.Offset, bufferSegment.Count);
            }
        }

        private bool ShouldTryIPv6(AmqpTcpEndpoint endpoint)
        {
            return (Socket.OSSupportsIPv6 && endpoint.AddressFamily != AddressFamily.InterNetwork);
        }

        private ITcpClient ConnectUsingIPv6(AmqpTcpEndpoint endpoint,
                                            Func<AddressFamily, int, ITcpClient> socketFactory,
                                            int timeout)
        {
            return ConnectUsingAddressFamily(endpoint, socketFactory, timeout, AddressFamily.InterNetworkV6);
        }

        private ITcpClient ConnectUsingIPv4(AmqpTcpEndpoint endpoint,
                                            Func<AddressFamily, int, ITcpClient> socketFactory,
                                            int timeout)
        {
            return ConnectUsingAddressFamily(endpoint, socketFactory, timeout, AddressFamily.InterNetwork);
        }

        private ITcpClient ConnectUsingAddressFamily(AmqpTcpEndpoint endpoint,
                                                    Func<AddressFamily, int, ITcpClient> socketFactory,
                                                    int timeout, AddressFamily family)
        {
            ITcpClient socket = socketFactory(family, timeout);
            try
            {
                ConnectOrFail(socket, endpoint, timeout);
                return socket;
            }
            catch (ConnectFailureException)
            {
                socket.Dispose();
                throw;
            }
        }

        private void ConnectOrFail(ITcpClient socket, AmqpTcpEndpoint endpoint, int timeout)
        {
            try
            {
                socket.ConnectAsync(endpoint.HostName, endpoint.Port)
                      .TimeoutAfter(timeout)
                      .ConfigureAwait(false)
                      // this ensures exceptions aren't wrapped in an AggregateException
                      .GetAwaiter()
                      .GetResult();
            }
            catch (ArgumentException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (SocketException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (NotSupportedException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (TimeoutException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_reader?.Dispose();
                m_writer?.Dispose();
                m_socket?.Dispose();
            }
            m_reader = null;
            m_writer = null;
            m_socket = null;
        }
        

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }


    public class HyperSocketFrameHandler : IFrameHandler, IDisposable
    {
        private readonly ArraySegmentStream m_stream;
        private IHyperTcpClient m_socket;
        private readonly object _semaphore = new object();
        private bool _closed;

        public HyperSocketFrameHandler(AmqpTcpEndpoint endpoint, Func<AddressFamily, int, IHyperTcpClient> socketFactory, int connectionTimeout, int readTimeout, int writeTimeout)
        {
            Endpoint = endpoint;

            if (ShouldTryIPv6(endpoint))
            {
                try
                {
                    m_socket = ConnectUsingIPv6(endpoint, socketFactory, connectionTimeout);
                }
                catch (ConnectFailureException)
                {
                    m_socket = ConnectUsingIPv4(endpoint, socketFactory, connectionTimeout);
                }
            }
            else
            {
                m_socket = ConnectUsingIPv4(endpoint, socketFactory, connectionTimeout);
            }
            
            m_socket.Receive += M_socket_Receive;
            m_socket.Closed += M_socket_Closed;

            m_stream = new ArraySegmentStream();
            m_stream.BufferUsed += M_stream_BufferUsed;
        }

        private void M_stream_BufferUsed(object sender, EventArgs e)
        {
            m_socket.BufferUsed();
        }

        private void M_socket_Closed(object sender, EventArgs e)
        {
            m_stream.NotifyClosed();
        }

        private void M_socket_Receive(object sender, ReadOnlyMemory<byte> e)
        {
            m_stream.Write(e);
        }

        public AmqpTcpEndpoint Endpoint { get; set; }

        public EndPoint LocalEndPoint
        {
            get { return m_socket.ClientLocalEndPoint; }
        }

        public int LocalPort
        {
            get { return ((IPEndPoint)LocalEndPoint).Port; }
        }

        public EndPoint RemoteEndPoint
        {
            get { return m_socket.ClientRemoteEndPoint; }
        }

        public int RemotePort
        {
            get { return ((IPEndPoint)LocalEndPoint).Port; }
        }

        
        public void Close()
        {
            if (!_closed)
            {
                lock (_semaphore)
                {
                    if (!_closed)
                    {
                        try
                        {
                            m_socket.Close();
                        }
                        catch (Exception)
                        {
                            // ignore, we are closing anyway
                        }
                        finally
                        {
                            _closed = true;
                        }
                    }
                }
            }
        }

        public InboundFrame ReadFrame()
        {
            return RabbitMQ.Client.Impl.InboundFrame.ReadFrom(new NetworkArraySegmentsReader(m_stream));
        }

        private static readonly byte[] amqp = Encoding.ASCII.GetBytes("AMQP");
        public void SendHeader()
        {
            byte[] header = Endpoint.Protocol.Revision != 0 ?
                    new byte[8] { amqp[0], amqp[1], amqp[2], amqp[3], (byte)0, (byte)Endpoint.Protocol.MajorVersion, (byte)Endpoint.Protocol.MinorVersion, (byte)Endpoint.Protocol.Revision }
                    :
                    new byte[8] { amqp[0], amqp[1], amqp[2], amqp[3], (byte)1, (byte)1, (byte)Endpoint.Protocol.MajorVersion, (byte)Endpoint.Protocol.MinorVersion };

            m_socket.Write(new ArraySegment<byte>(header));
        }

        public void WriteFrame(OutboundFrame frame)
        {
            using (var ms = MemoryStreamPool.GetObject())
            {
                var nbw = new NetworkBinaryWriter(ms.Instance);
                frame.WriteTo(nbw);
                m_socket.Write(ms.Instance.GetBufferSegment());
            }
        }

        public void WriteFrameSet(IList<OutboundFrame> frames)
        {
            using (var ms = MemoryStreamPool.GetObject())
            {
                var nbw = new NetworkBinaryWriter(ms.Instance);
                foreach (var f in frames) f.WriteTo(nbw);
                m_socket.Write(ms.Instance.GetBufferSegment());
            }
        }

        private bool ShouldTryIPv6(AmqpTcpEndpoint endpoint)
        {
            return (Socket.OSSupportsIPv6 && endpoint.AddressFamily != AddressFamily.InterNetwork);
        }

        private IHyperTcpClient ConnectUsingIPv6(AmqpTcpEndpoint endpoint, Func<AddressFamily, int, IHyperTcpClient> socketFactory, int timeout)
        {
            return ConnectUsingAddressFamily(endpoint, socketFactory, timeout, AddressFamily.InterNetworkV6);
        }

        private IHyperTcpClient ConnectUsingIPv4(AmqpTcpEndpoint endpoint, Func<AddressFamily, int, IHyperTcpClient> socketFactory, int timeout)
        {
            return ConnectUsingAddressFamily(endpoint, socketFactory, timeout, AddressFamily.InterNetwork);
        }

        private IHyperTcpClient ConnectUsingAddressFamily(AmqpTcpEndpoint endpoint, Func<AddressFamily, int, IHyperTcpClient> socketFactory, int timeout, AddressFamily family)
        {
            IHyperTcpClient socket = socketFactory(family, timeout );
            try
            {
                if (endpoint.Ssl.Enabled) SecureConnectOrFail(socket, endpoint, timeout, false/*endpoint.Ssl.checkCertRevocation*/); else ConnectOrFail(socket, endpoint, timeout);
                return socket;
            }
            catch (ConnectFailureException)
            {
                socket.Dispose();
                throw;
            }
        }

        private void ConnectOrFail(IHyperTcpClient socket, AmqpTcpEndpoint endpoint, int timeout)
        {
            try
            {
                socket.ConnectAsync(endpoint.HostName, endpoint.Port)
                      .TimeoutAfter(timeout)
                      .ConfigureAwait(false)
                      // this ensures exceptions aren't wrapped in an AggregateException
                      .GetAwaiter()
                      .GetResult();
            }
            catch (ArgumentException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (SocketException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (NotSupportedException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (TimeoutException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
        }
        private void SecureConnectOrFail(IHyperTcpClient socket, AmqpTcpEndpoint endpoint, int timeout, bool checkCertRevocation)
        {
            try
            {
                socket.SecureConnectAsync(
                    endpoint.HostName, 
                    endpoint.Port, 
                    endpoint.Ssl.Certs, 
                    endpoint.Ssl.CertificateValidationCallback, 
                    endpoint.Ssl.CertificateSelectionCallback,
                    checkCertRevocation
                ).TimeoutAfter(timeout)
                 .ConfigureAwait(false) // this ensures exceptions aren't wrapped in an AggregateException
                 .GetAwaiter()
                 .GetResult();
            }
            catch (ArgumentException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (SocketException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (NotSupportedException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (TimeoutException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_socket?.Dispose();
            }
            m_socket = null;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
    public class ArraySegmentStream : Stream
    {
        private BlockingCollection<ReadOnlyMemory<byte>> data = new BlockingCollection<ReadOnlyMemory<byte>>();
        public event EventHandler BufferUsed;
        public ArraySegmentStream(byte[] buffer) {
            data.Add(new ArraySegment<byte>(buffer, 0, buffer.Length));
        }
        public ArraySegmentStream(ArraySegment<byte> buffer)
        {
            data.Add(buffer);
        }
        public ArraySegmentStream(IEnumerable<ArraySegment<byte>> buffers)
        {
            foreach(var buffer in buffers)
                data.Add(buffer);
        }
        public ArraySegmentStream() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cts.Dispose();
                data.Dispose();
            }
            base.Dispose(disposing);
            data = null;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position { get => 0; set => throw new NotImplementedException(); }

        public override void Flush()
        {

        }

        private ReadOnlyMemory<byte> top = new ReadOnlyMemory<byte>();
        private readonly ArraySegment<byte> empty = new ArraySegment<byte>();
        public ReadOnlyMemory<byte>[] Read(int count)
        {
            List<ReadOnlyMemory<byte>> result = new List<ReadOnlyMemory<byte>>();

            while (count > 0)
            {
                if (top.Length == 0)
                {
                    try
                    {
                        top = data.Take(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new EndOfStreamException();
                    }
                }
                if (top.Length > count)
                {
                    var read = top.Slice(0, count);
                    top = top.Slice(count, top.Length - count);
                    result.Add(read);
                    return result.ToArray();
                }
                else
                {
                    var read = top.Slice(0, top.Length);
                    count -= top.Length;
                    top = empty;
                    BufferUsed?.Invoke(this, EventArgs.Empty);
                    result.Add(read);
                }
            }
            return result.ToArray();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            data.Add(new ArraySegment<byte>(buffer, offset, count));
        }
        public void Write(ReadOnlyMemory<byte> buffer)
        {
            data.Add(buffer);
        }

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        internal void NotifyClosed()
        {
            cts.Cancel();
        }
    }
}
#endif
