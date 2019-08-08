using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Unit.src.unit
{
    [TestFixture]
    public class TestDisposableWrapper
    {
        [Test]
        public void TestCreateDisposableWrapper()
        {
            using (var ms = new MemoryStream())
            {
                using (DisposableWrapper<MemoryStream> disposer = new DisposableWrapper<MemoryStream>(ms))
                {
                    Assert.AreEqual(ms, disposer.Instance);
                    disposer.Disposing += (sender, e) =>
                    {
                        Assert.AreEqual(ms, e);
                    };
                }
            }
        }
    }
}
