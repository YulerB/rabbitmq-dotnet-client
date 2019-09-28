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
using System.Collections;
using System.Collections.Generic;
using System.Text;

using RabbitMQ.Util;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestFieldTableFormattingGeneric 
    {
        [Test]
        public void TestStandardTypesE()
        {
            FrameBuilder writerStream = new FrameBuilder();

            IDictionary<string, object> t2 = new Dictionary<string, object>();
            t2["test"] = "test";

            IDictionary<string, object> t = new Dictionary<string, object>
            {
                ["string"] = "Hello",
                ["int"] = 1234,
                ["decimal"] = 12.34m,
                ["timestamp"] = new AmqpTimestamp(0),
                ["fieldtable"] = t2,
                ["fieldarray"] = new List<object>
                {
                    "longstring",
                    1234
                }
            };
            writerStream.WriteTable(t);

            using (ArraySegmentSequence readerSequence = new ArraySegmentSequence(writerStream.ToData()))
            {
                IDictionary<string, object> nt = readerSequence.ReadTable(out long read);

                Assert.AreEqual(Encoding.UTF8.GetBytes("Hello"), nt["string"]);
                Assert.AreEqual(1234, nt["int"]);
                Assert.AreEqual(12.34m, nt["decimal"]);
                Assert.AreEqual(0, ((AmqpTimestamp)nt["timestamp"]).UnixTime);
                IDictionary<string, object> nt2 = (IDictionary<string, object>)nt["fieldtable"];
                Assert.AreEqual(Encoding.UTF8.GetBytes("test"), nt2["test"]);
                IList<object> narray = (IList<object>)nt["fieldarray"];
                Assert.AreEqual(Encoding.UTF8.GetBytes("longstring"), narray[0]);
                Assert.AreEqual(1234, narray[1]);
            }
        }
    }
}
