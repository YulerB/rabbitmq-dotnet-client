using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

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

            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void PooledMemoryStream()
            {
                Stack<DisposableMemoryStreamWrapper> streams1 = new Stack<DisposableMemoryStreamWrapper>();

                for (int i = 0; i < 10000; i++)
                {
                    streams1.Push(MemoryStreamPool.GetObject());
                    if (i > 3) streams1.Pop().Dispose();
                }
                streams1.Pop().Dispose(); streams1.Pop().Dispose(); streams1.Pop().Dispose();
            }

            [Benchmark]
            public void NewMemoryStream()
            {
                Stack<MemoryStream> streams2 = new Stack<MemoryStream>();

                for (int i = 0; i < 10000; i++)
                {
                    streams2.Push(new MemoryStream(256));
                    if (i > 3) streams2.Pop().Dispose();
                }
                streams2.Pop().Dispose(); streams2.Pop().Dispose(); streams2.Pop().Dispose();
            }
        }

    }
}
