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

using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Authentication;

#if !NETFX_CORE

using System.Net.Security;

#endif

namespace RabbitMQ.Client
{
    /// <summary>Main entry point to the RabbitMQ .NET AMQP client
    ///API. Constructs <see cref="IConnection"/> instances.</summary>
    /// <remarks>
    /// <para>
    /// A simple example of connecting to a broker:
    /// </para>
    /// <example><code>
    ///     ConnectionFactory factory = new ConnectionFactory();
    ///     //
    ///     // The next six lines are optional:
    ///     factory.UserName = ConnectionFactory.DefaultUser;
    ///     factory.Password = ConnectionFactory.DefaultPass;
    ///     factory.VirtualHost = ConnectionFactory.DefaultVHost;
    ///     factory.HostName = hostName;
    ///     factory.Port     = AmqpTcpEndpoint.UseDefaultPort;
    ///     //
    ///     IConnection conn = factory.CreateConnection();
    ///     //
    ///     IModel ch = conn.CreateModel();
    ///     //
    ///     // ... use ch's IModel methods ...
    ///     //
    ///     ch.Close(Constants.ReplySuccess, "Closing the channel");
    ///     conn.Close(Constants.ReplySuccess, "Closing the connection");
    /// </code></example>
    /// <para>
    ///The same example, written more compactly with AMQP URIs:
    /// </para>
    /// <example><code>
    ///     ConnectionFactory factory = new ConnectionFactory();
    ///     factory.SetUri("amqp://localhost");
    ///     IConnection conn = factory.CreateConnection();
    ///     ...
    /// </code></example>
    /// <para>
    /// Please see also the API overview and tutorial in the User Guide.
    /// </para>
    /// <para>
    ///Note that the Uri property takes a string representation of an
    ///AMQP URI.  Omitted URI parts will take default values.  The
    ///host part of the URI cannot be omitted and URIs of the form
    ///"amqp://foo/" (note the trailling slash) also represent the
    ///default virtual host.  The latter issue means that virtual
    ///hosts with an empty name are not addressable. </para></remarks>
    public class ConnectionFactory :  IAsyncConnectionFactory
    {
        private readonly Func<AmqpTcpEndpoint, IFrameHandler> selector;
        public ConnectionFactory() 
        {
            HandshakeContinuationTimeout = TimeSpan.FromSeconds(10);
            ContinuationTimeout = TimeSpan.FromSeconds(20);
            ClientProperties = Connection.DefaultClientProperties();
            selector = CreateHyperFrameHandler;
        }

        /// <summary>
        /// Default value for the desired maximum channel number, with zero meaning unlimited (value: 0).
        /// </summary>
        /// <remarks>PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const ushort DefaultChannelMax = 0;

        /// <summary>
        /// Default value for connection attempt timeout, in milliseconds.
        /// </summary>
        public const int DefaultConnectionTimeout = 30 * 1000;

        /// <summary>
        /// Default value for the desired maximum frame size, with zero meaning unlimited (value: 0).
        /// </summary>
        /// <remarks>PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const uint DefaultFrameMax = 0U;

        /// <summary>
        /// Default value for desired heartbeat interval, in seconds, with zero meaning none (value: 60).
        /// </summary>
        /// <remarks>PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const ushort DefaultHeartbeat = 60; //

        /// <summary>
        /// Default password (value: "guest").
        /// </summary>
        /// <remarks>PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const string DefaultPass = "guest";

        /// <summary>
        /// Default user name (value: "guest").
        /// </summary>
        /// <remarks>PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const string DefaultUser = "guest";

        /// <summary>
        /// Default virtual host (value: "/").
        /// </summary>
        /// <remarks> PLEASE KEEP THIS MATCHING THE DOC ABOVE.</remarks>
        public const string DefaultVHost = "/";

        /// <summary>
        /// The default AMQP URI SSL protocols.
        /// </summary>
        public static SslProtocols DefaultAmqpUriSslProtocols { get; set; } =
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        /// <summary>
        /// The AMQP URI SSL protocols.
        /// </summary>
        public SslProtocols AmqpUriSslProtocols { get; set; } =
            DefaultAmqpUriSslProtocols;

        /// <summary>
        ///  Default SASL auth mechanisms to use.
        /// </summary>
        public static readonly IList<IAuthMechanismFactory> DefaultAuthMechanisms =
            new List<IAuthMechanismFactory>() { new PlainMechanismFactory() };

        /// <summary>
        ///  SASL auth mechanisms to use.
        /// </summary>
        public IList<IAuthMechanismFactory> AuthMechanisms { get; set; } = DefaultAuthMechanisms;

        /// <summary>
        /// Set to false to disable automatic connection recovery.
        /// Defaults to true.
        /// </summary>
        public bool AutomaticRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// Set to true will enable a asynchronous consumer dispatcher which is compatible with <see cref="IAsyncBasicConsumer"/>.
        /// Defaults to false.
        /// </summary>
        public bool DispatchConsumersAsync { get; set; } = false;

        /// <summary>The host to connect to.</summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// Amount of time client will wait for before re-trying  to recover connection.
        /// </summary>
        public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Amount of time protocol handshake operations are allowed to take before
        /// timing out.
        /// </summary>
        public TimeSpan HandshakeContinuationTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of time protocol  operations (e.g. <code>queue.declare</code>) are allowed to take before
        /// timing out.
        /// </summary>
        public TimeSpan ContinuationTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Factory function for creating the <see cref="IEndpointResolver"/>
        /// used to generate a list of endpoints for the ConnectionFactory
        /// to try in order.
        /// The default value creates an instance of the <see cref="DefaultEndpointResolver"/>
        /// using the list of endpoints passed in. The DefaultEndpointResolver shuffles the
        /// provided list each time it is requested.
        /// </summary>
        public Func<IEnumerable<AmqpTcpEndpoint>, IEndpointResolver> EndpointResolverFactory { get; set; } =
            endpoints => new DefaultEndpointResolver(endpoints);

        /// <summary>
        /// The port to connect on. <see cref="AmqpTcpEndpoint.UseDefaultPort"/>
        ///  indicates the default for the protocol should be used.
        /// </summary>
        public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;

        /// <summary>
        /// Timeout setting for connection attempts (in milliseconds).
        /// </summary>
        public int RequestedConnectionTimeout { get; set; } = DefaultConnectionTimeout;

        /// <summary>
        /// Timeout setting for socket read operations (in milliseconds).
        /// </summary>
        public int SocketReadTimeout { get; set; } = DefaultConnectionTimeout;

        /// <summary>
        /// Timeout setting for socket write operations (in milliseconds).
        /// </summary>
        public int SocketWriteTimeout { get; set; } = DefaultConnectionTimeout;

        /// <summary>
        /// Ssl options setting.
        /// </summary>
        public SslOption Ssl { get; set; } = new SslOption();

        /// <summary>
        /// Set to false to make automatic connection recovery not recover topology (exchanges, queues, bindings, etc).
        /// Defaults to true.
        /// </summary>
        public bool TopologyRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// Task scheduler connections created by this factory will use when
        /// dispatching consumer operations, such as message deliveries.
        /// </summary>
        [Obsolete("This scheduler is no longer used for dispatching consumer operations and will be removed in the next major version.", false)]
        public TaskScheduler TaskScheduler { get; set; } = TaskScheduler.Default;

        /// <summary>
        /// Construct a fresh instance, with all fields set to their respective defaults.
        /// </summary>


        /// <summary>
        /// Connection endpoint.
        /// </summary>
        public AmqpTcpEndpoint Endpoint
        {
            get { return new AmqpTcpEndpoint(HostName, Port, Ssl); }
            set
            {
                Port = value.Port;
                HostName = value.HostName;
                Ssl = value.Ssl;
            }
        }

        /// <summary>
        /// Dictionary of client properties to be sent to the server.
        /// </summary>
        public Dictionary<string, object> ClientProperties { get; set; }

        /// <summary>
        /// Password to use when authenticating to the server.
        /// </summary>
        public string Password { get; set; } = DefaultPass;

        /// <summary>
        /// Maximum channel number to ask for.
        /// </summary>
        public ushort RequestedChannelMax { get; set; } = DefaultChannelMax;

        /// <summary>
        /// Frame-max parameter to ask for (in bytes).
        /// </summary>
        public uint RequestedFrameMax { get; set; } = DefaultFrameMax;

        /// <summary>
        /// Heartbeat timeout to use when negotiating with the server (in seconds).
        /// </summary>
        public ushort RequestedHeartbeat { get; set; } = DefaultHeartbeat;

        /// <summary>
        /// When set to true, background thread will be used for the I/O loop.
        /// </summary>
        public bool UseBackgroundThreadsForIO { get; set; }

        /// <summary>
        /// Username to use when authenticating to the server.
        /// </summary>
        public string UserName { get; set; } = DefaultUser;

        /// <summary>
        /// Virtual host to access during this connection.
        /// </summary>
        public string VirtualHost { get; set; } = DefaultVHost;

        /// <summary>
        /// The uri to use for the connection.
        /// </summary>
        public Uri Uri
        {
            get; 
            private set;
        }

        /// <summary>
        /// Default client provided name to be used for connections.
        /// </summary>
        public string ClientProvidedName { get; set; }

        /// <summary>
        /// Given a list of mechanism names supported by the server, select a preferred mechanism,
        ///  or null if we have none in common.
        /// </summary>
        public IAuthMechanismFactory AuthMechanismFactory(IList<string> mechanismNames)
        {
            return AuthMechanisms.FirstOrDefault(
                factory =>
                    mechanismNames.Any(
                        x => 
                            string.Equals(x, factory.Name, StringComparison.OrdinalIgnoreCase)
                    )
            );
        }
        
        /// <summary>
        /// Create a connection to one of the endpoints provided by the IEndpointResolver
        /// returned by the EndpointResolverFactory. By default the configured
        /// hostname and port are used.
        /// </summary>
        /// <exception cref="BrokerUnreachableException">
        /// When the configured hostname was not reachable.
        /// </exception>
        public virtual IConnection CreateConnection()
        {
            return CreateConnection(this.EndpointResolverFactory(LocalEndpoints()), ClientProvidedName);
        }

        /// <summary>
        /// Create a connection to one of the endpoints provided by the IEndpointResolver
        /// returned by the EndpointResolverFactory. By default the configured
        /// hostname and port are used.
        /// </summary>
        /// <param name="clientProvidedName">
        /// Application-specific connection name, will be displayed in the management UI
        /// if RabbitMQ server supports it. This value doesn't have to be unique and cannot
        /// be used as a connection identifier, e.g. in HTTP API requests.
        /// This value is supposed to be human-readable.
        /// </param>
        /// <exception cref="BrokerUnreachableException">
        /// When the configured hostname was not reachable.
        /// </exception>
        public IConnection CreateConnection(String clientProvidedName)
        {
            return CreateConnection(EndpointResolverFactory(LocalEndpoints()), clientProvidedName);
        }

        /// <summary>
        /// Create a connection using a list of hostnames using the configured port.
        /// By default each hostname is tried in a random order until a successful connection is
        /// found or the list is exhausted using the DefaultEndpointResolver.
        /// The selection behaviour can be sealed overriden by configuring the EndpointResolverFactory.
        /// </summary>
        /// <param name="hostnames">
        /// List of hostnames to use for the initial
        /// connection and recovery.
        /// </param>
        /// <returns>Open connection</returns>
        /// <exception cref="BrokerUnreachableException">
        /// When no hostname was reachable.
        /// </exception>
        public IConnection CreateConnection(IList<string> hostnames)
        {
            return CreateConnection(hostnames, ClientProvidedName);
        }

        /// <summary>
        /// Create a connection using a list of hostnames using the configured port.
        /// By default each endpoint is tried in a random order until a successful connection is
        /// found or the list is exhausted.
        /// The selection behaviour can be sealed overriden by configuring the EndpointResolverFactory.
        /// </summary>
        /// <param name="hostnames">
        /// List of hostnames to use for the initial
        /// connection and recovery.
        /// </param>
        /// <param name="clientProvidedName">
        /// Application-specific connection name, will be displayed in the management UI
        /// if RabbitMQ server supports it. This value doesn't have to be unique and cannot
        /// be used as a connection identifier, e.g. in HTTP API requests.
        /// This value is supposed to be human-readable.
        /// </param>
        /// <returns>Open connection</returns>
        /// <exception cref="BrokerUnreachableException">
        /// When no hostname was reachable.
        /// </exception>
        public IConnection CreateConnection(IList<string> hostnames, String clientProvidedName)
        {
            return CreateConnection(
                new DefaultEndpointResolver(
                    hostnames.Select(
                        h => 
                            new AmqpTcpEndpoint(h, this.Port, this.Ssl)
                    )
                ), 
                clientProvidedName
            );
        }

        /// <summary>
        /// Create a connection using a list of endpoints. By default each endpoint will be tried
        /// in a random order until a successful connection is found or the list is exhausted.
        /// The selection behaviour can be sealed overriden by configuring the EndpointResolverFactory.
        /// </summary>
        /// <param name="endpoints">
        /// List of endpoints to use for the initial
        /// connection and recovery.
        /// </param>
        /// <returns>Open connection</returns>
        /// <exception cref="BrokerUnreachableException">
        /// When no hostname was reachable.
        /// </exception>
        public IConnection CreateConnection(IList<AmqpTcpEndpoint> endpoints)
        {
            return CreateConnection(new DefaultEndpointResolver(endpoints), ClientProvidedName);
        }

        /// <summary>
        /// Create a connection using an IEndpointResolver.
        /// </summary>
        /// <param name="endpointResolver">
        /// The endpointResolver that returns the endpoints to use for the connection attempt.
        /// </param>
        /// <param name="clientProvidedName">
        /// Application-specific connection name, will be displayed in the management UI
        /// if RabbitMQ server supports it. This value doesn't have to be unique and cannot
        /// be used as a connection identifier, e.g. in HTTP API requests.
        /// This value is supposed to be human-readable.
        /// </param>
        /// <returns>Open connection</returns>
        /// <exception cref="BrokerUnreachableException">
        /// When no hostname was reachable.
        /// </exception>
        public IConnection CreateConnection(IEndpointResolver endpointResolver, String clientProvidedName)
        {
            IConnection conn;
            try
            {
                if (AutomaticRecoveryEnabled)
                {
                    var autorecoveringConnection = new AutorecoveringConnection(this, clientProvidedName);
                    autorecoveringConnection.Init(endpointResolver);
                    conn = autorecoveringConnection;
                }
                else
                {
                    conn = Protocols.DefaultProtocol.CreateConnection(this, false, endpointResolver.SelectOne(selector), clientProvidedName);
                }
            }
            catch (Exception e)
            {
                throw new BrokerUnreachableException(e);
            }

            return conn;
        }

        public IFrameHandler CreateHyperFrameHandler(AmqpTcpEndpoint endpoint)
        {
            return new HyperSocketFrameHandler(new HyperSocketFrameSettings(endpoint,RequestedHeartbeat,RequestedConnectionTimeout,SocketReadTimeout,SocketWriteTimeout));
        }


        public IFrameHandler CreateFrameHandlerForHostname(string hostname)
        {
            return CreateHyperFrameHandler(this.Endpoint.CloneWithHostname(hostname));
        }

        private static readonly char[] delimiter = new char[] { ':' };

        public void SetUri(Uri uri)
        {
            const string amqp = "amqp";
            const string amqps = "amqps";
            Endpoint = new AmqpTcpEndpoint();

            if (string.Equals(amqp, uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                // nothing special to do
            }
            else if (string.Equals(amqps, uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                Ssl.Enabled = true;
                Ssl.Version = AmqpUriSslProtocols;
#if !(NETFX_CORE)
                Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch;
#endif
                Port = AmqpTcpEndpoint.DefaultAmqpSslPort;
            }
            else
            {
                throw new ArgumentException("Wrong scheme in AMQP URI: " + uri.Scheme);
            }
            string host = uri.Host;
            if (!string.IsNullOrEmpty(host))
            {
                HostName = host;
            }
            Ssl.ServerName = HostName;

            int port = uri.Port;
            if (port != -1)
            {
                Port = port;
            }

            string userInfo = uri.UserInfo;
            if (!string.IsNullOrEmpty(userInfo))
            {
                string[] userPass = userInfo.Split(delimiter);
                if (userPass.Length > 2)
                {
                    throw new ArgumentException("Bad user info in AMQP " + "URI: " + userInfo);
                }
                UserName = UriDecode(userPass[0]);
                if (userPass.Length == 2)
                {
                    Password = UriDecode(userPass[1]);
                }
            }

            /* C# automatically changes URIs into a canonical form
               that has at least the path segment "/". */
            if (uri.Segments.Length > 2)
            {
                throw new ArgumentException("Multiple segments in " +
                                            "path of AMQP URI: " +
                                            string.Join(", ", uri.Segments));
            }
            if (uri.Segments.Length == 2)
            {
                VirtualHost = UriDecode(uri.Segments[1]);
            }

            this.Uri = uri;
        }

        ///<summary>
        /// Unescape a string, protecting '+'.
        /// </summary>
        private static string UriDecode(string uri)
        {
            return System.Uri.UnescapeDataString(uri.Replace("+", "%2B"));
        }

        private List<AmqpTcpEndpoint> LocalEndpoints()
        {
            return new List<AmqpTcpEndpoint> { this.Endpoint };
        }
    }
}
