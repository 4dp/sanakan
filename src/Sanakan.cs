#pragma warning disable 1591

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Sanakan.Api;
using Sanakan.Config;
using Sanakan.Services;
using Sanakan.Services.Commands;
using Sanakan.Services.Executor;
using Sanakan.Services.PocketWaifu;
using Sanakan.Services.Session;
using Sanakan.Services.Supervisor;
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
        private SessionManager _sessions;
        private CommandHandler _handler;
        private ExperienceManager _exp;
        private Supervisor _supervisor;
        private ImageProcessing _img;
        private DeletedLog _deleted;
        private Daemonizer _daemon;
        private Greeting _greeting;
        private Profile _profile;
        private IConfig _config;
        private ILogger _logger;
        private Moderator _mod;
        private Helper _helper;
        private Events _events;
        private Waifu _waifu;
        private Spawn _spawn;
        private Chaos _chaos;

        public static void Main() => new Sanakan().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            LoadConfig();
            CreateModules();
            AddSigTermHandler();
            EnsureDbIsCreated();

            var tmpCnf = _config.Get();
            await _client.LoginAsync(TokenType.Bot, tmpCnf.BotToken);
            await _client.SetGameAsync(tmpCnf.Prefix + "pomoc");
            await _client.StartAsync();

            var services = BuildServiceProvider();
            BotWebHost.RunWebHost(_client, _shindenClient, _waifu, _config, _helper, _executor, _logger);

            _executor.Initialize(services);
            _sessions.Initialize(services);
            await _handler.InitializeAsync(services, _helper);

            await Task.Delay(-1);
        }

        private void EnsureDbIsCreated()
        {
            using (var db = new Database.BuildDatabaseContext(_config))
            {
                db.Database.EnsureCreated();
            }
        }

        private void CreateModules()
        {
            Services.Dir.Create();

            _logger = new ConsoleLogger();

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 200,
            });

            _client.Log += log =>
            {
                _logger.Log(log.ToString());
                return Task.CompletedTask;
            };

            var tmpCnf = _config.Get();
            _shindenClient = new ShindenClient(new Auth(tmpCnf.Shinden.Token,
                tmpCnf.Shinden.UserAgent, tmpCnf.Shinden.Marmolade), _logger);

            _helper = new Helper(_config);
            _events = new Events(_shindenClient);
            _img = new ImageProcessing(_shindenClient);
            _deleted = new DeletedLog(_client, _config);
            _chaos = new Chaos(_client, _config, _logger);
            _executor = new SynchronizedExecutor(_logger);
            _mod = new Moderator(_logger, _config, _client);
            _waifu = new Waifu(_img, _shindenClient, _events);
            _daemon = new Daemonizer(_client, _logger, _config);
            _sessions = new SessionManager(_client, _executor, _logger);
            _supervisor = new Supervisor(_client, _config, _logger, _mod);
            _greeting = new Greeting(_client, _logger, _config, _executor);
            _exp = new ExperienceManager(_client, _executor, _config, _img);
            _spawn = new Spawn(_client, _executor, _waifu, _config, _logger);
            _handler = new CommandHandler(_client, _config, _logger, _executor);
            _profile = new Profile(_client, _shindenClient, _img, _logger, _config);
        }

        private void LoadConfig()
        {
#if !DEBUG
            _config = new ConfigManager("Config.json");
#else
            _config = new ConfigManager("ConfigDebug.json");
#endif
        }

        private void AddSigTermHandler()
        {
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
        }

        private IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton<IExecutor>(_executor)
                .AddSingleton(_shindenClient)
                .AddSingleton(_sessions)
                .AddSingleton(_profile)
                .AddSingleton(_config)
                .AddSingleton(_logger)
                .AddSingleton(_client)
                .AddSingleton(_helper)
                .AddSingleton(_events)
                .AddSingleton(_chaos)
                .AddSingleton(_waifu)
                .AddSingleton(_spawn)
                .AddSingleton(_mod)
                .AddSingleton(_exp)
                .AddSingleton(_img)
                .AddSingleton<Services.Fun>()
                .AddSingleton<Services.Shinden>()
                .AddSingleton<Services.LandManager>()
                .BuildServiceProvider();
        }
    }
}
