using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client
{
    internal class AsyncConsumerWorkService : ConsumerWorkService , IDisposable
    {
        private ConcurrentDictionary<IModel, WorkPool> workPools = new ConcurrentDictionary<IModel, WorkPool>();

        public void Schedule<TWork>(ModelBase model, TWork work)
            where TWork : Work
        {
            // two step approach is taken, as TryGetValue does not aquire locks
            // if this fails, GetOrAdd is called, which takes a lock
            if (!workPools.TryGetValue(model, out WorkPool workPool))
            {
                var newWorkPool = new WorkPool(model);
                workPool = workPools.GetOrAdd(model, newWorkPool);

                // start if it's only the workpool that has been just created
                if (newWorkPool == workPool)
                {
                    newWorkPool.Start();
                }
            }

            workPool.Enqueue(work);
        }

        public async Task Stop(IModel model)
        {
            if (workPools.TryRemove(model, out WorkPool workPool))
            {
                await workPool.Stop().ConfigureAwait(false);
            }
        }

        public async Task Stop()
        {
            foreach (var model in workPools.Keys)
            {
                await Stop(model).ConfigureAwait(false);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var item in workPools.Values)
                    {
                        item.Dispose();
                    }
                }

                workPools = null;
                disposedValue = true;
            }
        }

        // TODO: sealed override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AsyncConsumerWorkService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
    public class TestWorkPool : IDisposable
    {
        private BlockingCollection<Work> workQueue = new BlockingCollection<Work>();
        private CancellationTokenSource tokenSource;
        private ModelBase model;
        private Task task;
        private readonly Func<Task> loop;

        public TestWorkPool(ModelBase model)
        {
            this.model = model;
            loop = Loop;
        }

        public void Start()
        {
            tokenSource = new CancellationTokenSource();
            task = Task.Run(loop, tokenSource.Token);
        }

        public void Enqueue(Work work)
        {
            workQueue.Add(work);
        }

        private async Task Loop()
        {
            foreach (var work in workQueue.GetConsumingEnumerable(tokenSource.Token))
            {
                await work.Execute(model).ConfigureAwait(false);
            }
        }

        public Task Stop()
        {
            tokenSource.Cancel();
            return task;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    workQueue?.Dispose();
                    tokenSource?.Dispose();
                }
                workQueue=null;
                task = null;
                model = null;
                disposedValue = true;
            }
        }

        // TODO: sealed override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WorkPool() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
    public class WorkPool : IDisposable
    {
        ConcurrentQueue<Work> workQueue;
        readonly TimeSpan waitTime;
        CancellationTokenSource tokenSource;
        ModelBase model;
        TaskCompletionSource<bool> messageArrived;
        private Task task;
        private readonly Func<Task> loop;
        public WorkPool(ModelBase model)
        {
            this.model = model;
            workQueue = new ConcurrentQueue<Work>();
            messageArrived = new TaskCompletionSource<bool>();
            waitTime = TimeSpan.FromMilliseconds(100);
            tokenSource = new CancellationTokenSource();
            loop = Loop;
        }

        public void Start()
        {
            task = Task.Run(loop, CancellationToken.None);
        }

        public void Enqueue(Work work)
        {
            workQueue.Enqueue(work);
            messageArrived.TrySetResult(true);
        }

        async Task Loop()
        {
            while (!tokenSource.IsCancellationRequested)
            {
                while (workQueue.TryDequeue(out Work work))
                {
                    await work.Execute(model).ConfigureAwait(false);
                }

                await Task.WhenAny(new Task[]{
                    Task.Delay(waitTime, tokenSource.Token),
                    messageArrived.Task
                }).ConfigureAwait(false);

                messageArrived.TrySetResult(true);
                messageArrived = new TaskCompletionSource<bool>();
            }
        }

        public Task Stop()
        {
            tokenSource.Cancel();
            return task;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.tokenSource.Dispose(); 
                    // TODO: dispose managed state (managed objects).
                }

                this.tokenSource = null;
                this.messageArrived = null;
                this.task = null;
                this.model = null;
                // TODO: free unmanaged resources (unmanaged objects) and sealed override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }

        }

        // TODO: sealed override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WorkPool() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}