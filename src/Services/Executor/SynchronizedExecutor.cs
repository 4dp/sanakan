#pragma warning disable 1591

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Shinden.Logger;

namespace Sanakan.Services.Executor
{
    public class SynchronizedExecutor : IExecutor
    {
        private const int QueueLength = 60;

        private IServiceProvider _provider;
        private ILogger _logger;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private BlockingCollection<IExecutable> _queue = new BlockingCollection<IExecutable>(QueueLength);
        private BlockingCollection<IExecutable> _hiQueue = new BlockingCollection<IExecutable>(QueueLength / 2);

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

        public Task<bool> TryAdd(IExecutable task, TimeSpan timeout)
        {
            _logger.Log($"Executor: adding new task, on pool: {_queue.Count} /hi: {_hiQueue.Count}");
            if (AddToQueue(task, timeout))
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public async Task RunWorker()
        {
            if (_queue.Count < 1 && _hiQueue.Count < 1)
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

        private bool AddToQueue(IExecutable task, TimeSpan timeout)
        {
            if (task.GetPriority() == Priority.High)
            {
                return _hiQueue.TryAdd(task, timeout);
            }
            return _queue.TryAdd(task, timeout);
        }

        private BlockingCollection<IExecutable> SelectQueue()
        {
            if (_hiQueue.Count > 0)
                return _hiQueue;

            return _queue;
        }

        private async Task ProcessCommandsAsync()
        {
            if (SelectQueue().TryTake(out var cmd, 100))
            {
                var taskName = cmd.GetName();
                try
                {
                    _logger.Log($"Executor: running {taskName}");

                    var watch = Stopwatch.StartNew();
                    await cmd.ExecuteAsync(_provider);
                    _logger.Log($"Executor: completed {taskName} in {watch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    _logger.Log($"Executor: {taskName} - {ex}");
                }

                using (var proc = Process.GetCurrentProcess())
                {
                    _logger.Log($"mem usage: {proc.WorkingSet64 / 1048576} MiB");
                }
            }
        }
    }
}
