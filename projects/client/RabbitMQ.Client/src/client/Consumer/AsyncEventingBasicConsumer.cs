using System;
using System.Threading.Tasks;
using TaskExtensions = RabbitMQ.Client.Impl.TaskExtensions;

namespace RabbitMQ.Client.Events
{
    public class AsyncEventingBasicConsumer : AsyncDefaultBasicConsumer
    {
        ///<summary>Constructor which sets the Model property to the
        ///given value.</summary>
        public AsyncEventingBasicConsumer(IModel model) : base(model)
        {
        }

        ///<summary>Event fired on HandleBasicDeliver.</summary>
        public event AsyncEventHandler<BasicDeliverEventArgs> Received;

        ///<summary>Event fired on HandleBasicConsumeOk.</summary>
        public event AsyncEventHandler<ConsumerEventArgs> Registered;

        ///<summary>Event fired on HandleModelShutdown.</summary>
        public event AsyncEventHandler<ShutdownEventArgs> Shutdown;

        ///<summary>Event fired on HandleBasicCancelOk.</summary>
        public event AsyncEventHandler<ConsumerEventArgs> Unregistered;

        ///<summary>Fires the Unregistered event.</summary>
        public sealed override async Task HandleBasicCancelOk(string consumerTag)
        {
            await base.HandleBasicCancelOk(consumerTag).ConfigureAwait(false);
            await Raise(Unregistered, new ConsumerEventArgs(consumerTag)).ConfigureAwait(false);
        }

        ///<summary>Fires the Registered event.</summary>
        public sealed override async Task HandleBasicConsumeOk(string consumerTag)
        {
            await base.HandleBasicConsumeOk(consumerTag).ConfigureAwait(false);
            await Raise(Registered, new ConsumerEventArgs(consumerTag)).ConfigureAwait(false);
        }

        ///<summary>Fires the Received event.</summary>
        public sealed override async Task HandleBasicDeliver(BasicDeliverEventArgs args)
        {
            await base.HandleBasicDeliver(args).ConfigureAwait(false);
            await Raise(Received, args).ConfigureAwait(false);
        }

        ///<summary>Fires the Shutdown event.</summary>
        public sealed override async Task HandleModelShutdown(object model, ShutdownEventArgs reason)
        {
            await base.HandleModelShutdown(model, reason).ConfigureAwait(false);
            await Raise(Shutdown, reason).ConfigureAwait(false);
        }

        private Task Raise<TEvent>(AsyncEventHandler<TEvent> eventHandler, TEvent evt) 
            where TEvent : EventArgs
        {
            if (eventHandler != null)
            {
                return eventHandler(this, evt);
            }
            return TaskExtensions.CompletedTask;
        }
    }
}