// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2016 Pivotal Software, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------
using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestObjectPool
    {
        [Test]
        public void TestGetObjectPoolItem()
        {
            using (var disposer = MemoryStreamPool.GetObject())
            {
                Assert.IsNotNull(disposer.Instance);
            }
        }

        [Test]
        public void TestPoolVsNative()
        {
            long ticks1 = 0;
            long ticks2 = 0;
            Stopwatch sw1 = null;
            Stopwatch sw2 = null;
            Stack<DisposableMemoryStreamWrapper> streams1 = new Stack<DisposableMemoryStreamWrapper>();
            Stack<MemoryStream> streams2 = new Stack<MemoryStream>();

            {
                sw1 = Stopwatch.StartNew();

                for (int i = 0; i < 10000; i++)
                {
                    streams1.Push(MemoryStreamPool.GetObject());
                    if (i > 3) streams1.Pop().Dispose();  
                }
                streams1.Pop().Dispose(); streams1.Pop().Dispose(); streams1.Pop().Dispose();

                ticks1 = sw1.ElapsedTicks;
            }

            {
                sw2 = Stopwatch.StartNew();

                for (int i = 0; i < 10000; i++)
                {
                    streams2.Push(new MemoryStream(256));
                    if (i > 3) streams2.Pop().Dispose();
                }
                streams2.Pop().Dispose(); streams2.Pop().Dispose(); streams2.Pop().Dispose();

                ticks2 = sw2.ElapsedTicks;
            }

            Console.WriteLine(ticks1);
            Console.WriteLine(ticks2);
            Assert.IsTrue(ticks1 < ticks2);
        }

        [Test]
        public void TestPoolerPool()
        {
            using (var disposer = MemoryStreamPool.GetObject())
            {
                Assert.IsNotNull(disposer.Instance);
            }
        }
    }
}
