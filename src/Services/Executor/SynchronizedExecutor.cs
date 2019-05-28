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

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private BlockingCollection<IExecutable> _queue = new BlockingCollection<IExecutable>(100);

        private Timer _timer;
        private Task _runningTask { get; set; }
        private CancellationTokenSource _cts { get; set; }

        public SynchronizedExecutor(ILogger logger)
        {
            _cts = new CancellationTokenSource();
            _runningTask = null;
            _logger = logger;

            _timer = new Timer(_ =>
            {
                RunWorker();
            },
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(5));
        }

        public void Initialize(IServiceProvider provider)
        {
            _provider = provider;
        }

        public bool TryAdd(IExecutable task, TimeSpan timeout)
        {
            _logger.Log($"Executor: qc {_queue.Count}");
            if (_queue.TryAdd(task, timeout))
            {
                RunWorker();
                return true;
            }
            return false;
        }

        public void RunWorker()
        {
            if (_runningTask == null)
            {
                if (_queue.Count > 0)
                {
                    _runningTask = Task.Run(async () => await ProcessCommandsAsync(), _cts.Token).ContinueWith(_ =>
                    {
                        _runningTask = null;
                        _logger.Log($"Executor: Task canceled!");
                    });

                    _ = Task.Delay(TimeSpan.FromSeconds(120)).ContinueWith(_ =>
                    {
                        _logger.Log($"Executor: canceling task!");

                        _cts.Cancel();
                        _cts = new CancellationTokenSource();
                    });
                }
            }
        }

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
                        await Task.Delay(30);
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
