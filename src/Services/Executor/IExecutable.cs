#pragma warning disable 1591

using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public enum Priority
    {
        Normal = 0,
        High = 1,
        Low = 2
    }

    public interface IExecutable
    {
        string GetName();
        Priority GetPriority();
        Task<bool> ExecuteAsync(IServiceProvider provider);
    }
}
