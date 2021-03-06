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
//  Copyright (c) 2013-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using RabbitMQ.Util;
using NUnit.Framework;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestIntAllocator
    {
        [Test]
        public void TestRandomAllocation()
        {
            int repeatCount = 10000;
            ushort range = 100;
            IList<ushort> allocated = new List<ushort>();
            UIntAllocator intAllocator = new UIntAllocator(0, range);
            Random rand = new Random();
            while (repeatCount-- > 0)
            {
                if (rand.Next(2) == 0)
                {
                    ushort? a = intAllocator.Allocate();
                    if (a.HasValue)
                    {
                        Assert.False(allocated.Contains(a.Value));
                        allocated.Add(a.Value);
                    }
                }
                else if (allocated.Count > 0)
                {
                    ushort a = allocated[0];
                    intAllocator.Free(a);
                    allocated.RemoveAt(0);
                }
            }
        }

        [Test]
        public void TestAllocateAll()
        {
            ushort range = 100;
            IList<ushort> allocated = new List<ushort>();
            UIntAllocator intAllocator = new UIntAllocator(0, range);
            for (int i=0; i <= range; i++)
            {
                ushort? a = intAllocator.Allocate();
                Assert.AreNotEqual(default(ushort?), a);
                Assert.False(allocated.Contains(a.Value));
                allocated.Add(a.Value);
            }
        }
    }
}

