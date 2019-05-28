#pragma warning disable 1591

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Shinden.Logger;

namespace Sanakan.Services.Executor
{
    public class SynchronizedExecutor : IExecutor
    {
        private IServiceProvider _provider;
        private ILogger _logger;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);
        private BlockingCollection<IExecutable> _queue = new BlockingCollection<IExecutable>(150);

        private CancellationTokenSource _cts { get; set; }

        public SynchronizedExecutor(ILogger logger)
        {
            _cts = new CancellationTokenSource();
            _logger = logger;
        }
        
        public void Initialize(IServiceProvider provider)
        {
            _provider = provider;
            RunWorker();
        }

        public bool TryAdd(IExecutable task, TimeSpan timeout) 
        {
            _logger.Log($"Executor: qc {_queue.Count}");
            if (_queue.TryAdd(task, timeout))
            {
                RunWorker();
                return true;
            }
            else
            {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                RunWorker();
            }
            return false;
        }

        public void RunWorker() => _ = Task.Run(async () => await ProcessCommandsAsync(), _cts.Token);

        private async Task ProcessCommandsAsync()
        {
            if (!await _semaphore.WaitAsync(0))
                return;

            try
            {
                while (_queue.Count > 0)
                {
                    if (_queue.TryTake(out var cmd, 100))
                    {
                        try
                        {
                            await cmd.ExecuteAsync(_provider);
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"Executor: {ex}");
                        }
                        await Task.Delay(50);
                    }
                    else
                    {
                        _logger.Log($"Executor: cannot take task!");
                        await Task.Delay(10);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
