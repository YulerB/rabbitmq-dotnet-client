#if !NETFX_CORE
using System.Net.Sockets;

namespace RabbitMQ.Client
{
    public class HyperTcpClientSettings
    {
        public HyperTcpClientSettings(AmqpTcpEndpoint endPoint, AddressFamily addressFamily, int requestedHeartbeat)
        {
            this.EndPoint = endPoint;
            this.AddressFamily = addressFamily;
            this.RequestedHeartbeat = requestedHeartbeat;
        }
        public AmqpTcpEndpoint EndPoint { get; private set; }
        public AddressFamily AddressFamily { get;private set; }
        public int RequestedHeartbeat { get; private set; }
    }
}
#endif