using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;

namespace Unit.src.benches
{
    [TestFixture]
    public class MemoryStreamPooling
    {
        [Test]
        public void TestMemoryStreamPooling()
        {
            var summary = BenchmarkRunner.Run<Benches>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }

        [MemoryDiagnoser]
        [RyuJitX64Job]
        [CoreJob]
        [RankColumn]
        public class Benches
        {
            int processors = Environment.ProcessorCount;

            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void PooledMemoryStream()
            {
                for (int i = 0; i < 10000; i++)
                {
                    using (var ms = MemoryStreamPool.GetObject())
                    {
                        //do nothing
                    }
                }
            }

            [Benchmark]
            public void NewMemoryStream()
            {
                for (int i = 0; i < 10000; i++)
                {
                    using (var ms = new MemoryStream(256))
                    {
                        //do nothing
                    }
                }
            }
        }

    }
}
