#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Services.Executor;
using Shinden.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sanakan.Services.Session
{
    public class SessionManager
    {
        private DiscordSocketClient _client;
        private IServiceProvider _provider;
        private IExecutor _executor;
        private ILogger _logger;
        private Timer _timer;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private List<ISession> _sessions = new List<ISession>();

        public SessionManager(DiscordSocketClient client, IExecutor executor, ILogger logger)
        {
            _client = client;
            _logger = logger;
            _executor = executor;
        }

        public void Initialize(IServiceProvider provider)
        {
            _provider = provider;

            _timer = new Timer(async _ =>
            {
                await AutoValidate();
            },
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30));

            _client.MessageReceived += HandleMessageAsync;
            _client.ReactionAdded += HandleReactionAddedAsync;
            _client.ReactionRemoved += HandleReactionRemovedAsync;
        }

        public async Task<bool> TryAddSession<T>(T session) where T : ISession
        {
            if (SessionExist(session))
                return false;

            await _semaphore.WaitAsync();

            try
            {
                if (_sessions.Count < 1)
                    ToggleAutoValidation(true);

                _sessions.Add(session);
                session.MarkAsAdded();
            }
            finally
            {
                _semaphore.Release();
            }

            return true;
        }

        public async Task KillSessionIfExistAsync<T>(T session) where T : ISession
        {
            var thisSession = _sessions.FirstOrDefault(x => x.IsOwner(session.GetOwner())
                && ((x.GetId() == null) ? (x is T) : (x.GetId() == session.GetId())));

            if (thisSession != null) await DisposeAsync(thisSession).ConfigureAwait(false);
        }

        public bool SessionExist<T>(T session) where T : ISession
            => _sessions.Where(x => x.IsOwner(session.GetParticipants()))
                .Any(x => ((x.GetId() == null) ? (x is T) : (x.GetId() == session.GetId())));

        private async Task DisposeAsync(ISession session)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (_sessions.Contains(session))
                    _sessions.Remove(session);

                await session.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task RunSessions(List<ISession> sessions, SessionContext context)
        {
            foreach (var session in sessions)
            {
                if (!session.IsValid())
                {
                    await DisposeAsync(session);
                    continue;
                }

                session.WithLogger(_logger);

                switch (session.GetRunMode())
                {
                    case RunMode.Async:
                        _ = Task.Run(async () =>
                        {
                            if (await session.GetExecutable(context).ExecuteAsync(_provider).Unwrap().ConfigureAwait(false))
                                await DisposeAsync(session).ConfigureAwait(false);
                        });
                        break;

                    default:
                    case RunMode.Sync:
                        session.SetDestroyer(DisposeAsync);
                        if (!await _executor.TryAdd(session.GetExecutable(context), TimeSpan.FromSeconds(1)))
                                _logger.Log($"Sessions: {session.GetEventType()}-{session.GetOwner().Id} waiting time has been exceeded!");
                        break;
                }
            }
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            var msg = message as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.IsBot || msg.Author.IsWebhook) return;

            var userSessions = _sessions.FindAll(x => x.IsOwner(message.Author)
                && x.GetEventType().HasFlag(ExecuteOn.Message));

            if (userSessions.Count == 0) return;

            await RunSessions(userSessions, new SessionContext(new SocketCommandContext(_client, msg))).ConfigureAwait(false);
        }

        private async Task HandleReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified) return;
            var user = reaction.User.Value;

            if ((user.IsBot || user.IsWebhook)) return;

            var userSessions = _sessions.FindAll(x => x.IsOwner(user)
                && x.GetEventType().HasFlag(ExecuteOn.ReactionAdded));

            if (userSessions.Count == 0) return;

            var thisUser = _client.GetUser(user.Id);
            if (thisUser == null) return;

            var chan = await channel.GetOrDownloadAsync();
            if (chan == null) return;

            var msg = await chan.GetMessageAsync(message.Id);
            if (msg == null) return;

            var thisMessage = msg as IUserMessage;
            if (thisMessage == null) return;

            await RunSessions(userSessions, new SessionContext(chan, thisUser, thisMessage, _client, reaction, true)).ConfigureAwait(false);
        }

        private async Task HandleReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified) return;
            var user = reaction.User.Value;

            if ((user.IsBot || user.IsWebhook)) return;

            var userSessions = _sessions.FindAll(x => x.IsOwner(user)
                && x.GetEventType().HasFlag(ExecuteOn.ReactionRemoved));

            if (userSessions.Count == 0) return;

            var thisUser = _client.GetUser(user.Id);
            if (thisUser == null) return;

            var chan = await channel.GetOrDownloadAsync();
            if (chan == null) return;

            var msg = await chan.GetMessageAsync(message.Id);
            if (msg == null) return;

            var thisMessage = msg as IUserMessage;
            if (thisMessage == null) return;

            await RunSessions(userSessions, new SessionContext(chan, thisUser, thisMessage, _client, reaction, false)).ConfigureAwait(false);
        }

        private void ToggleAutoValidation(bool on)
        {
            if (on)
                _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
            else
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private async Task AutoValidate()
        {
            if (_sessions.Count < 1)
            {
                ToggleAutoValidation(false);
                return;
            }

            try
            {
                for (int i = _sessions.Count; i > 0; i--)
                {
                    if (!_sessions[i - 1].IsValid())
                        await DisposeAsync(_sessions[i - 1]);
                }
            }
            catch(Exception ex)
            {
                _logger.Log($"Session: autovalidate error {ex}");
            }
        }
    }
}
