#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sanakan.Api.Models;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Shinden;
using Shinden.Logger;
using Z.EntityFramework.Plus;

namespace Sanakan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfig _config;
        private readonly ILogger _logger;
        private readonly IExecutor _executor;
        private readonly ShindenClient _shClient;
        private readonly DiscordSocketClient _client;

        public UserController(DiscordSocketClient client, ShindenClient shClient, ILogger logger, IExecutor executor, IConfig config)
        {
            _config = config;
            _client = client;
            _logger = logger;
            _executor = executor;
            _shClient = shClient;
        }

        /// <summary>
        /// Pobieranie użytkownika bota
        /// </summary>
        /// <param name="id">id użytkownika discorda</param>
        /// <returns>użytkownik bota</returns>
        [HttpGet("discord/{id}"), Authorize(Policy = "Site")]
        public async Task<Database.Models.User> GetUserByDiscordIdAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                return await db.GetCachedFullUserAsync(id);
            }
        }

        /// <summary>
        /// Wyszukuje id użytkownika na shinden
        /// </summary>
        /// <param name="name">nazwa użytkownika</param>
        /// <returns>id użytkownika</returns>
        [HttpPost("find")]
        public async Task<IEnumerable<Shinden.Models.IUserSearch>> GetUserIdByNameAsync([FromBody, Required]string name)
        {
            var res = await _shClient.Search.UserAsync(name);
            if (!res.IsSuccessStatusCode())
            {
                await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                return null;
            }
            return res.Body;
        }

        /// <summary>
        /// Pobieranie użytkownika bota
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <returns>użytkownik bota</returns>
        [HttpGet("shinden/{id}"), Authorize(Policy = "Site")]
        public async Task<UserWithToken> GetUserByShindenIdAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.GetCachedFullUserByShindenIdAsync(id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                TokenData tokenData = null;
                var currUser = ControllerContext.HttpContext.User;
                if (currUser.HasClaim(x => x.Type == ClaimTypes.Webpage))
                {
                    tokenData = UserTokenBuilder.BuildUserToken(_config, user);
                }

                return new UserWithToken()
                {
                    Expire = tokenData?.Expire,
                    Token = tokenData?.Token,
                    User = user,
                };
            }
        }

        /// <summary>
        /// Pobieranie użytkownika bota z zmniejszoną ilością danych
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <returns>użytkownik bota</returns>
        [HttpGet("shinden/simple/{id}"), Authorize(Policy = "Site")]
        public async Task<UserWithToken> GetUserByShindenIdSimpleAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = db.Users.AsQueryable().AsSplitQuery().Where(x => x.Shinden == id).Include(x => x.GameDeck).AsNoTracking().FirstOrDefault();
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                TokenData tokenData = null;
                var currUser = ControllerContext.HttpContext.User;
                if (currUser.HasClaim(x => x.Type == ClaimTypes.Webpage))
                {
                    tokenData = UserTokenBuilder.BuildUserToken(_config, user);
                }

                return new UserWithToken()
                {
                    Expire = tokenData?.Expire,
                    Token = tokenData?.Token,
                    User = user,
                };
            }
        }

        /// <summary>
        /// Pełne łączenie użytkownika
        /// </summary>
        /// <param name="id">relacja</param>
        /// <response code="403">Can't connect to shinden!</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Model is invalid!</response>
        [HttpPut("register"), Authorize(Policy = "Site")]
        public async Task RegisterUserAsync([FromBody, Required]UserRegistration id)
        {
            if (id == null)
            {
                var body = new System.IO.StreamReader(ControllerContext.HttpContext.Request.Body);
                body.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
                var requestBody = body.ReadToEnd();

                _logger.Log(requestBody);

                await "Model is Invalid!".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return;
            }

            var user = _client.GetUser(id.DiscordUserId);
            if (user == null)
            {
                await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                return;
            }

            using (var db = new Database.UserContext(_config))
            {
                var botUser = db.Users.FirstOrDefault(x => x.Id == id.DiscordUserId);
                if (botUser != null)
                {
                    if (botUser.Shinden != 0)
                    {
                        await "User already connected!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                        return;
                    }
                }

                var response = await _shClient.Search.UserAsync(id.Username);
                if (!response.IsSuccessStatusCode())
                {
                    await "Can't connect to shinden!".ToResponse(403).ExecuteResultAsync(ControllerContext);
                    return;
                }

                var sUser = (await _shClient.User.GetAsync(response.Body.First())).Body;
                if (sUser.ForumId.Value != id.ForumUserId)
                {
                    await "Something went wrong!".ToResponse(500).ExecuteResultAsync(ControllerContext);
                    return;
                }

                var exe = new Executable($"api-register u{id.DiscordUserId}", new Task(() =>
                {
                    using (var dbs = new Database.UserContext(_config))
                    {
                        botUser = dbs.GetUserOrCreateAsync(id.DiscordUserId).Result;
                        botUser.Shinden = sUser.Id;

                        dbs.SaveChanges();

                        QueryCacheManager.ExpireTag(new string[] { $"user-{user.Id}", "users" });
                    }
                }));

                await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                await "User connected!".ToResponse(200).ExecuteResultAsync(ControllerContext);
            }
        }

        /// <summary>
        /// Zmiana ilości punktów TC użytkownika
        /// </summary>
        /// <param name="id">id użytkownika discorda</param>
        /// <param name="value">liczba TC</param>
        /// <response code="404">User not found</response>
        [HttpPut("discord/{id}/tc"), Authorize(Policy = "Site")]
        public async Task ModifyPointsTCDiscordAsync(ulong id, [FromBody, Required]long value)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = db.Users.FirstOrDefault(x => x.Id == id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return;
                }

                var exe = new Executable($"api-tc u{id} ({value})", new Task(() =>
                {
                    using (var dbc = new Database.AnalyticsContext(_config))
                    {
                        dbc.TransferData.Add(new Database.Models.Analytics.TransferAnalytics()
                        {
                            Value = value,
                            DiscordId = user.Id,
                            Date = DateTime.Now,
                            ShindenId = user.Shinden,
                            Source = Database.Models.Analytics.TransferSource.ByDiscordId,
                        });

                        dbc.SaveChanges();
                    }

                    using (var dbs = new Database.UserContext(_config))
                    {
                        user = dbs.GetUserOrCreateAsync(id).Result;
                        user.TcCnt += value;

                        dbs.SaveChanges();

                        QueryCacheManager.ExpireTag(new string[] { $"user-{user.Id}", "users" });
                    }
                }));

                await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                await "TC added!".ToResponse(200).ExecuteResultAsync(ControllerContext);
            }
        }

        /// <summary>
        /// Zmiana ilości punktów TC użytkownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <param name="value">liczba TC</param>
        /// <response code="404">User not found</response>
        [HttpPut("shinden/{id}/tc"), Authorize(Policy = "Site")]
        public async Task ModifyPointsTCAsync(ulong id, [FromBody, Required]long value)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = db.Users.FirstOrDefault(x => x.Shinden == id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return;
                }

                var exe = new Executable($"api-tc su{id} ({value})", new Task(() =>
                {
                    using (var dbc = new Database.AnalyticsContext(_config))
                    {
                        dbc.TransferData.Add(new Database.Models.Analytics.TransferAnalytics()
                        {
                            Value = value,
                            DiscordId = user.Id,
                            Date = DateTime.Now,
                            ShindenId = user.Shinden,
                            Source = Database.Models.Analytics.TransferSource.ByShindenId,
                        });

                        dbc.SaveChanges();
                    }

                    using (var dbs = new Database.UserContext(_config))
                    {
                        user = dbs.Users.FirstOrDefault(x => x.Shinden == id);
                        user.TcCnt += value;

                        dbs.SaveChanges();

                        QueryCacheManager.ExpireTag(new string[] { $"user-{user.Id}", "users" });
                    }
                }));

                await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                await "TC added!".ToResponse(200).ExecuteResultAsync(ControllerContext);
            }
        }
    }
}