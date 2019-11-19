#pragma warning disable 1591

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sanakan.Api.Models;
using Sanakan.Config;

namespace Sanakan.Api
{
    public static class UserTokenBuilder
    {
        public static TokenData BuildUserToken(IConfig conf, Database.Models.User user)
        {
            var config = conf.Get();

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