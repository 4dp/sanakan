#pragma warning disable 1591

using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Executor
{
    public interface IExecutable
    {
        Task<IResult> ExecuteAsync(IServiceProvider provider);
    }
}
