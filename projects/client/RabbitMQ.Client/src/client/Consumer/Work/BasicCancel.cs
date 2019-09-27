using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Impl
{
    public sealed class BasicCancel : Work
    {
        private readonly string consumerTag;

        public BasicCancel(IBasicConsumer consumer, string consumerTag) : base(consumer)
        {
            this.consumerTag = consumerTag;
        }

        protected override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleBasicCancel(consumerTag).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, new Dictionary<string, object>
                {
                    {"consumer", consumer},
                    {"context",  "HandleBasicCancel"}
                }));
            }
        }
    }
}