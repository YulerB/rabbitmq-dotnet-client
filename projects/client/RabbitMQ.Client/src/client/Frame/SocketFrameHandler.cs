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
using System.Buffers;

namespace RabbitMQ.Client.Impl
{
    public class HyperSocketFrameHandler : IFrameHandler, IDisposable
    {
        private readonly HyperSocketFrameSettings settings;
        private ArraySegmentSequence m_stream = new ArraySegmentSequence();
        private IHyperTcpClient m_socket;
        private readonly object _semaphore = new object();
        private bool _closed;

        public HyperSocketFrameHandler(HyperSocketFrameSettings settings)
        {
            this.settings = settings;

            if(Socket.OSSupportsIPv6 && settings.Endpoint.AddressFamily != AddressFamily.InterNetwork)
            {
                try
                {
                    m_socket = ConnectUsingAddressFamily(new HyperTcpClientSettings (settings.Endpoint , AddressFamily.InterNetworkV6, settings.RequestedHeartbeat));
                }
                catch (ConnectFailureException)
                {
                    m_socket = ConnectUsingAddressFamily(new HyperTcpClientSettings (settings.Endpoint, AddressFamily.InterNetwork, settings.RequestedHeartbeat ));
                }
            }
            else
            {
                m_socket = ConnectUsingAddressFamily(new HyperTcpClientSettings(settings.Endpoint, AddressFamily.InterNetwork, settings.RequestedHeartbeat ));
            }

            m_socket.Closed += M_socket_Closed;
            m_stream.BufferUsed += M_stream_BufferUsed;
            m_socket.Receive += M_socket_Receive;
        }

        private void M_stream_BufferUsed(object sender, BufferUsedEventArgs e)
        {
            m_socket.BufferUsed(e.Size);
        }

        private void M_socket_Closed(object sender, EventArgs e)
        {
            m_stream.NotifyClosed();
        }

        private void M_socket_Receive(object sender, ArraySegment<byte> e)
        {
            m_stream.Write(e);
        }

        public AmqpTcpEndpoint Endpoint { get { return settings.Endpoint; } }

        public int LocalPort
        {
            get { return m_socket.ClientLocalEndPointPort; }
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
            return FrameReader.ReadFrom(m_stream);
        }


        private static readonly byte[] amqp = Encoding.ASCII.GetBytes("AMQP");
        public void SendHeader()
        {
            var prot = settings.Endpoint.Protocol;
            byte[] header = prot.Revision != 0 ?
                    new byte[8] { amqp[0], amqp[1], amqp[2], amqp[3], (byte)0, (byte)prot.MajorVersion, (byte)prot.MinorVersion, (byte)prot.Revision }
                    :
                    new byte[8] { amqp[0], amqp[1], amqp[2], amqp[3], (byte)1, (byte)1, (byte)prot.MajorVersion, (byte)prot.MinorVersion };

            m_socket.Write(new ArraySegment<byte>(header));
        }

        public void WriteFrame(OutboundFrame frame)
        {
            var size = frame.EstimatedSize();
            using (var byteBuffer = MemoryPool<byte>.Shared.Rent(size))
            {
                Span<byte> buffer = byteBuffer.Memory.Span;
                frame.WriteTo(buffer, out int written);
                m_socket.Write(new ArraySegment<byte>(byteBuffer.Memory.ToArray(), 0, written));
            }
        }
        public void WriteFrameSet(IList<OutboundFrame> frames)
        {
            var size = 0;
            foreach (var frame in frames) size += frame.EstimatedSize();
            using (var byteBuffer = MemoryPool<byte>.Shared.Rent(size))
            {
                Span<byte> buffer = byteBuffer.Memory.Span;
                var total = 0;
                foreach (var f in frames)
                {
                    f.WriteTo(buffer.Slice(total), out int written);
                    total += written;
                }
                m_socket.Write(new ArraySegment<byte>(byteBuffer.Memory.ToArray(), 0, total));
            }
        }

        private IHyperTcpClient ConnectUsingAddressFamily(HyperTcpClientSettings settings)
        {
            IHyperTcpClient socket = settings.EndPoint.Ssl.Enabled ? 
                new HyperTcpSecureClientAdapter(settings) as IHyperTcpClient : 
                new HyperTcpClientAdapter(settings) as IHyperTcpClient;

            try
            {
                ConnectOrFail(socket);
                return socket;
            }
            catch (ConnectFailureException)
            {
                socket.Dispose();
                throw;
            }
        }

        private void ConnectOrFail(IHyperTcpClient socket)
        {
            try
            {
                socket.ConnectAsync()
                      .TimeoutAfter(this.settings.ConnectionTimeout)
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
                m_socket?.Dispose();
                m_stream.Dispose();
            }
            m_socket = null;
            m_stream = null;
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
#endif
