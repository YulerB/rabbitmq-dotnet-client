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
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Impl
{
    public class RecoveryAwareModel : Model, IFullModel, IRecoverable
    {
        public RecoveryAwareModel(ISession session) : base(session)
        {
            ActiveDeliveryTagOffset = 0;
            MaxSeenDeliveryTag = 0;
        }

        public ulong ActiveDeliveryTagOffset { get; private set; }
        public ulong MaxSeenDeliveryTag { get; private set; }

        public void InheritOffsetFrom(RecoveryAwareModel other)
        {
            ActiveDeliveryTagOffset = other.ActiveDeliveryTagOffset + other.MaxSeenDeliveryTag;
            MaxSeenDeliveryTag = 0;
        }

        public override void HandleBasicGetOk(BasicGetResult args)
        {
            if (args.DeliveryTag > MaxSeenDeliveryTag)
            {
                MaxSeenDeliveryTag = args.DeliveryTag;
            }

            args.OffsetDeliveryTag(ActiveDeliveryTagOffset);

            base.HandleBasicGetOk(
                args);
        }

        public override void HandleBasicDeliver(BasicDeliverEventArgs args)
        {
            if (args.DeliveryTag > MaxSeenDeliveryTag)
            {
                MaxSeenDeliveryTag = args.DeliveryTag;
            }
            args.OffsetDeliveryTag(ActiveDeliveryTagOffset);
            base.HandleBasicDeliver(args);
        }

        public override void BasicAck(ulong deliveryTag,
            bool multiple)
        {
            ulong realTag = deliveryTag - ActiveDeliveryTagOffset;
            if (realTag > 0 && realTag <= deliveryTag)
            {
                base.BasicAck(realTag, multiple);
            }
        }

        public override void BasicNack(ulong deliveryTag, BasicNackFlags settings)
        {
            ulong realTag = deliveryTag - ActiveDeliveryTagOffset;
            if (realTag > 0 && realTag <= deliveryTag)
            {
                base.BasicNack(realTag, settings);
            }
        }

        public override void BasicReject(ulong deliveryTag,
            bool requeue)
        {
            ulong realTag = deliveryTag - ActiveDeliveryTagOffset;
            if (realTag > 0 && realTag <= deliveryTag)
            {
                base.BasicReject(realTag, requeue);
            }
        }

    }
}
