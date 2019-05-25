#pragma warning disable 1591

using Microsoft.AspNetCore.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Text;
using Sanakan.Config;
using Microsoft.AspNetCore.Mvc;
using Sanakan.Services.Executor;
using Discord.WebSocket;
using Shinden;
using Sanakan.Services.PocketWaifu;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Sanakan.Api
{
    public static class BotWebHost
    {
        public static void RunWebHost(DiscordSocketClient client, ShindenClient shinden, Waifu waifu, IConfig config, Services.Helper helper, IExecutor executor, Shinden.Logger.ILogger logger)
        {
            new Thread(() => 
            {
                Thread.CurrentThread.IsBackground = true;
                CreateWebHostBuilder(config).ConfigureServices(services =>
                {
                    services.AddSingleton(waifu);
                    services.AddSingleton(logger);
                    services.AddSingleton(client);
                    services.AddSingleton(helper);
                    services.AddSingleton(shinden);
                    services.AddSingleton(executor);
                }).Build().Run();
            }).Start();
        }

        private static IWebHostBuilder CreateWebHostBuilder(IConfig config) =>
            WebHost.CreateDefaultBuilder().ConfigureServices(services =>
            {
                var tmpCnf = config.Get();
                services.AddSingleton(config);

                services.AddDbContext<Database.UserContext>();
                services.AddDbContext<Database.ManagmentContext>();
                services.AddDbContext<Database.GuildConfigContext>();

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = tmpCnf.Jwt.Issuer,
                        ValidAudience = tmpCnf.Jwt.Issuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tmpCnf.Jwt.Key))
                    };
                });
                services.AddAuthorization(op =>
                {
                    op.AddPolicy("Player", policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "Player" && c.Value == "waifu_player")));
                });
                services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddJsonOptions(o => o.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)
                    .AddJsonOptions(o => o.SerializerSettings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }));
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowEverything", builder =>
                    {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
                });
                services.AddApiVersioning(o =>
                {
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                    o.ApiVersionReader = new HeaderApiVersionReader("x-api-version");
                });
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v2", new Info
                    {
                        Title = "Sanakan API",
                        Version = "1.0",
                        Description = "Autentykacja następuje poprzez dopasowanie tokenu przesłanego w ciele zapytania `api/token`, a następnie wysyłania w nagłowku `Authorization` z przedrostkiem `Bearer` otrzymanego w zwrocie tokena."
                            + "\n\nDocelowa wersja api powinna zostać przesłana pod nagówkiem `x-api-version`, w przypadku jej nie podania zapytania są interpretowane jako wysłane do wersji `1.0`.",
                    });

                    var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Sanakan.xml");
                    if (File.Exists(filePath)) c.IncludeXmlComments(filePath);

                    c.DescribeAllEnumsAsStrings();
                    c.CustomSchemaIds(x => x.FullName);
                });
            }).ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole(x => x.DisableColors = true);
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Configure(app =>
            {
                app.UseSwagger();
                app.UseCors("AllowEverything");
                app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
                app.UseAuthentication();
                app.UseMvc();
#if !DEBUG
            });
#else   
            }).UseUrls("http://*:5005");
#endif
            }
}