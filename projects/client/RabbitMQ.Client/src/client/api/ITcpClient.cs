#if !NETFX_CORE
using System;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Wrapper interface for standard TCP-client. Provides socket for socket frame handler class.
    /// </summary>
    /// <remarks>Contains all methods that are currenty in use in rabbitmq client.</remarks>
    public interface ITcpClient : IDisposable
    {
        bool Connected { get; }
        int ReceiveTimeout { get; set; }
        EndPoint ClientLocalEndPoint { get; }
        EndPoint ClientRemoteEndPoint { get; }
        int ClientReceiveBufferSize { get; }
        int ClientSendTimeout{set;}
        bool ClientPollCanWrite(int m_writeableStateTimeout);
        Task ConnectAsync(string host, int port);
        NetworkStream GetStream();
        void Close();
    }
    public interface IHyperTcpClient : IDisposable
    {
        bool Connected { get; }
        int ReceiveTimeout { get; set; }
        EndPoint ClientLocalEndPoint { get; }
        EndPoint ClientRemoteEndPoint { get; }
        int ClientReceiveBufferSize { get; }
        int ClientSendTimeout { set; }
        bool ClientPollCanWrite(int m_writeableStateTimeout);

        Task ConnectAsync(string host, int port);
        Task SecureConnectAsync(string host, int port, X509CertificateCollection certs, RemoteCertificateValidationCallback remoteCertValidator, LocalCertificateSelectionCallback localCertSelector, bool checkCertRevocation);

        void Write(ArraySegment<byte> data);
        event EventHandler<ArraySegment<byte>> Receive;

        void Close();
    }
}
#endif
