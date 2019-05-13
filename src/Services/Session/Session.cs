#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Sanakan.Services.Executor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Services.Session
{
    public class Session : ISession
    {
        protected List<IUser> _owners = new List<IUser>();

        private Func<ISession, Task> OnSyncEnd { get; set; }
        private Stopwatch _timer { get; set; }
        private bool _added { get; set; }

        public Session(IUser owner)
        {
            _owners.Add(owner);
            _added = false;

            RunMode = RunMode.Sync;
            TimeoutMs = 30000;

            OnDispose = null;
            OnExecute = null;
            OnSyncEnd = null;
            Id = null;
        }
        
        // Session
        public string Id { get; set; }
        public RunMode RunMode { get; set; }
        public ExecuteOn Event { get; set; }
        public long TimeoutMs { get; set; }
        public Func<Task> OnDispose { get; set; }
        public Func<SessionContext, Session, Task<bool>> OnExecute { get; set; }
        public void AddParticipant(IUser participant) => _owners.Add(participant);

        public void RestartTimer()
        {
            if (_added && TimeoutMs > 0)
                _timer = Stopwatch.StartNew();
        }

        // ISession
        public string GetId() => Id;
        public RunMode GetRunMode() => RunMode;
        public ExecuteOn GetEventType() => Event;
        public IUser GetOwner() => _owners.First();
        public bool IsOwner(IUser user) => _owners.Any(x => x.Id == user.Id);
        public void SetDestroyer(Func<ISession, Task> destroyer) => OnSyncEnd = destroyer;

        public void MarkAsAdded()
        {
            if (!_added)
            {
                _added = true;

                if (TimeoutMs > 0)
                    _timer = Stopwatch.StartNew();
            }
        }

        public bool IsValid()
        {
            if (_timer == null) return true;
            return _timer.ElapsedMilliseconds <= TimeoutMs;
        }

        public async Task DisposeAsync()
        {
            if (OnDispose != null)
                await OnDispose();

            _timer = null;
            _owners = null;

            OnDispose = null;
            OnExecute = null;
            OnSyncEnd = null;
        }

        public IExecutable GetExecutable(SessionContext context)
        {
            return new Executable(new Task<bool>(() => 
            {
                if (OnExecute == null)
                    return true;

                var res = OnExecute(context, this).Result;
                if (res && RunMode == RunMode.Sync && OnSyncEnd != null)
                {
                    _ = Task.Run(async () =>
                   {
                       await Task.Delay(500);
                       await OnSyncEnd(this);
                   });
                }
                
                return res;
            }));
        }
    }
}