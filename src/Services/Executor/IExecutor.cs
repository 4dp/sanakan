#pragma warning disable 1591

using System;

namespace Sanakan.Services.Executor
{
    public interface IExecutor
    {
        void RunWorker();
        bool TryAdd(IExecutable task, TimeSpan timeout);
    }
}
