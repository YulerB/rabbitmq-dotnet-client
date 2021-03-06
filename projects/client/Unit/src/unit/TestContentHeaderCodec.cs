//// This source code is dual-licensed under the Apache License, version
//// 2.0, and the Mozilla Public License, version 1.1.
////
//// The APL v2.0:
////
////---------------------------------------------------------------------------
////   Copyright (c) 2007-2016 Pivotal Software, Inc.
////
////   Licensed under the Apache License, Version 2.0 (the "License");
////   you may not use this file except in compliance with the License.
////   You may obtain a copy of the License at
////
////       http://www.apache.org/licenses/LICENSE-2.0
////
////   Unless required by applicable law or agreed to in writing, software
////   distributed under the License is distributed on an "AS IS" BASIS,
////   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
////   See the License for the specific language governing permissions and
////   limitations under the License.
////---------------------------------------------------------------------------
////
//// The MPL v1.1:
////
////---------------------------------------------------------------------------
////  The contents of this file are subject to the Mozilla Public License
////  Version 1.1 (the "License"); you may not use this file except in
////  compliance with the License. You may obtain a copy of the License
////  at http://www.mozilla.org/MPL/
////
////  Software distributed under the License is distributed on an "AS IS"
////  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
////  the License for the specific language governing rights and
////  limitations under the License.
////
////  The Original Code is RabbitMQ.
////
////  The Initial Developer of the Original Code is Pivotal Software, Inc.
////  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
////---------------------------------------------------------------------------

//using NUnit.Framework;

//using System;
//using System.IO;
//using System.Text;
//using System.Collections;

//using RabbitMQ.Client;
//using RabbitMQ.Client.Impl;
//using RabbitMQ.Util;
//using System.Collections.Generic;

//namespace RabbitMQ.Client.Unit
//{
//    [TestFixture]
//    public class TestContentHeaderCodec
//    {
//        //public static ContentHeaderPropertyWriter2 Writer()
//        //{
//        //    return new ContentHeaderPropertyWriter2(new NetworkBinaryWriter(new ArraySegmentStream()));
//        //}

//        //public byte[] Contents(ContentHeaderPropertyWriter2 w)
//        //{
//        //    return ((MemoryStream)w.BaseWriter.BaseStream).ToArray();
//        //}

//        public void Check(ContentHeaderPropertyWriter2 w, byte[] expected)
//        {
//            byte[] actual = Contents(w);
//            try
//            {
//                Assert.AreEqual(expected, actual);
//            }
//            catch
//            {
//                Console.WriteLine();
//                Console.WriteLine("EXPECTED ==================================================");
//                DebugUtil.Dump(expected, Console.Out);
//                Console.WriteLine("ACTUAL ====================================================");
//                DebugUtil.Dump(actual, Console.Out);
//                Console.WriteLine("===========================================================");
//                throw;
//            }
//        }

//        public ContentHeaderPropertyWriter2 m_w;

//        [SetUp]
//        public void SetUp()
//        {
//            m_w = Writer();
//        }

//        [Test]
//        public void TestPresence()
//        {
//            m_w.WriteBits(new bool[] { false, true, false, true });
//            Check(m_w, new byte[] { 0x50, 0x00 });
//        }

//        [Test]
//        public void TestLongPresence()
//        {
//            var bits = new List<bool> { false, true, false, true };
//            for (int i = 0; i < 20; i++)
//            {
//                bits.Add(false);
//            }
//            bits.Add(true);
//            m_w.WriteBits(bits.ToArray());
//            Check(m_w, new byte[] { 0x50, 0x01, 0x00, 0x40 });
//        }

//        [Test]
//        public void TestNoPresence()
//        {
//            m_w.WriteBits(new bool[] { });
//            Check(m_w, new byte[] { 0x00, 0x00 });
//        }

//        [Test]
//        public void TestBodyLength()
//        {
//            RabbitMQ.Client.Framing.BasicProperties prop =
//                new RabbitMQ.Client.Framing.BasicProperties();
//            prop.WriteTo(m_w.BaseWriter, 0x123456789ABCDEF0UL);
//            Check(m_w, new byte[] { 0x00, 0x00, // weight
//			          0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, // body len
//			          0x00, 0x00}); // props flags
//        }

//        [Test]
//        public void TestSimpleProperties()
//        {
//            RabbitMQ.Client.Framing.BasicProperties prop =
//                new RabbitMQ.Client.Framing.BasicProperties();
//            prop.ContentType = "text/plain";
//            prop.WriteTo(m_w.BaseWriter, 0x123456789ABCDEF0UL);
//            Check(m_w, new byte[] { 0x00, 0x00, // weight
//			          0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, // body len
//			          0x80, 0x00, // props flags
//			          0x0A, // shortstr len
//			          0x74, 0x65, 0x78, 0x74,
//			          0x2F, 0x70, 0x6C, 0x61,
//			          0x69, 0x6E });
//        }
//    }
//}
