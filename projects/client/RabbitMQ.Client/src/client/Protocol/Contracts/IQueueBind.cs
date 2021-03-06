namespace RabbitMQ.Client.Framing
{
    /// <summary>Autogenerated type. AMQP specification method "queue.bind".</summary>
    public interface IQueueBind : IMethod
    {
        ushort Reserved1 { get; }
        string Queue { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        bool Nowait { get; }
        System.Collections.Generic.Dictionary<string, object> Arguments { get; }
    }
}
