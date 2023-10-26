using BitStudioWeb.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BitStudioWeb
{
    public class TokenHepler
    {
        public static void GenerateTokenModel(IConfiguration _configuration, string mobile,
    out DateTime Expires, out string tokenString)
        {
            var tokenSecret = _configuration["TokenSecret"];
            int ex = int.Parse(_configuration["TokenExpirationTime"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(tokenSecret);
            Expires = DateTime.UtcNow.AddDays(ex);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
{
            new Claim(ClaimTypes.Name, mobile) // 根据你的需求添加其他Claim
}),
                Expires = Expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            tokenString = tokenHandler.WriteToken(token);
        }

        public static (bool, string) VerifyTokenModel(IConfiguration _configuration, TokenRequest request)
        {
            var tokenSecret = _configuration["TokenSecret"];
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(tokenSecret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true
            };

            SecurityToken validatedToken;
            ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(request.Token, validationParameters, out validatedToken);

            var expirationDate = ((JwtSecurityToken)validatedToken).ValidTo.ToString("yyyy-MM-dd HH:mm:ss");

            return (claimsPrincipal.Identity.IsAuthenticated, expirationDate);
        }


    }
}
