﻿using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    internal class ConcurrentConsumerDispatcher : IConsumerDispatcher
    {
        private ModelBase model;
        private readonly ConsumerWorkService workService;

        public ConcurrentConsumerDispatcher(ModelBase model, ConsumerWorkService ws)
        {
            this.model = model;
            this.workService = ws;
            this.IsShutdown = false;
        }

        public void Quiesce()
        {
            IsShutdown = true;
        }

        public void Shutdown()
        {
            this.workService.StopWork();
        }

        public void Shutdown(IModel model)
        {
            this.workService.StopWork(model);
        }

        public bool IsShutdown
        {
            get;
            private set;
        }

        public void HandleBasicConsumeOk(IBasicConsumer consumer,
                                         string consumerTag)
        {
            UnlessShuttingDown(() =>
            {
                try
                {
                    consumer.HandleBasicConsumeOk(consumerTag);
                }
                catch (Exception e)
                {
                    model.OnCallbackException(CallbackExceptionEventArgs.Build(e, new Dictionary<string, object>()
                    {
                        {"consumer", consumer},
                        {"context",  "HandleBasicConsumeOk"}
                    }));
                }
            });
        }

        public void HandleBasicDeliver(IBasicConsumer consumer,
                                       BasicDeliverEventArgs args)
        {
            UnlessShuttingDown(() =>
            {
                try
                {
                    consumer.HandleBasicDeliver(args);
                }
                catch (Exception e)
                {
                    model.OnCallbackException(CallbackExceptionEventArgs.Build(e, new Dictionary<string, object>()
                    {
                        {"consumer", consumer},
                        {"context",  "HandleBasicDeliver"}
                    }));
                }
            });
        }

        public void HandleBasicCancelOk(IBasicConsumer consumer, string consumerTag)
        {
            UnlessShuttingDown(() =>
            {
                try
                {
                    consumer.HandleBasicCancelOk(consumerTag);
                }
                catch (Exception e)
                {
                    model.OnCallbackException(CallbackExceptionEventArgs.Build(e, new Dictionary<string, object>()
                    {
                        {"consumer", consumer},
                        {"context",  "HandleBasicCancelOk"}
                    }));
                }
            });
        }

        public void HandleBasicCancel(IBasicConsumer consumer, string consumerTag)
        {
            UnlessShuttingDown(() =>
            {
                try
                {
                    consumer.HandleBasicCancel(consumerTag);
                }
                catch (Exception e)
                {
                    model.OnCallbackException(CallbackExceptionEventArgs.Build(e, new Dictionary<string, object>()
                    {
                        {"consumer", consumer},
                        {"context",  "HandleBasicCancel"}
                    }));
                }
            });
        }

        public void HandleModelShutdown(IBasicConsumer consumer, ShutdownEventArgs reason)
        {
            // the only case where we ignore the shutdown flag.
            try
            {
                consumer.HandleModelShutdown(model, reason);
            }
            catch (Exception e)
            {
                var details = new Dictionary<string, object>()
                    {
                        {"consumer", consumer},
                        {"context",  "HandleModelShutdown"}
                    };
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, details));
            };
        }

        private void UnlessShuttingDown(Action fn)
        {
            if (!this.IsShutdown)
            {
                Execute(fn);
            }
        }

        private void Execute(Action fn)
        {
            this.workService.AddWork(this.model, fn);
        }
    }
}
