#pragma warning disable 1591

using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Text;
using System.Security.Claims;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Api.Models;

namespace Sanakan.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly IConfig _config;

        public TokenController(IConfig config)
        {
            _config = config;
        }
        
        /// <summary>
        /// Zwraca token ważny jeden dzień
        /// </summary>
        /// <param name="apikey">Key aplikacji</param>
        /// <response code="401">API Key Not Provided</response>
        /// <response code="403">API Key Is Invalid</response>
        [HttpPost, AllowAnonymous]
        public IActionResult CreateToken([FromBody]string apikey)
        {
            if (apikey == null) return "API Key Not Provided".ToResponse(401);

            IActionResult response = "API Key Is Invalid".ToResponse(403);
            var user = Authenticate(apikey);

            if (user != null)
            {
                var tokenData = BuildToken(user);
                response = Ok(new { token = tokenData.Token, expire = tokenData.Expire });
            }

            return response;
        }

        private TokenData BuildToken(string user)
        {
            var config = _config.Get();

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Website, user),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(config.Jwt.Issuer,
              config.Jwt.Issuer,
              claims,
              expires: DateTime.Now.AddHours(24),
              signingCredentials: creds);

            return new TokenData()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expire = token.ValidTo
            };
        }

        private string Authenticate(string apikey)
        {
            return _config.Get().ApiKeys.FirstOrDefault(x => x.Key.Equals(apikey))?.Bearer;
        }
    }
}
