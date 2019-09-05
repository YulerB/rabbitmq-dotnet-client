using NUnit.Framework;
using System.IO;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using RabbitMQ.Util;
using System;
using RabbitMQ.Client.Unsafe;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using RabbitMQ.Client.Impl;

namespace Unit.src.benches
{
    [TestFixture]
    public class NetworkBinaryReaderTests
    {
        [Test]
        public void TestReadDouble()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(new byte[] {
                    0,1,0,1,0,1,0,1,0,1
                }, 0, 10);
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new NetworkBinaryReader(stream);
            var reader1 = new NewNetworkBinaryReader(stream);
            var values = reader.ReadDouble();

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader.ReadDouble(), "ReadDouble1");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadDouble2(), "ReadDouble2");


            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadDouble4(), "ReadDouble4");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadDouble5(), "ReadDouble4");
        }
        [Test]
        public void TestReadInt32()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(new byte[] {
                    0,1,0,1,0,1,0,1,0,1
                }, 0, 10);
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new NetworkBinaryReader(stream);
            var reader1 = new NewNetworkBinaryReader(stream);
            var values = reader.ReadInt32();

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt32(), "ReadInt32");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt322(), "ReadInt322");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt323(), "ReadInt323");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt324(), "ReadInt324");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt325(), "ReadInt324");
        }
        [Test]
        public void TestReadInt64()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(new byte[] {
                    0,1,0,1,0,1,0,1,0,1
                }, 0, 10);
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new NetworkBinaryReader(stream);
            var reader1 = new NewNetworkBinaryReader(stream);
            var values = reader.ReadInt64();

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt64(), "ReadInt64");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt642(), "ReadInt642");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadInt643(), "ReadInt643");
        }
        [Test]
        public void TestReadUInt32()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(new byte[] {
                    0,1,0,1,0,1,0,1,0,1
                }, 0, 10);
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new NetworkBinaryReader(stream);
            var reader1 = new NewNetworkBinaryReader(stream);
            var values = reader.ReadInt32();

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadUInt32(), "ReadInt32");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadUInt322(), "ReadInt322");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadUInt323(), "ReadInt323");
        }
        [Test]
        public void TestReadUInt64()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(new byte[] {
                    0,1,0,1,0,1,0,1,0,1
                }, 0, 10);
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new NetworkBinaryReader(stream);
            var reader1 = new NewNetworkBinaryReader(stream);
            var values = reader.ReadInt64();

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadUInt64(), "ReadInt64");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadUInt642(), "ReadInt642");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadUInt643(), "ReadInt643");

            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(values, reader1.ReadUInt64(), "ReadInt644");
        }
        [Test]
        public void TestBenchReadDouble()
        {
            var summary = BenchmarkRunner.Run<BenchReadDouble>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }      
        [Test]
        public void TestBenchReadInt64()
        {
            var summary = BenchmarkRunner.Run<BenchReadInt64>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }
        [Test]
        public void TestBenchReadInt16()
        {
            var summary = BenchmarkRunner.Run<BenchReadInt16>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }
        [Test]
        public void TestBenchReadInt32()
        {
            var summary = BenchmarkRunner.Run<BenchReadInt32>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }
        [Test]
        public void TestBenchReadUInt32()
        {
            var summary = BenchmarkRunner.Run<BenchReadUInt32>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }
        [Test]
        public void TestBenchReadUInt64()
        {
            var summary = BenchmarkRunner.Run<BenchReadUInt64>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }
        [Test]
        public void TestBenchReadSingle()
        {
            var summary = BenchmarkRunner.Run<BenchReadSingle>(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        }

        [MemoryDiagnoser]
        [CoreJob]
        [RankColumn]
        public class BenchReadDouble
        {
            byte[] shared = new byte[10] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void TestReadDouble()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NetworkBinaryReader(stream);
                reader.ReadDouble();
            }

            [Benchmark]
            public void TestNewReadDouble()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadDouble();
            }

            [Benchmark]
            public void TestReadDouble2()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadDouble2();
            }



            [Benchmark]
            public void TestReadDouble4()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadDouble4();
            }


            [Benchmark]
            public void TestReadDouble5()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadDouble5();
            }
        }
        [MemoryDiagnoser]
        [CoreJob]
        [RankColumn]
        public class BenchReadInt16
        {
            byte[] shared = new byte[10] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };

            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void TestReadInt16()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NetworkBinaryReader(stream);
                reader.ReadInt16();
            }

            [Benchmark]
            public void TestNewReadInt16()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt16();
            }

            [Benchmark]
            public void TestNewReadInt162()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt162();
            }
            [Benchmark]
            public void TestNewReadInt163()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt163();
            }
            [Benchmark]
            public void TestNewReadInt164()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt164();
            }

            [Benchmark]
            public void TestNewReadInt165()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt165();
            }

        }
        [MemoryDiagnoser]
        [CoreJob]
        [RankColumn]
        public class BenchReadSingle
        {

            byte[] shared = new byte[10] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void TestReadSingle()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NetworkBinaryReader(stream);
                reader.ReadSingle();
            }
            

            [Benchmark]
            public void TestReadSingle2()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadSingle2();
            }
            [Benchmark]
            public void TestReadSingle3()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadSingle3();
            }
            [Benchmark]
            public void TestReadSingle4()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadSingle4();
            }

        }
        [MemoryDiagnoser]
        [CoreJob]
        [RankColumn]
        public class BenchReadInt64
        {
            byte[] shared = new byte[10] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };

            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void TestReadInt64()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NetworkBinaryReader(stream);
                reader.ReadInt16();
            }

            [Benchmark]
            public void TestNewReadInt642()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt642();
            }


            [Benchmark]
            public void TestNewReadInt643()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt643();
            }

        }
        [MemoryDiagnoser]
        [CoreJob]
        [RankColumn]
        public class BenchReadInt32
        {
            byte[] shared = new byte[10] {0,1,0,1,0,1,0,1,0,1};
            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void TestReadInt32()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NetworkBinaryReader(stream);
                reader.ReadInt16();
            }

            [Benchmark]
            public void TestNewReadInt322()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt322();
            }


            [Benchmark]
            public void TestNewReadInt323()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt323();
            }


            [Benchmark]
            public void TestNewReadInt324()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt324();
            }

            [Benchmark]
            public void TestNewReadInt325()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadInt325();
            }

        }
        [MemoryDiagnoser]
        [CoreJob]
        [RankColumn]
        public class BenchReadUInt32
        {

            byte[] shared = new byte[10] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void TestReadUInt32()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NetworkBinaryReader(stream);
                reader.ReadInt16();
            }

            [Benchmark]
            public void TestNewReadUInt322()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadUInt322();
            }

            [Benchmark]
            public void TestNewReadUInt323()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadUInt323();
            }
        }
        [MemoryDiagnoser]
        [CoreJob]
        [RankColumn]
        public class BenchReadUInt64
        {

            byte[] shared = new byte[10] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public void TestReadUInt64()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NetworkBinaryReader(stream);
                reader.ReadInt16();
            }

            [Benchmark]
            public void TestNewReadUInt642()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadUInt642();
            }

            [Benchmark]
            public void TestNewReadUInt643()
            {
                MemoryStream stream = new MemoryStream(shared);
                stream.Position = 0;
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new NewNetworkBinaryReader(stream);
                reader.ReadUInt643();
            }

            [Benchmark]
            public void TestNewReadUInt644()
            {
                ArraySegmentStream stream = new ArraySegmentStream(shared);
                var reader = new NetworkArraySegmentsReader(stream);
                reader.ReadUInt64();
            }
        }

        private class NewNetworkBinaryReader : BinaryReader
        {
            // Not particularly efficient. To be more efficient, we could
            // reuse BinaryReader's implementation details: m_buffer and
            // FillBuffer, if they weren't private
            // members. Private/protected claim yet another victim, film
            // at 11. (I could simply cut-n-paste all that good code from
            // BinaryReader, but two wrongs do not make a right)

            /// <summary>
            /// Construct a NetworkBinaryReader over the given input stream.
            /// </summary>
            public NewNetworkBinaryReader(Stream input) : base(input)
            {
            }

            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override double ReadDouble()
            {
                var byes = base.ReadBytes(8);
                return BitConverter.ToDouble(new byte[] { byes[7], byes[6], byes[5], byes[4], byes[3], byes[2], byes[1], byes[0] }, 0);
            }
            public double ReadDouble2()
            {
                var byes = new byte[8];
                byes[7] = base.ReadByte();
                byes[6] = base.ReadByte();
                byes[5] = base.ReadByte();
                byes[4] = base.ReadByte();
                byes[3] = base.ReadByte();
                byes[2] = base.ReadByte();
                byes[1] = base.ReadByte();
                byes[0] = base.ReadByte();
                return BitConverter.ToDouble(byes, 0);
            }
            public double ReadDouble4()
            {
                return LittleEndianBitConverter.ToDouble(base.ReadBytes(8), 0);
            }
            public double ReadDouble5()
            {
                var span = new Span<byte>(base.ReadBytes(8));
                span.Reverse();
                return MemoryMarshal.Cast<byte, double>(span)[0];
            }

            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override short ReadInt16()
            {
                byte[] i = base.ReadBytes(2);
                return BitConverter.ToInt16(new byte[] { i[1], i[0] }, 0);
            }
            public short ReadInt162()
            {
                byte[] i = new byte[2];
                i[1] = base.ReadByte();
                i[0] = base.ReadByte();
                return BitConverter.ToInt16(i, 0);
            }
            public short ReadInt163()
            {
                uint i = base.ReadUInt16();
                return (short)(((i & 0xFF00) >> 8) |
                                ((i & 0x00FF) << 8));
            }
            public short ReadInt164()
            {
                return LittleEndianBitConverter.ToInt16(base.ReadBytes(2), 0);
            }
            public short ReadInt165()
            {
                return BinaryPrimitives.ReadInt16BigEndian(new ReadOnlySpan<byte>(base.ReadBytes(2)));
            }

            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override int ReadInt32()
            {
                uint i = base.ReadUInt32();
                return (int)(((i & 0xFF000000) >> 24) |
                             ((i & 0x00FF0000) >> 8) |
                             ((i & 0x0000FF00) << 8) |
                             ((i & 0x000000FF) << 24));
            }
            public int ReadInt322()
            {
                byte[] i = base.ReadBytes(4);
                byte tmp = i[0];
                i[0] = i[3];
                i[3] = tmp;
                tmp = i[1];
                i[1] = i[2];
                i[2] = tmp;
                return BitConverter.ToInt32(i, 0);
            }
            public int ReadInt323()
            {
                byte[] i = new byte[4];
                i[3] = base.ReadByte();
                i[2] = base.ReadByte();
                i[1] = base.ReadByte();
                i[0] = base.ReadByte();
                return BitConverter.ToInt32(i, 0);
            }
            public int ReadInt324()
            {
                return LittleEndianBitConverter.ToInt32(base.ReadBytes(4), 0);
            }
            public int ReadInt325()
            {
                return BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(base.ReadBytes(4)));
            }

            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override long ReadInt64()
            {
                ulong i = base.ReadUInt64();
                return (long)(((i & 0xFF00000000000000) >> 56) |
                              ((i & 0x00FF000000000000) >> 40) |
                              ((i & 0x0000FF0000000000) >> 24) |
                              ((i & 0x000000FF00000000) >> 8) |
                              ((i & 0x00000000FF000000) << 8) |
                              ((i & 0x0000000000FF0000) << 24) |
                              ((i & 0x000000000000FF00) << 40) |
                              ((i & 0x00000000000000FF) << 56));
            }
            public long ReadInt642()
            {
                return LittleEndianBitConverter.ToInt64(base.ReadBytes(8), 0);
            }
            public long ReadInt643()
            {
                return BinaryPrimitives.ReadInt64BigEndian(new ReadOnlySpan<byte>(base.ReadBytes(8)));
            }

            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override float ReadSingle()
            {
                byte[] bytes = ReadBytes(4);
                byte temp = bytes[0];
                bytes[0] = bytes[3];
                bytes[3] = temp;
                temp = bytes[1];
                bytes[1] = bytes[2];
                bytes[2] = temp;
                return BitConverter.ToSingle(bytes, 0);
            }
            public float ReadSingle2()
            {
                uint i = base.ReadUInt32();
                return (float)(((i & 0xFF000000) >> 24) |
                             ((i & 0x00FF0000) >> 8) |
                             ((i & 0x0000FF00) << 8) |
                             ((i & 0x000000FF) << 24));
            }
            public float ReadSingle3()
            {
                return LittleEndianBitConverter.ToSingle(base.ReadBytes(4), 0);
            }
            public float ReadSingle4()
            {
                var span = new Span<byte>(base.ReadBytes(4));
                span.Reverse();
                return MemoryMarshal.Cast<byte, float>(span)[0];
            }

            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override ushort ReadUInt16()
            {
                uint i = base.ReadUInt16();
                return (ushort)(((i & 0xFF00) >> 8) |
                                ((i & 0x00FF) << 8));
            }
            public ushort ReadUInt162()
            {
                return LittleEndianBitConverter.ToUInt16(base.ReadBytes(2), 0);
            }
            public ushort ReadUInt163()
            {
                return BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(base.ReadBytes(2)));
            }

            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override uint ReadUInt32()
            {
                uint i = base.ReadUInt32();
                return (((i & 0xFF000000) >> 24) |
                        ((i & 0x00FF0000) >> 8) |
                        ((i & 0x0000FF00) << 8) |
                        ((i & 0x000000FF) << 24));
            }
            public uint ReadUInt322()
            {
                return LittleEndianBitConverter.ToUInt32(base.ReadBytes(4), 0);
            }
            public uint ReadUInt323()
            {
                return BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(base.ReadBytes(4)));
            }
            
            /// <summary>
            /// Override BinaryReader's method for network-order.
            /// </summary>
            public override ulong ReadUInt64()
            {
                ulong i = base.ReadUInt64();
                return (((i & 0xFF00000000000000) >> 56) |
                        ((i & 0x00FF000000000000) >> 40) |
                        ((i & 0x0000FF0000000000) >> 24) |
                        ((i & 0x000000FF00000000) >> 8) |
                        ((i & 0x00000000FF000000) << 8) |
                        ((i & 0x0000000000FF0000) << 24) |
                        ((i & 0x000000000000FF00) << 40) |
                        ((i & 0x00000000000000FF) << 56));
            }
            public ulong ReadUInt642()
            {
                return LittleEndianBitConverter.ToUInt64(base.ReadBytes(8), 0);
            }
            public ulong ReadUInt643()
            {
                return BinaryPrimitives.ReadUInt64BigEndian(new ReadOnlySpan<byte>(base.ReadBytes(8)));
            }
        }
    }
}