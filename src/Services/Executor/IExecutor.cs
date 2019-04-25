#pragma warning disable 1591

namespace Sanakan.Services.Executor
{
    public interface IExecutor
    {
        void RunWorker();
        bool TryAdd(IExecutable task);
    }
}
