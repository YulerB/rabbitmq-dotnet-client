using NUnit.Framework;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unit.src.unit
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
            Assert.AreEqual(buffer[0], segments[0].ToArray()[0], "Test 2");

            segments = stream.Read(2);
            Assert.AreEqual(2, segments[0].Length, "Test 3");
            Assert.AreEqual(buffer[1], segments[0].ToArray()[0], "Test 4");
            Assert.AreEqual(buffer[2], segments[0].ToArray()[1], "Test 5");

            segments = stream.Read(4);
            Assert.AreEqual(2, segments[0].Length, "Test 6");
            Assert.AreEqual(2, segments[1].Length, "Test 7");
            Assert.AreEqual(buffer[3], segments[0].ToArray()[0], "Test 8");
            Assert.AreEqual(buffer[4], segments[0].ToArray()[1], "Test 9");
            Assert.AreEqual(buffer[0], segments[1].ToArray()[0], "Test 10");
            Assert.AreEqual(buffer[1], segments[1].ToArray()[1], "Test 11");

        }
        [Test]
        public void NetworkArraySegmentsReaderTest()
        {
            byte[] buffer = new byte[] { 200, 199, 197, 196, 195 };
            ArraySegmentStream stream = new ArraySegmentStream();
            stream.Write(buffer, 0, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
            NetworkArraySegmentsReader reader = new NetworkArraySegmentsReader(stream);
            Assert.AreEqual(buffer[0], reader.ReadByte(), "Test 3");
            Assert.AreEqual(new byte[] { 199, 197, 196 }, reader.ReadBytes(3), "Test 4");
            Assert.AreEqual(new byte[] { 195, 200, 199 }, reader.ReadBytes(3), "Test 5");
            Assert.AreEqual(BitConverter.ToUInt16(new byte[] { 196, 197 }, 0), reader.ReadUInt16(), "Test 6");
            Assert.AreEqual(buffer[4], reader.ReadByte(), "Test 7");


        }
    }
}
