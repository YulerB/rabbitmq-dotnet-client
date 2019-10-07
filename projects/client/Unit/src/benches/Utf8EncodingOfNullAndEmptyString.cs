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
    public class Utf8EncodingOfNullAndEmptyString
    {
        [Test]
        public void TestUtf8EncodingOfNullAndEmptyString()
        {
            var summary = BenchmarkRunner.Run<Benches>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }
        [Test]
        public void TestUtf8EncodingOfNull()
        {
            string s = null;
            foreach (var utf8Byte in System.Text.Encoding.UTF8.GetBytes(s)){
                Console.WriteLine(utf8Byte);
            }
        }

        [Test]
        public void TestUtf8EncodingOfEmptyString()
        {
            foreach (var utf8Byte in System.Text.Encoding.UTF8.GetBytes(string.Empty))
            {
                Console.WriteLine(utf8Byte);
            }
        }

        [Test]
        public void TestGetStringZeroBytes()
        {
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(new byte[]{ }).Length);
        }

        

        [MemoryDiagnoser]
        [RyuJitX64Job]
        [CoreJob]
        [RankColumn]
        public class Benches
        {
            string s = null;
            string e = string.Empty;

            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void Utf8EncodingOfNull()
            {
                System.Text.Encoding.UTF8.GetBytes(s);
            }

            [Benchmark]
            public void Utf8EncodingOfEmptyString()
            {
                System.Text.Encoding.UTF8.GetBytes(e);
            }
        }
    }
}
