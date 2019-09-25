﻿// Autogenerated code. Do not edit.

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
//   The contents of this file are subject to the Mozilla Public License
//   Version 1.1 (the "License"); you may not use this file except in
//   compliance with the License. You may obtain a copy of the License at
//   http://www.rabbitmq.com/mpl.html
//
//   Software distributed under the License is distributed on an "AS IS"
//   basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//   License for the specific language governing rights and limitations
//   under the License.
//
//   The Original Code is RabbitMQ.
//
//   The Initial Developer of the Original Code is Pivotal Software, Inc.
//   Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using RabbitMQ.Client.Exceptions;
namespace RabbitMQ.Client.Framing.Impl
{
    using RabbitMQ.Client.Framing;

    public class Model : RabbitMQ.Client.Impl.ModelBase
    {
        public Model(RabbitMQ.Client.Impl.ISession session) : base(session) { }
        public Model(RabbitMQ.Client.Impl.ISession session, RabbitMQ.Client.ConsumerWorkService workService) : base(session, workService) { }
        public override void ConnectionTuneOk(
          ushort @channelMax,
          uint @frameMax,
          ushort @heartbeat)
        {
            ModelSend(new ConnectionTuneOk(@channelMax,@frameMax,@heartbeat), null, null);
        }
        public override void _Private_BasicCancel(
          string @consumerTag,
          bool @nowait)
        {
            ModelSend(new BasicCancel(consumerTag, nowait), null, null);
        }
        public override void _Private_BasicConsume(
          string @queue,
          string @consumerTag,
          BasicConsumeFlags settings ,
          System.Collections.Generic.IDictionary<string, object> @arguments)
        {
            ModelSend(new BasicConsume(0, queue, consumerTag, settings, arguments), null, null);
        }
        public override void _Private_BasicGet(
          string @queue,
          bool @autoAck)
        {
            ModelSend(new BasicGet(0, queue, autoAck), null, null);
        }
        public override void _Private_BasicPublish(
          string @exchange,
          string @routingKey,
          bool @mandatory,
          RabbitMQ.Client.IBasicProperties @basicProperties,
          byte[] @body)
        {
            ModelSend(new BasicPublish(0, exchange, routingKey, mandatory, false), (BasicProperties)basicProperties, body);
        }
        public override void _Private_BasicRecover(
          bool @requeue)
        {
            ModelSend(new BasicRecover(requeue), null, null);
        }
        public override void _Private_ChannelClose(
          ushort @replyCode,
          string @replyText,
          ushort @classId,
          ushort @methodId)
        {
            ModelSend(new ChannelClose(replyCode, replyText, classId, methodId), null, null);
        }
        public override void _Private_ChannelCloseOk()
        {
            ModelSend(new ChannelCloseOk(), null, null);
        }
        public override void _Private_ChannelFlowOk(bool @active)
        {
            ModelSend(new ChannelFlowOk(active), null, null);
        }
        public override void _Private_ChannelOpen(
          string @outOfBand)
        {
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(new ChannelOpen(outOfBand), null, null);
            ChannelOpenOk __rep = __repBase as ChannelOpenOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_ConfirmSelect(
          bool @nowait)
        {
            ConfirmSelect __req = new ConfirmSelect(nowait);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            ConfirmSelectOk __rep = __repBase as ConfirmSelectOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_ConnectionClose(
          ushort @replyCode,
          string @replyText,
          ushort @classId,
          ushort @methodId)
        {
            ConnectionClose __req = new ConnectionClose(replyCode, replyText, classId, methodId);
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            ConnectionCloseOk __rep = __repBase as ConnectionCloseOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_ConnectionCloseOk()
        {
            ModelSend(new ConnectionCloseOk(), null, null);
        }
        public override void _Private_ConnectionOpen(
          string @virtualHost,
          string @capabilities,
          bool @insist)
        {
            ModelSend(new ConnectionOpen(@virtualHost, @capabilities, @insist), null, null);
        }
        public override void _Private_ConnectionSecureOk(
          string @response)
        {
            ModelSend(new ConnectionSecureOk(response), null, null);
        }
        public override void _Private_ConnectionStartOk(
          System.Collections.Generic.IDictionary<string, object> @clientProperties,
          string @mechanism,
          string @response,
          string @locale)
        {
            ModelSend(new ConnectionStartOk(clientProperties, mechanism, response, locale), null, null);
        }
        public override void _Private_ExchangeBind(
          string @destination,
          string @source,
          string @routingKey,
          bool @nowait,
          System.Collections.Generic.IDictionary<string, object> @arguments)
        {
            ExchangeBind __req = new ExchangeBind(0, destination, source, routingKey, nowait, arguments);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            ExchangeBindOk __rep = __repBase as ExchangeBindOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_ExchangeDeclare(
          string @exchange,
          string @type,
          bool @passive,
          bool @durable,
          bool @autoDelete,
          bool @internal,
          bool @nowait,
          System.Collections.Generic.IDictionary<string, object> @arguments)
        {
            ExchangeDeclare __req = new ExchangeDeclare(0, exchange, type, passive, durable, autoDelete,@internal, nowait, arguments);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            ExchangeDeclareOk __rep = __repBase as ExchangeDeclareOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_ExchangeDelete(
          string @exchange,
          bool @ifUnused,
          bool @nowait)
        {
            ExchangeDelete __req = new ExchangeDelete(0, exchange, ifUnused, nowait);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            ExchangeDeleteOk __rep = __repBase as ExchangeDeleteOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_ExchangeUnbind(
          string @destination,
          string @source,
          string @routingKey,
          bool @nowait,
          System.Collections.Generic.IDictionary<string, object> @arguments)
        {
            ExchangeUnbind __req = new ExchangeUnbind(0, destination, source, routingKey, nowait, arguments);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            ExchangeUnbindOk __rep = __repBase as ExchangeUnbindOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_QueueBind(
          string @queue,
          string @exchange,
          string @routingKey,
          bool @nowait,
          System.Collections.Generic.IDictionary<string, object> @arguments)
        {
            QueueBind __req = new QueueBind(0, queue, exchange, routingKey, nowait, arguments);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            QueueBindOk __rep = __repBase as QueueBindOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void _Private_QueueDeclare(
          string @queue,
          bool @passive,
          bool @durable,
          bool @exclusive,
          bool @autoDelete,
          bool @nowait,
          System.Collections.Generic.IDictionary<string, object> @arguments)
        {
            ModelSend(new QueueDeclare(0, @queue, @passive, @durable, @exclusive, @autoDelete, @nowait, @arguments), null, null);
        }
        public override uint _Private_QueueDelete(
          string @queue,
          bool @ifUnused,
          bool @ifEmpty,
          bool @nowait)
        {
            QueueDelete __req = new QueueDelete(0,@queue,@ifUnused,@ifEmpty,@nowait);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return 0xFFFFFFFF;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            QueueDeleteOk __rep = __repBase as QueueDeleteOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
            return __rep.m_messageCount;
        }
        public override uint _Private_QueuePurge(
          string @queue,
          bool @nowait)
        {
            QueuePurge __req = new QueuePurge(0, @queue, @nowait);
            if (nowait)
            {
                ModelSend(__req, null, null);
                return 0xFFFFFFFF;
            }
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            QueuePurgeOk __rep = __repBase as QueuePurgeOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
            return __rep.m_messageCount;
        }
        public override void BasicAck(
          ulong @deliveryTag,
          bool @multiple)
        {
            ModelSend(new BasicAck(deliveryTag, multiple), null, null);
        }
        public override void BasicNack(
          ulong @deliveryTag,
          bool @multiple,
          bool @requeue)
        {
            ModelSend(new BasicNack(deliveryTag, multiple, requeue), null, null);
        }
        public override void BasicQos(
          uint @prefetchSize,
          ushort @prefetchCount,
          bool @global)
        {
            BasicQos __req = new BasicQos();
            __req.m_prefetchSize = @prefetchSize;
            __req.m_prefetchCount = @prefetchCount;
            __req.m_global = @global;
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            BasicQosOk __rep = __repBase as BasicQosOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void BasicRecoverAsync(
          bool @requeue)
        {
            ModelSend(new BasicRecoverAsync(@requeue), null, null);
        }
        public override void BasicReject(
          ulong @deliveryTag,
          bool @requeue)
        {
            ModelSend(new BasicReject(deliveryTag, requeue), null, null);
        }
        public override RabbitMQ.Client.IBasicProperties CreateBasicProperties()
        {
            return new BasicProperties();
        }
        public override void QueueUnbind(
          string @queue,
          string @exchange,
          string @routingKey,
          System.Collections.Generic.IDictionary<string, object> @arguments)
        {
            QueueUnbind __req = new QueueUnbind(0,@queue,@exchange,@routingKey,@arguments);
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            QueueUnbindOk __rep = __repBase as QueueUnbindOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void TxCommit()
        {
            TxCommit __req = new TxCommit();
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            TxCommitOk __rep = __repBase as TxCommitOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void TxRollback()
        {
            TxRollback __req = new TxRollback();
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            TxRollbackOk __rep = __repBase as TxRollbackOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override void TxSelect()
        {
            TxSelect __req = new TxSelect();
            RabbitMQ.Client.Impl.MethodBase __repBase = ModelRpc(__req, null, null);
            TxSelectOk __rep = __repBase as TxSelectOk;
            if (__rep == null) throw new UnexpectedMethodException(__repBase);
        }
        public override bool DispatchAsynchronous(RabbitMQ.Client.Impl.Command cmd)
        {
            RabbitMQ.Client.Impl.MethodBase __method = (RabbitMQ.Client.Impl.MethodBase)cmd.Method;
            switch ((__method.ProtocolClassId << 16) | __method.ProtocolMethodId)
            {
                case 3932240:
                    {
                        BasicAck __impl = (BasicAck)__method;
                        HandleBasicAck(__impl.m_deliveryTag,__impl.m_multiple);
                        return true;
                    }
                case 3932190:
                    {
                        BasicCancel __impl = (BasicCancel)__method;
                        HandleBasicCancel(
                          __impl.m_consumerTag,
                          __impl.m_nowait);
                        return true;
                    }
                case 3932191:
                    {
                        BasicCancelOk __impl = (BasicCancelOk)__method;
                        HandleBasicCancelOk(
                          __impl.m_consumerTag);
                        return true;
                    }
                case 3932181:
                    {
                        BasicConsumeOk __impl = (BasicConsumeOk)__method;
                        HandleBasicConsumeOk(
                          __impl.m_consumerTag);
                        return true;
                    }
                case 3932220:
                    {
                        BasicDeliver __impl = (BasicDeliver)__method;
                        HandleBasicDeliver(
                          __impl.m_consumerTag,
                          __impl.m_deliveryTag,
                          __impl.m_redelivered,
                          __impl.m_exchange,
                          __impl.m_routingKey,
                          (RabbitMQ.Client.IBasicProperties)cmd.Header,
                          cmd.Body);
                        return true;
                    }
                case 3932232:
                    {
                        HandleBasicGetEmpty();
                        return true;
                    }
                case 3932231:
                    {
                        BasicGetOk __impl = (BasicGetOk)__method;
                        HandleBasicGetOk(
                          __impl.m_deliveryTag,
                          __impl.m_redelivered,
                          __impl.m_exchange,
                          __impl.m_routingKey,
                          __impl.m_messageCount,
                          (RabbitMQ.Client.IBasicProperties)cmd.Header,
                          cmd.Body);
                        return true;
                    }
                case 3932280:
                    {
                        BasicNack __impl = (BasicNack)__method;
                        HandleBasicNack(
                          __impl.m_deliveryTag,
                          __impl.m_multiple,
                          __impl.m_requeue);
                        return true;
                    }
                case 3932271:
                    {
                        HandleBasicRecoverOk();
                        return true;
                    }
                case 3932210:
                    {
                        BasicReturn __impl = (BasicReturn)__method;
                        HandleBasicReturn(
                          __impl.m_replyCode,
                          __impl.m_replyText,
                          __impl.m_exchange,
                          __impl.m_routingKey,
                          (RabbitMQ.Client.IBasicProperties)cmd.Header,
                          cmd.Body);
                        return true;
                    }
                case 1310760:
                    {
                        ChannelClose __impl = (ChannelClose)__method;
                        HandleChannelClose(
                          __impl.m_replyCode,
                          __impl.m_replyText,
                          __impl.m_classId,
                          __impl.m_methodId);
                        return true;
                    }
                case 1310761:
                    {
                        HandleChannelCloseOk();
                        return true;
                    }
                case 1310740:
                    {
                        ChannelFlow __impl = (ChannelFlow)__method;
                        HandleChannelFlow(
                          __impl.m_active);
                        return true;
                    }
                case 655420:
                    {
                        ConnectionBlocked __impl = (ConnectionBlocked)__method;
                        HandleConnectionBlocked(
                          __impl.m_reason);
                        return true;
                    }
                case 655410:
                    {
                        ConnectionClose __impl = (ConnectionClose)__method;
                        HandleConnectionClose(
                          __impl.m_replyCode,
                          __impl.m_replyText,
                          __impl.m_classId,
                          __impl.m_methodId);
                        return true;
                    }
                case 655401:
                    {
                        ConnectionOpenOk __impl = (ConnectionOpenOk)__method;
                        HandleConnectionOpenOk(
                          __impl.m_reserved1);
                        return true;
                    }
                case 655380:
                    {
                        ConnectionSecure __impl = (ConnectionSecure)__method;
                        HandleConnectionSecure(
                          __impl.m_challenge);
                        return true;
                    }
                case 655370:
                    {
                        ConnectionStart __impl = (ConnectionStart)__method;
                        HandleConnectionStart(
                          __impl.m_versionMajor,
                          __impl.m_versionMinor,
                          __impl.m_serverProperties,
                          __impl.m_mechanisms,
                          __impl.m_locales);
                        return true;
                    }
                case 655390:
                    {
                        ConnectionTune __impl = (ConnectionTune)__method;
                        HandleConnectionTune(
                          __impl.m_channelMax,
                          __impl.m_frameMax,
                          __impl.m_heartbeat);
                        return true;
                    }
                case 655421:
                    {
                        HandleConnectionUnblocked();
                        return true;
                    }
                case 3276811:
                    {
                        QueueDeclareOk __impl = (QueueDeclareOk)__method;
                        HandleQueueDeclareOk(
                          __impl.m_queue,
                          __impl.m_messageCount,
                          __impl.m_consumerCount);
                        return true;
                    }
                default: return false;
            }
        }
    }
}