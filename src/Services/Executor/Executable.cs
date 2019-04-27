#pragma warning disable 1591

using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public class Executable : IExecutable
    {
        private Task<bool> _task { get; set; }

        public Executable(Task<bool> task) => _task = task;

        public async Task<bool> ExecuteAsync(IServiceProvider provider)
        {
            _task.Start();

            await Task.CompletedTask;

            return _task.Result;
        }
    }
}
