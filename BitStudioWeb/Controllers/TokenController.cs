using BitStudioWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BitStudioWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TokenController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("generate")]
        public IActionResult GenerateToken(string mobile)
        {
            var tokenSecret = _configuration["TokenSecret"];
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(tokenSecret);
            var Expires = DateTime.UtcNow.AddDays(7); // 设置Token的过期时间
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
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { token = tokenString, expires= Expires.ToString("yyyy-MM-dd HH:mm:ss") });
        }

        [HttpPost("verify")]
  
        public IActionResult VerifyToken([FromBody] TokenRequest request)
        {
            var tokenSecret = _configuration["TokenSecret"];
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(tokenSecret);

            try
            {
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

                if (claimsPrincipal.Identity.IsAuthenticated)
                {
                    return Ok(new { expiredAt = expirationDate, isValid = true });
                }
                else
                {
                    return Ok(new { expiredAt = expirationDate, isValid = false });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("test")]
        public ActionResult Test()
        {
            return Ok(new { result= "Authorize" });
        }

    }
}
