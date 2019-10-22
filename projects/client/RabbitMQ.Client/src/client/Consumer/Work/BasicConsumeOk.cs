using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Impl
{
    sealed class BasicConsumeOk : Work
    {
        readonly string consumerTag;

        public BasicConsumeOk(IBasicConsumer consumer, string consumerTag) : base(consumer)
        {
            this.consumerTag = consumerTag;
        }

        protected sealed override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleBasicConsumeOk(consumerTag).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, new Dictionary<string, object>()
                {
                    {"consumer", consumer},
                    {"context",  "HandleBasicConsumeOk"}
                }));
            }
        }
    }
}