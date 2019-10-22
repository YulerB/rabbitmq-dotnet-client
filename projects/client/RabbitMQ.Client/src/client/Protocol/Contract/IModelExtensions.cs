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

using RabbitMQ.Client.Framing.Impl;
using System.Collections.Generic;

namespace RabbitMQ.Client
{
    public static class IModelExensions
    {
        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(this IModel model,
            IBasicConsumer consumer,
            string queue,
            BasicConsumeFlags settings,
            string consumerTag = "",
            Dictionary<string, object> arguments = null)
        {
            return model.BasicConsume(queue, consumerTag, settings, arguments, consumer);
        }

        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(this IModel model, string queue, bool autoAck, IBasicConsumer consumer)
        {
            return model.BasicConsume(queue, string.Empty, autoAck ?BasicConsumeFlags.NoAck : BasicConsumeFlags.None, null, consumer);
        }

        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(this IModel model, string queue,
            bool autoAck,
            string consumerTag,
            IBasicConsumer consumer)
        {
            return model.BasicConsume(queue, consumerTag, autoAck ? BasicConsumeFlags.NoAck: BasicConsumeFlags.None, null, consumer);
        }

        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(this IModel model, string queue,
            bool autoAck,
            string consumerTag,
            Dictionary<string, object> arguments,
            IBasicConsumer consumer)
        {
            return model.BasicConsume(queue, consumerTag, autoAck ? BasicConsumeFlags.NoAck : BasicConsumeFlags.None, arguments, consumer);
        }

        /// <summary>
        /// (Extension method) Convenience overload of BasicPublish.
        /// </summary>
        /// <remarks>
        /// The publication occurs with mandatory=false and immediate=false.
        /// </remarks>
        public static void BasicPublish(this IModel model, PublicationAddress addr, RabbitMQ.Client.Impl.BasicProperties basicProperties, byte[] body)
        {
            model.BasicPublish(new BasicPublishFull(new BasicPublish(0, addr.ExchangeName, addr.RoutingKey, BasicPublishFlags.None),basicProperties, body));
        }

        /// <summary>
        /// (Extension method) Convenience overload of BasicPublish.
        /// </summary>
        /// <remarks>
        /// The publication occurs with mandatory=false
        /// </remarks>
        public static void BasicPublish(this IModel model, string exchange, string routingKey, RabbitMQ.Client.Impl.BasicProperties basicProperties, byte[] body)
        {
            model.BasicPublish(new BasicPublishFull(new BasicPublish (0, exchange, routingKey, BasicPublishFlags.None), basicProperties, body));
        }

        /// <summary>
        /// (Spec method) Convenience overload of BasicPublish.
        /// </summary>
        public static void BasicPublish(this IModel model, string exchange, string routingKey, bool mandatory = false, RabbitMQ.Client.Impl.BasicProperties basicProperties = null, byte[] body = null)
        {
            model.BasicPublish(new BasicPublishFull(new BasicPublish(0, exchange, routingKey, mandatory ? BasicPublishFlags.Mandatory : BasicPublishFlags.None), basicProperties, body));
        }

        /// <summary>
        /// (Spec method) Declare a queue.
        /// </summary>
        public static QueueDeclareOk QueueDeclare(this IModel model, string queue = "", bool durable = false, bool exclusive = true,
            bool autoDelete = true, Dictionary<string, object> arguments = null)
            {
                return model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
            }

        /// <summary>
        /// (Extension method) Bind an exchange to an exchange.
        /// </summary>
        public static void ExchangeBind(this IModel model, string destination, string source, string routingKey, Dictionary<string, object> arguments = null)
        {
            model.ExchangeBind(new ExchangeBind(0,destination, source, routingKey, false, arguments));
        }

        /// <summary>
        /// (Extension method) Like exchange bind but sets nowait to true. 
        /// </summary>
        public static void ExchangeBindNoWait(this IModel model, string destination, string source, string routingKey, Dictionary<string, object> arguments = null)
        {
            model.ExchangeBindNoWait(new ExchangeBind(0, destination, source, routingKey, true, arguments));
        }

        /// <summary>
        /// (Spec method) Declare an exchange.
        /// </summary>
        public static void ExchangeDeclare(this IModel model, string exchange, string type, bool durable = false, bool autoDelete = false,
            Dictionary<string, object> arguments = null)
            {
                model.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
            }

        /// <summary>
        /// (Extension method) Like ExchangeDeclare but sets nowait to true. 
        /// </summary>
        public static void ExchangeDeclareNoWait(this IModel model, string exchange, string type, bool durable = false, bool autoDelete = false,
            Dictionary<string, object> arguments = null)
            {
                model.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);
            }

        /// <summary>
        /// (Spec method) Unbinds an exchange.
        /// </summary>
        public static void ExchangeUnbind(this IModel model, string destination,
            string source,
            string routingKey,
            Dictionary<string, object> arguments = null)
            {
                model.ExchangeUnbind(new ExchangeUnbind(0, destination, source, routingKey, false, arguments));
            }

        /// <summary>
        /// (Spec method) Deletes an exchange.
        /// </summary>
        public static void ExchangeDelete(this IModel model, string exchange, bool ifUnused = false)
        {
            model.ExchangeDelete(exchange, ifUnused);
        }

        /// <summary>
        /// (Extension method) Like ExchangeDelete but sets nowait to true.
        /// </summary>
        public static void ExchangeDeleteNoWait(this IModel model, string exchange, bool ifUnused = false)
        {
            model.ExchangeDeleteNoWait(exchange, ifUnused);
        }

        /// <summary>
        /// (Spec method) Binds a queue.
        /// </summary>
        public static void QueueBind(this IModel model, string queue, string exchange, string routingKey, Dictionary<string, object> arguments = null)
        {
            model.QueueBind(queue, exchange, routingKey, arguments);
        }

        /// <summary>
        /// (Spec method) Deletes a queue.
        /// </summary>
        public static uint QueueDelete(this IModel model, string queue, bool ifUnused = false, bool ifEmpty = false)
        {
            return model.QueueDelete(queue, ifUnused, ifEmpty);
        }

        /// <summary>
        /// (Extension method) Like QueueDelete but sets nowait to true.
        /// </summary>
        public static void QueueDeleteNoWait(this IModel model, string queue, bool ifUnused = false, bool ifEmpty = false)
        {
            model.QueueDeleteNoWait(queue, ifUnused, ifEmpty);
        }

        /// <summary>
        /// (Spec method) Unbinds a queue.
        /// </summary>
        public static void QueueUnbind(this IModel model, string queue, string exchange, string routingKey, Dictionary<string, object> arguments = null)
        {
            model.QueueUnbind(queue, exchange, routingKey, arguments);
        }
    }
}
