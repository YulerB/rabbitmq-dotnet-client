using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Unit.src.unit
{
    [TestFixture]
    public class TestObjectPool
    {
        [Test]
        public void TestCreateObjectPool()
        {
            ObjectPool<MemoryStream> pool = new ObjectPool<MemoryStream>(() => new MemoryStream(), m => {
                m.Position = 0;
                m.SetLength(0);
            });
            
        }
        [Test]
        public void TestGetObjectPoolItem()
        {
            ObjectPool<MemoryStream> pool = new ObjectPool<MemoryStream>(() => new MemoryStream(), m => {
                m.Position = 0;
                m.SetLength(0);
            });
            
            using (var disposer = pool.GetObject())
            {
                Assert.IsNotNull(disposer.Instance);
                disposer.Disposing += (sender, e) =>
                {
                    Assert.IsTrue(e != null && e is MemoryStream);
                };
            }
        }
        [Test]
        public void TestPoolerPool()
        {
            using (var disposer = Pooler.MemoryStreamPool.GetObject())
            {
                Assert.IsNotNull(disposer.Instance);
                disposer.Disposing += (sender, e) =>
                {
                    Assert.IsTrue(e != null && e is MemoryStream);
                };
            }
        }
    }
}
