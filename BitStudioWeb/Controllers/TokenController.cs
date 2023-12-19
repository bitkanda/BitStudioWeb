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

        //[HttpPost("generate")]
        //public IActionResult GenerateToken(string mobile)
        //{
        //    DateTime Expires;
        //    string tokenString;
        //    TokenHepler.GenerateTokenModel(_configuration,mobile, out Expires, out tokenString);

        //    return Ok(new { token = tokenString, expires = Expires.ToString("yyyy-MM-dd HH:mm:ss") });
        //}



        [HttpPost("verify")]
  
        public IActionResult VerifyToken([FromBody] TokenRequest request)
        {
            try
            {
                var (IsAuthenticated, expirationDate) = TokenHepler.VerifyTokenModel(_configuration,request);
                if (IsAuthenticated)
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

    
        //[Authorize]
        //[HttpPost("test")]
        //public ActionResult Test()
        //{
        //    // 获取当前用户的标识
        //    var userIdentity = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    return Ok(new { result= "Authorize" });
        //}

    }
}
