#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Shinden.Logger;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sanakan.Services.Commands
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;
        private IServiceProvider _provider;
        private CommandService _cmd;
        private IExecutor _executor;
        private ILogger _logger;
        private IConfig _config;
        private Helper _helper;

        public CommandHandler(DiscordSocketClient client, IConfig config, ILogger logger, IExecutor executor)
        {
            _client = client;
            _config = config;
            _logger = logger;
            _executor = executor;
            _cmd = new CommandService();
        }

        public async Task InitializeAsync(IServiceProvider provider, Helper helper)
        {
            _helper = helper;
            _provider = provider;

            _helper.PublicModulesInfo = await _cmd.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
            _helper.PrivateModulesInfo.Add("Moderacja", await _cmd.AddModuleAsync<Modules.Moderation>(_provider));

            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage message)
        {
            var msg = message as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.IsBot || msg.Author.IsWebhook) return;

            int argPos = 0;
            if (msg.HasStringPrefix(_config.Get().Prefix, ref argPos))
            {
                var context = new SocketCommandContext(_client, msg);
                var res = await _cmd.GetExecutableCommandAsync(context, argPos, _provider);

                if (res.IsSuccess())
                {
                    switch (res.Command.Match.Command.RunMode)
                    {
                        case RunMode.Async:
                            await res.Command.ExecuteAsync(_provider);
                            break;

                        default:
                        case RunMode.Sync:
                            if (!_executor.TryAdd(res.Command, TimeSpan.FromSeconds(1)))
                                    await context.Channel.SendMessageAsync("", embed: "Przekroczono czas oczekiwania!".ToEmbedMessage(EMType.Error).Build());
                            break;
                    }
                }
                else await ProcessResultAsync(res.Result, context, argPos);
            }
        }

        private async Task ProcessResultAsync(IResult result, SocketCommandContext context, int argPos)
        {
            if (result == null) return;

            switch (result.Error)
            {
                case CommandError.MultipleMatches:
                    await context.Channel.SendMessageAsync("", embed: "Dopasowano wielu użytkowników!".ToEmbedMessage(EMType.Error).Build());
                    break;

                case CommandError.ParseFailed:
                case CommandError.BadArgCount:
                    var cmd = _cmd.Search(context, argPos);
                    if (cmd.Commands.Count > 0)
                        await context.Channel.SendMessageAsync(_helper.GetCommandInfo(cmd.Commands.First().Command));
                    break;

                case CommandError.UnmetPrecondition:
                    if (result.ErrorReason.StartsWith("|IMAGE|"))
                    {
                        var emb = new EmbedBuilder()
                            .WithImageUrl(result.ErrorReason.Remove(0, 7))
                            .WithColor(EMType.Error.Color());

                        await context.Channel.SendMessageAsync("", embed: emb.Build());
                    }
                    else await context.Channel.SendMessageAsync("", embed: result.ErrorReason.ToEmbedMessage(EMType.Error).Build());
                    break;

                default:
                    _logger.Log(result.ErrorReason);
                    break;
            }
        }
    }
}
