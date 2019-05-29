#pragma warning disable 1591

using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public class Executable : IExecutable
    {
        private Task _task { get; set; }
        private readonly string _name;

        public Executable(string name, Task task)
        {
            _name = name;
            _task = task;
        }

        public string GetName() => _name;

        public async Task<bool> ExecuteAsync(IServiceProvider provider)
        {
            _task.Start();

            await Task.CompletedTask;

            return true;
        }
    }
}
