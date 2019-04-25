#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Sanakan.Services.Executor;
using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Session
{
    [Flags]
    public enum ExecuteOn
    {
        None            = 0,
        Message         = 1 << 0,
        ReactionAdded   = 1 << 1,
        ReactionRemoved = 1 << 2,
        
        AllReactions    = ReactionAdded | ReactionRemoved,
        AllEvents       = Message | AllReactions,
    }

    public interface ISession
    {
        bool IsValid();
        string GetId();
        IUser GetOwner();
        void MarkAsAdded();
        Task DisposeAsync();
        RunMode GetRunMode();
        ExecuteOn GetEventType();
        bool IsOwner(IUser user);
        void SetDestroyer(Func<ISession, Task> destroyer);
        IExecutable GetExecutable(SessionContext context);
    }
}
