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

using System;
using System.Text;

namespace RabbitMQ.Client.Unit {
    [TestFixture]
    public class TestStreamRingBuffer  {

        [Test]
        public void StreamRingBufferTest()
        {
            Random r = new Random();
            const int capacity = 2;
            ReadOnlyMemory<byte> taken=null;
            ArraySegment<byte> peek= default;
            StreamRingBuffer ringBuffer = new StreamRingBuffer(capacity);
            for (int i = 0; i < 2; i++)
            {
                peek = ringBuffer.Peek();
                Assert.IsTrue(peek.Count > 0);
                taken =  ringBuffer.Take(1);
                Assert.IsTrue(taken.Length == 1);
            }
            ringBuffer.Release(taken.Length);
            peek = ringBuffer.Peek();
            Assert.IsTrue(peek.Count > 0);
            taken = ringBuffer.Take(1);
            Assert.IsTrue(taken.Length == 1);
            peek = ringBuffer.Peek();
            Assert.IsTrue(peek.Count == 0);
            ringBuffer.Release(2);
            for (int i = 0; i < 2; i++)
            {
                peek = ringBuffer.Peek();
                Assert.IsTrue(peek.Count > 0);
                taken = ringBuffer.Take(1);
                Assert.IsTrue(taken.Length == 1);
            }
            ringBuffer.Release(taken.Length);
            peek = ringBuffer.Peek();
            Assert.IsTrue(peek.Count > 0);
            taken = ringBuffer.Take(1);
            Assert.IsTrue(taken.Length == 1);
            peek = ringBuffer.Peek();
            Assert.IsTrue(peek.Count == 0);

        }


        [Test]
        public void StreamRingBufferTest2()
        {
            Random r = new Random();
            const int capacity = 2;
            StreamRingBuffer ringBuffer = new StreamRingBuffer(capacity);

            int runs = 1000000;
            while (runs--  > 0)
            {
                ringBuffer.Peek();
                ringBuffer.Take(1);
                ringBuffer.Release(1);
            }
        }
    }
}
