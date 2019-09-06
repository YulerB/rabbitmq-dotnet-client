﻿using NUnit.Framework;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestArraySegmentStream
    {
        [Test]
        public void ArraySegmentStreamTest()
        {
            byte[] buffer = new byte[] { 200, 199, 197, 196, 195};
            ArraySegmentStream stream = new ArraySegmentStream();
            stream.Write(buffer, 0, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);

            var segments = stream.Read(1);
            Assert.AreEqual(1, segments[0].Length, "Test 1");
            Assert.AreEqual(buffer[0], segments[0].Span[0], "Test 2");

            segments = stream.Read(2);
            Assert.AreEqual(2, segments[0].Length, "Test 3");
            Assert.AreEqual(buffer[1], segments[0].Span[0], "Test 4");
            Assert.AreEqual(buffer[2], segments[0].Span[1], "Test 5");

            segments = stream.Read(4);
            Assert.AreEqual(2, segments[0].Length, "Test 6");
            Assert.AreEqual(2, segments[1].Length, "Test 7");
            Assert.AreEqual(buffer[3], segments[0].Span[0], "Test 8");
            Assert.AreEqual(buffer[4], segments[0].Span[1], "Test 9");
            Assert.AreEqual(buffer[0], segments[1].Span[0], "Test 10");
            Assert.AreEqual(buffer[1], segments[1].Span[1], "Test 11");
        }
        [Test]
        public void NetworkArraySegmentsReaderTest()
        {
            Random r = new Random();
            byte[] buffer = new byte[5];// {r.Next(65, 198) , 199, 197, 196, 195 };
            r.NextBytes(buffer);
            ArraySegmentStream stream = new ArraySegmentStream();
            for (int i = 0; i < 300; i++)
            {
                stream.Write(buffer, 0, buffer.Length);
            }

            MemoryStream ms = new MemoryStream();
            for (int i = 0; i < 256/5; i++)
            {
                ms.Write(buffer, 0, 5);
            }

            NetworkArraySegmentsReader reader = new NetworkArraySegmentsReader(stream);
            Assert.AreEqual(buffer[0], reader.ReadByte(), "Test 3");
            Assert.AreEqual(new byte[] { buffer[1], buffer[2], buffer[3] }, reader.ReadBytes(3), "Test 4");
            Assert.AreEqual(new byte[] { buffer[4], buffer[0], buffer[1] }, reader.ReadBytes(3), "Test 5");
            Assert.AreEqual(BitConverter.ToUInt16(new byte[] { buffer[3], buffer[2] }, 0), reader.ReadUInt16(), "Test 6");
            Assert.AreEqual(buffer[4], reader.ReadByte(), "Test 7");
            Assert.AreEqual(BitConverter.ToDouble(new byte[] { buffer[2], buffer[1], buffer[0], buffer[4], buffer[3], buffer[2], buffer[1], buffer[0] }, 0), reader.ReadDouble(), "Test 8");
            Assert.AreEqual(BitConverter.ToInt16(new byte[] { buffer[4], buffer[3] }, 0), reader.ReadInt16(), "Test 9");
            Assert.AreEqual(BitConverter.ToInt64(new byte[] { buffer[2], buffer[1], buffer[0], buffer[4], buffer[3], buffer[2], buffer[1], buffer[0] }, 0), reader.ReadInt64(), "Test 10");
            Assert.AreEqual(BitConverter.ToSingle(new byte[] { buffer[1], buffer[0], buffer[4], buffer[3] }, 0), reader.ReadSingle(), "Test 11");
            Assert.AreEqual(BitConverter.ToUInt64(new byte[] { buffer[4], buffer[3], buffer[2], buffer[1], buffer[0], buffer[4], buffer[3], buffer[2] }, 0), reader.ReadUInt64(), "Test 12");
            Assert.AreEqual(new byte[] { buffer[0], buffer[1], buffer[2], buffer[3], buffer[4] }, reader.ReadBytes(5), "Test 13");
            Assert.AreEqual(System.Text.Encoding.UTF8.GetString(ms.ToArray(), 1, buffer[0]), reader.ReadShortString(out long read), "Test 14");

            uint FiveHundred = 500;
            byte[] bytes = BitConverter.GetBytes(FiveHundred);
            byte[] newBytes = new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] };


            stream = new ArraySegmentStream();
            stream.Write(newBytes, 0, newBytes.Length);
            ms = new MemoryStream();
            for (int i = 0; i < FiveHundred/5; i++)
            {
                stream.Write(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, buffer.Length);
            }
            reader = new NetworkArraySegmentsReader(stream);

            Assert.AreEqual(System.Text.Encoding.UTF8.GetString(ms.ToArray(), 0, Convert.ToInt32(FiveHundred)), reader.ReadLongString(out long read1), "Test 15");

            uint twentyFive = 25;
            bytes = BitConverter.GetBytes(twentyFive);
            newBytes = new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] };

            stream = new ArraySegmentStream();
            stream.Write(newBytes, 0, newBytes.Length);
            stream.WriteByte((byte) 'I');
            stream.Write(buffer, 0, 4);
            stream.WriteByte((byte)'I');
            stream.Write(buffer, 0, 4);
            stream.WriteByte((byte)'I');
            stream.Write(buffer, 0, 4);
            stream.WriteByte((byte)'I');
            stream.Write(buffer, 0, 4);
            stream.WriteByte((byte)'I');
            stream.Write(buffer, 0, 4);
            reader = new NetworkArraySegmentsReader(stream);
            
            var result = reader.ReadArray(out long read2);
            Assert.AreEqual(5, result.Count, "Test 16");
            Assert.AreEqual(BitConverter.ToInt32(new byte[] { buffer[3], buffer[2],buffer[1], buffer[0] }, 0),result[0], "Test 17");
        }
    }
}
