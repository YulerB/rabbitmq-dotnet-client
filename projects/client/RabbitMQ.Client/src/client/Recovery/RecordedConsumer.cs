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

using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Impl
{
    public class RecordedConsumer : RecordedEntity
    {
        public RecordedConsumer(AutorecoveringModel model, string queue) : base(model)
        {
            Queue = queue;
        }

        public IDictionary<string, object> Arguments { get; set; }
        public bool AutoAck { get; set; }
        public IBasicConsumer Consumer { get; set; }
        public string ConsumerTag { get; set; }
        public BasicConsumeFlags Settings { get; set; }
        public string Queue { get; set; }

        public string Recover()
        {
            ConsumerTag = ModelDelegate.BasicConsume(Queue,
                ConsumerTag, Settings,
                Arguments, Consumer);

            return ConsumerTag;
        }

        public RecordedConsumer WithArguments(IDictionary<string, object> value)
        {
            Arguments = value;
            return this;
        }

        public RecordedConsumer WithSettings(BasicConsumeFlags value)
        {
            Settings = value;
            return this;
        }

        public RecordedConsumer WithConsumer(IBasicConsumer value)
        {
            Consumer = value;
            return this;
        }

        public RecordedConsumer WithConsumerTag(string value)
        {
            ConsumerTag = value;
            return this;
        }



        public RecordedConsumer WithQueue(string value)
        {
            Queue = value;
            return this;
        }
    }
}
