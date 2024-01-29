using BitStudioWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using SharpToken;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BitStudioWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : Controller
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

    }
}
