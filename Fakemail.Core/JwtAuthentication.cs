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
        private readonly string _secret;
        private readonly string _validIssuer;
        private readonly int _expiryMinutes;

        public JwtAuthentication(string secret, string validIssuer, int expiryMinutes)
        {
            // The CreateToken() method will fail if the secret (converted to bytes) is smaller than 32 bytes.
            //
            // As our secret is ASCII encoded, we need more characters than this to get 32 bytes of
            // randomness. To allow the secret to be specified as hex, require it to be at least 64 characters.
            if (secret?.Length < 64)
            {
                throw new Exception("JWT secret must be at least 64 characters in length");
            }

            _secret = secret;
            _validIssuer = validIssuer;
            _expiryMinutes = expiryMinutes;
        }

        public string GetAuthenticationToken(Guid userId, bool isAdmin = false)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, userId.ToString()),
                new(ClaimTypes.Role, "user")
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.ASCII.GetBytes(_secret);
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
