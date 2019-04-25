#pragma warning disable 1591

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public class SynchronizedExecutor : IExecutor
    {
        private IServiceProvider _provider;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);
        private BlockingCollection<IExecutable> _queue = new BlockingCollection<IExecutable>(100);

        public void Initialize(IServiceProvider provider)
        {
            _provider = provider;
            RunWorker();
        }

        public bool TryAdd(IExecutable task) 
        {
            if (_queue.TryAdd(task))
            {
                RunWorker();
                return true;
            }
            return false;
        }

        private void RunWorker() => _ = Task.Run(async () => await ProcessCommandsAsync());

        private async Task ProcessCommandsAsync()
        {
            await _semaphore.WaitAsync(0);
            try
            {
                while (_queue.Count > 0)
                {
                    if (_queue.TryTake(out var cmd))
                    {
                        await cmd.ExecuteAsync(_provider);
                    }
                    await Task.Delay(10);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
