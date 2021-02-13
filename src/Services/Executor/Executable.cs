#pragma warning disable 1591

using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public class Executable : IExecutable
    {
        private Task _task { get; set; }

        private readonly string _name;
        private readonly Priority _priority;

        public Executable(string name, Task task, Priority priority = Priority.Normal)
        {
            _name = name;
            _task = task;
            _priority = priority;
        }

        public Priority GetPriority() => _priority;

        public string GetName() => _name;

        public void Wait() => _task.Wait();

        public async Task<bool> ExecuteAsync(IServiceProvider provider)
        {
            try
            {
                _task.Start();

                await Task.CompletedTask;

                if (_task is Task<bool> bTask)
                {
                    return bTask.Result;
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("in executable:", ex);
            }
        }
    }
}
