using RabbitMQ.Client.Unit;
using System;

namespace PerfRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            TestExtensions e = new TestExtensions();
            e.Init();
            e.TestExchangeBinding();
            e.Dispose();


            //TestEventingConsumer c = new TestEventingConsumer();
            //try
            //{
            //    c.Init();
            //    c.TestEventingConsumerDeliveryEventsWithAck1Short();
            //}
            //finally
            //{
            //    c.Dispose();
            //}
        }
    }
}
