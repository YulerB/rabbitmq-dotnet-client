using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using rcu;
using RabbitMQ.Client.Unit;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Order;

namespace Unit.src.benches
{
    [TestFixture]
    public class RunEventingConsumeBenchTestFixture
    {
        [Test]
        public void RunEventingConsumeBenchTest()
        {
            var summary = BenchmarkRunner.Run<Benches>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }

        [CoreJob]
        [MemoryDiagnoser]
        //[InliningDiagnoser]
        [RankColumn]
        //[TailCallDiagnoser]
        [Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
        public class Benches
        {
            TestEventingConsumer ec;
            TestEventingConsumer1 ec1; 

            [GlobalSetup]
            public void Setup()
            {
                ec = new TestEventingConsumer();
                ec1 = new TestEventingConsumer1();
                ec.Init();
                ec1.Init();
            }
            [GlobalCleanup]
            public void Cleanup()
            {
                ec.Dispose();
                ec1.Dispose();
                ec = null;
                ec1 = null;
            }

            [Benchmark]
            public void Mine()
            {
                ec.TestEventingConsumerDeliveryEventsWithAck1Short();
            }

            [Benchmark]
            public void Existing()
            {
                ec1.TestEventingConsumerDeliveryEventsWithAck1Short();
            }
        }
    }
}
