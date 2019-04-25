using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public class SynchronizedExecutor : IExecutor
    {
        private IServiceProvider _provider;

        private BlockingCollection<IExecutable> _queue = new BlockingCollection<IExecutable>(100);

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;

            new Thread(ProcessCommandsAsync).Start();

            await Task.CompletedTask;
        }

        public bool TryAdd(IExecutable task) => _queue.TryAdd(task);

        private async void ProcessCommandsAsync()
        {
            while (true)
            {
                if (_queue.Count > 0)
                {
                    if (_queue.TryTake(out var cmd))
                    {
                        await cmd.ExecuteAsync(_provider);
                    }
                    await Task.Delay(10);
                }
            }
        }
    }
}
