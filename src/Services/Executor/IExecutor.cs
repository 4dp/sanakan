#pragma warning disable 1591

namespace Sanakan.Services.Executor
{
    public interface IExecutor
    {
        bool TryAdd(IExecutable task);
    }
}
