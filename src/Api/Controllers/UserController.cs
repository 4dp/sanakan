#pragma warning disable 1591

using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [ApiController, Authorize]
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
        [HttpGet("discord/{id}")]
        public async Task<Database.Models.User> GetUserByDiscordIdAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                return await db.GetCachedFullUserAsync(id);
            }
        }

        /// <summary>
        /// Pobieranie użytkownika bota
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <returns>użytkownik bota</returns>
        [HttpGet("shinden/{id}")]
        public async Task<UserWithToken> GetUserByShindenIdAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = db.Users.FirstOrDefault(x => x.Shinden == id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                TokenData tokenData = null;
                var currUser = ControllerContext.HttpContext.User;
                if (currUser.HasClaim(x => x.Type == ClaimTypes.Webpage))
                {
                    tokenData = BuildUserToken(user);
                }

                return new UserWithToken()
                {
                    Token = tokenData?.Token,
                    Expire = tokenData?.Expire,
                    User = await db.GetCachedFullUserAsync(user.Id),
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
        [HttpPut("register")]
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
        [HttpPut("discord/{id}/tc")]
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

                var exe = new Executable($"api-tc u{id}", new Task(() =>
                {
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
        [HttpPut("shinden/{id}/tc")]
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

                var exe = new Executable($"api-tc su{id}", new Task(() =>
                {
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

        private TokenData BuildUserToken(Database.Models.User user)
        {
            var config = _config.Get();

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("DiscordId", user.Id.ToString()),
                new Claim("Player", "waifu_player"),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(config.Jwt.Issuer,
              config.Jwt.Issuer,
              claims,
              expires: DateTime.Now.AddMinutes(30),
              signingCredentials: creds);

            return new TokenData()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expire = token.ValidTo
            };
        }
    }
}