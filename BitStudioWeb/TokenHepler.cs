﻿using bitkanda.Dal;
using BitStudioWeb.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace BitStudioWeb
{
    public class TokenHepler
    {
        public static string GetUserRole(IConfiguration _configuration, string phoneNumber)
        {
            string result = RoleConst.User; ;
            var adminStr = _configuration["Admin"];
            var Admin = new string[] { };
            if (!string.IsNullOrWhiteSpace(adminStr))
            {
                Admin = adminStr.Split(",");
            }
            if (Admin.Contains(phoneNumber))
                result = RoleConst.Admin;
            else
                result = RoleConst.User;
            return result;
        }

        public static ClaimsPrincipal GetClaimsIdentity(string phoneNumber,string Role= "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, phoneNumber)
            };

            // 添加用户角色声明
            claims.Add(new Claim(ClaimTypes.Role, Role)); // 设置用户的角色为 User
            if(Role== RoleConst.Admin)
            {
                //管理员拥有所有角色。
                claims.Add(new Claim(ClaimTypes.Role, RoleConst.User)); // 设置用户的角色为 User
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            return principal;
        }

        public static void GenerateTokenModel(IConfiguration _configuration, string mobile,
    out DateTime Expires, out string tokenString,string Role=RoleConst.User)
        {
            var tokenSecret = _configuration["TokenSecret"];
            int ex = int.Parse(_configuration["TokenExpirationTime"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(tokenSecret);
            Expires = DateTime.UtcNow.AddDays(ex);
            var claims = new List<Claim> {
                  new Claim(ClaimTypes.Name, mobile), // 根据你的需求添加其他Claim
                  new Claim(ClaimTypes.Role, Role)
            };
            
            if (Role == RoleConst.Admin)
            {
                //管理员拥有所有角色。
                claims.Add(new Claim(ClaimTypes.Role, RoleConst.User)); // 设置用户的角色为 User
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
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
