namespace Sanakan.Services.Executor
{
    public interface IExecutor
    {
        bool TryAdd(IExecutable task);
    }
}
