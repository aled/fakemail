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
        private readonly byte[] _secretBytes;
        private readonly string _validIssuer;
        private readonly int _expiryMinutes;

        public JwtAuthentication(string secret, string validIssuer, int expiryMinutes)
        {
            // secret must be at least 256 bits (32 bytes) otherwise the SymmetricSecurityKey constructor will throw an exception
            if (secret == null || secret.Length < 32)
            {
                throw new Exception("Invalid JWT secret - must be at least 32 bytes");
            }

            _secretBytes = Encoding.ASCII.GetBytes(secret);
            _validIssuer = validIssuer;
            _expiryMinutes = expiryMinutes;
        }

        public string GetAuthenticationToken(Guid userId, bool isAdmin = false)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, userId.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, "user"));

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = _secretBytes;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _validIssuer,
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
