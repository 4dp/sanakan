﻿#pragma warning disable 1591

using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public class Executable : IExecutable
    {
        private Task<Task> _task { get; set; }

        private readonly string _name;
        private readonly Priority _priority;

        public Executable(string name, Task<Task> task, Priority priority = Priority.Normal)
        {
            _name = name;
            _task = task;
            _priority = priority;
        }

        public Priority GetPriority() => _priority;

        public string GetName() => _name;

        public void Wait() => _task.Unwrap().Wait();
        public async Task WaitAsync() => await _task.Unwrap();

        public async Task<Task<bool>> ExecuteAsync(IServiceProvider provider)
        {
            try
            {
                _task.Start();

                await _task.ConfigureAwait(false);

                if (_task.Unwrap() is Task<bool> bTask)
                {
                    return Task.FromResult(bTask.Result);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                throw new Exception("in executable:", ex);
            }
        }
    }
}
