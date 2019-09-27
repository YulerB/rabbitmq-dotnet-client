﻿using RabbitMQ.Client.Unit;
using System;

namespace PerfRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            TestEventingConsumer c = new TestEventingConsumer();
            try
            {
                c.Init();
                c.TestEventingConsumerDeliveryEventsWithAck1();
                c.TestEventingConsumerDeliveryEventsNoAck1();
                c.TestEventingConsumerDeliveryEventsNoAck();
                c.TestEventingConsumerDeliveryEventsWithAck();
                c.TestEventingConsumerRegistrationEvents();
                c.TestEventingConsumerDeliveryEvents();
            }
            finally
            {
                c.Dispose();
            }
        }
    }
}
