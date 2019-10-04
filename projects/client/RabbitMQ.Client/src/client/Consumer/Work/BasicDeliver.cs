using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Impl
{
    sealed class BasicDeliver : Work
    {
        BasicDeliverEventArgs args;

        public BasicDeliver(IBasicConsumer consumer,
            BasicDeliverEventArgs args) : base(consumer)
        {
            this.args = args;
        }

        protected override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleBasicDeliver(args).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, new Dictionary<string, object>()
                {
                    {"consumer", consumer},
                    {"context",  "HandleBasicDeliver"}
                }));
            }
        }
    }
}