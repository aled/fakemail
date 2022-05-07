using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Fakemail.Core
{
    public class JwtAuthentication : IJwtAuthentication
    {
        private readonly string _key;

        public JwtAuthentication(string key)
        {
            _key = key;
        }

        public string GetAuthenticationToken(string username, bool isAdmin = false)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, username));
            claims.Add(new Claim(ClaimTypes.Role, "user"));

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.ASCII.GetBytes(_key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = "localhost:7053/", //"fakemail.stream/"                ,
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha512Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
