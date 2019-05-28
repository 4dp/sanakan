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
        private BlockingCollection<IExecutable> _queue = new BlockingCollection<IExecutable>(50);

        private Timer _timer;
        private CancellationTokenSource _cts { get; set; }

        public SynchronizedExecutor(ILogger logger)
        {
            _logger = logger;
            _cts = new CancellationTokenSource();

            _timer = new Timer(async _ =>
            {
                await RunWorker();
            },
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(1));
        }

        public void Initialize(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<bool> TryAdd(IExecutable task, TimeSpan timeout)
        {
            if (_queue.TryAdd(task, timeout))
            {
                await Task.CompletedTask;
                return true;
            }
            return false;
        }

        public async Task RunWorker()
        {
            if (_queue.Count < 1)
                return;

            if (!await _semaphore.WaitAsync(0))
                return;

            try
            {
                _ = Task.Run(async () => await ProcessCommandsAsync()).ContinueWith(_ =>
                {
                    _cts.Cancel();
                    _cts = new CancellationTokenSource();
                });

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(90), _cts.Token);
                }
                catch (Exception) { }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessCommandsAsync()
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
            }
        }
    }
}
