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

        [MemoryDiagnoser]
        //[RyuJitX64Job]
        //[DisassemblyDiagnoser]
        [CoreJob]
        [BenchmarkDotNet.Attributes.HardwareCounters]
        [RankColumn]
        public class Benches
        {
            readonly int processors = Environment.ProcessorCount;

            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void Mine()
            {
                TestEventingConsumer ec = new TestEventingConsumer();
                ec.Init();
                ec.TestEventingConsumerDeliveryEventsWithAck1Short();
                ec.Dispose();
            }

            [Benchmark]
            public void Existing()
            {
                TestEventingConsumer1 ec = new TestEventingConsumer1();
                ec.Init();
                ec.TestEventingConsumerDeliveryEventsWithAck1Short();
                ec.Dispose();
            }
        }
    }
}
