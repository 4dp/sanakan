#pragma warning disable 1591

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Sanakan.Config;
using Sanakan.Services;
using Sanakan.Services.Commands;
using Sanakan.Services.Executor;
using Shinden;
using Shinden.Logger;
using System;
using System.Threading.Tasks;

namespace Sanakan
{
    class Sanakan
    {
        private SynchronizedExecutor _executor;
        private ShindenClient _shindenClient;
        private DiscordSocketClient _client;
        private CommandHandler _handler;
        private IConfig _config;
        private ILogger _logger;

        public static void Main() => new Sanakan().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _logger = new ConsoleLogger();
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 250,
            });

            _client.Log += log =>
            {
                _logger.Log(log.ToString());
                return Task.CompletedTask;
            };

#if !DEBUG
            _config = new ConfigManager("Config.json");
#else
            _config = new ConfigManager("ConfigDebug.json");
#endif

            Console.CancelKeyPress += delegate
            {
                _ = Task.Run(async () =>
                {
                    _logger.Log("SIGTERM Received!");
                    await _client.LogoutAsync();
                    await Task.Delay(1000);
                    Environment.Exit(0);
                });
            };

            var tmpCnf = _config.Get();
            await _client.LoginAsync(TokenType.Bot, tmpCnf.BotToken);
            await _client.SetGameAsync(tmpCnf.Prefix + "pomoc");
            await _client.StartAsync();

            _executor = new SynchronizedExecutor();
            _shindenClient = new ShindenClient(new Auth(tmpCnf.Shinden.Token, 
                tmpCnf.Shinden.UserAgent, tmpCnf.Shinden.Marmolade), _logger);

            var services = new ServiceCollection()
                .AddSingleton(_shindenClient)
                .AddSingleton(_executor)
                .AddSingleton(_config)
                .AddSingleton(_logger)
                .AddSingleton(_client)
                .BuildServiceProvider();

            _executor.Initialize(services);
            
            _handler = new CommandHandler(services, _client, _config, _logger, _executor);
            await _handler.InitializeAsync();

            await Task.Delay(-1);
        }
    }
}
