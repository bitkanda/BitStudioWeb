﻿using bitkanda.Dal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;

namespace BitStudioWeb.Controllers
{
   
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly MysqlDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private static object _locker = new object();

        public UserController(MysqlDBContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        // 获取手机验证码接口
        [HttpPost("GetSmsCode")]
        public ActionResult GetSmsCode(string phoneNumber)
        {
            try
            {
                lock (_locker)
                {
                    // 生成随机验证码
                    string smsCode = GenerateSmsCode();



                    // 将验证码保存到数据库
                    var (IsSuccess, ErrorMsg) = SaveSmsCodeToDatabase(phoneNumber, smsCode);
                    if (IsSuccess)
                    {
                        return Json(new { success = true, message = "验证码已发送" });
                    }
                    else
                    {
                        return Json(new { success = false, message = ErrorMsg });
                    }
                }

            }
            catch(Exception error)
            {
                return Json(new { success = false, message = error.Message+ error.StackTrace });
            }



        }
        private (bool, string) SaveSmsCodeToDatabase(string phoneNumber, string smsCode)
        {
            bool result = true;
            string errorMsg = "";
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                user = new User { PhoneNumber = phoneNumber, AddTime = DateTime.UtcNow };
                _dbContext.Users.Add(user);
            }
            else
            {
                //检查上次获取时间是否超过60S.
                var s =int.Parse( _configuration["SmsSendSpan"]);
                if (DateTime.UtcNow.Subtract(user.LastSendSmsTime).TotalSeconds < s)
                {
                    result = false;
                    errorMsg = "获取验证码太频繁！";
                    return (result, errorMsg);
                }
            }

            var role = TokenHepler.GetUserRole(_configuration, phoneNumber);
            user.Role = role;

            // TODO: 调用发送短信的接口，将验证码发送到用户手机上
            var r = SmsHelper.SemdSmsCode(smsCode, phoneNumber,_configuration);
            if (r.StatusCode == 200 && r.Body != null && r.Body.Code == "OK")
            {
                user.LastSendSmsTime = DateTime.UtcNow;
                user.SmsCode = smsCode;
            }
            else
            {
                result = false;
                errorMsg = r?.Body?.Message;
                return (result, errorMsg);
            }


            _dbContext.SaveChanges();
            return (result, errorMsg);
        }

        // 短信登录接口
        [HttpPost("LoginSms")]
        public ActionResult LoginSms(string phoneNumber, string smsCode)
        {
            // 从数据库中获取保存的验证码
            var  saved = GetSmsCodeFromDatabase(phoneNumber);
            var savedSmsCode = saved?.SmsCode;
            
            if (smsCode == savedSmsCode)
            {
                var sendCodeTime = saved.LastSendSmsTime;
                if (DateTime.UtcNow.Subtract(sendCodeTime).TotalSeconds>60)
                {
                    return Json(new { success = false, message = "验证码过期，请重新获取！" });
                }
                var role = TokenHepler.GetUserRole(_configuration, phoneNumber);
                // 验证码正确，生成登录凭证和过期时间
                DateTime Expires;
                string authToken;
                TokenHepler.GenerateTokenModel(_configuration, phoneNumber, out Expires, out authToken, role);
                 
                // 将登录凭证和过期时间保存到数据库
                SaveAuthTokenToDatabase(phoneNumber, authToken, Expires);

                return Json(new { success = true, authToken=authToken, Expires= Expires.ToString("yyyy-MM-dd HH:mm:dd"), message="" });
            }
            else
            {
                return Json(new { success = false, message = "验证码或手机号错误！" });
            }
        }

        // 验证登录凭证接口
        [HttpPost("VerifyAuthToken")]
        public ActionResult VerifyAuthToken(string phoneNumber, string authToken)
        {

            // 从数据库中获取保存的登录凭证和过期时间
            User user = GetAuthTokenFromDatabase(phoneNumber);

            if (user != null && user.AuthToken == authToken)
            {
                if (DateTime.UtcNow <= user.ExpirationTime)
                {
                    // 登录凭证有效且未过期
                    return Json(new { success = true, isValid = true, expirationTime = user.ExpirationTime });
                }
                else
                {
                    // 登录凭证过期
                    return Json(new { success = true, isValid = false, reason = "登录凭证已过期" });
                }
            }
            else
            {
                // 登录凭证不存在
                return Json(new { success = true, isValid = false, reason = "登录凭证不存在" });
            }
        }

        static  Random random = new Random();
        private string GenerateSmsCode()
        {
            // TODO: 生成随机短信验证码的逻辑
         
            int code = random.Next(100000, 999999); // 生成6位随机数

            return code.ToString();
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("GetUserInfo")]
        public ActionResult GetUserInfo()
        {
            string phoneNumber = HttpContext.User.Identity.Name;
            //var userIdentity = User.FindFirst(phoneNumber)?.Value;
            var user = GetAuthTokenFromDatabase(phoneNumber);
            return Json(user);
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [HttpPost("GetUserInfoUser")]
        public ActionResult GetUserInfoUser()
        {
            string phoneNumber = HttpContext.User.Identity.Name;
            //var userIdentity = User.FindFirst(phoneNumber)?.Value;
            var user = GetAuthTokenFromDatabase(phoneNumber);
            return Json(user);
        }


        /// <summary>
        /// 获取当前用户算力余额。
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [HttpGet("GetUserInv")]
        public ActionResult GetUserInv()
        {
            var user = UserHelper.GetCurrentUserID(User.Identity.Name, _dbContext);
            var now = DateTime.Now;
            var result = new object();
            //查询存在未过期的，或不限过期时间的库存。
            var query = from c in _dbContext.InvTotalMaster
                        join u in _dbContext.InvUsedMaster on
                        new { c.UserID, c.ProductId, c.SkuId, c.ExpDayTime } equals
                        new { u.UserID, u.ProductId, u.SkuId, u.ExpDayTime }
                        into x1 
                        from used in x1.DefaultIfEmpty()
                        join ps in _dbContext.ProductSkus on new { SkuId=c.SkuId } equals new { SkuId=ps.ID }
                        into x2
                        from sku in x2.DefaultIfEmpty()
                        join  p in _dbContext.Products on new { ProductId = c.ProductId} equals new { ProductId = p.ID }
                        into x3
                        from product in x3.DefaultIfEmpty()
                        where c.UserID == user.ID
                       // && (c.ExpDayTime == DateTime.MinValue//不限制时间。
                       // || c.ExpDayTime >= now)//没有过期的。
                       // && (c.Count == 0 || (used == null) || (used != null && c.Count > used.Count))//不限使用次数的，没有使用过的，没有超过限制次数的。
                       // && (used == null || (used != null && c.Value > used.Value))
                        select new
                        {
                            SkuName=(sku == null?"": sku.Name),
                            ProductName=(product==null?"": product.Title),
                            c.ExpDayTime,
                            c.Count,
                            c.ProductId,
                            c.SkuId,
                            c.UserID,
                            c.Value,
                            c.CreateTime,
                            c.ModifyTime,
                            UsedValue=(used == null ? 0 : used.Value),
                            UsedCount=(used == null ? 0 : used.Count),
                            UsedExpDayTime = (used == null ? DateTime.MinValue : used.ExpDayTime),
                            UsedID=(used == null ? 0 : used.ID),
                            IsExpired= ((c.Count > 0 && c.Count <= (used == null ? 0 : used.Count)) 
                            || (c.Value <= (used == null ? 0 : used.Value)) ||
                            (c.ExpDayTime != DateTime.MinValue && c.ExpDayTime < now))
                        }
                        ;
            var q = query.ToList().OrderBy(e=>e.IsExpired);
            //foreach (var one in q)
            //{
            //    //是否失效。
            //    one.IsExpired = (one.Count > 0 && one.Count <= one.UsedCount) || (one.Value <= one.UsedValue) ||
            //        (one.ExpDayTime != DateTime.MinValue && one.ExpDayTime < now);
            //}
            return Json(q);
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles =RoleConst.Admin)]
        [HttpPost("GetUserInfoAdmin")]
        public ActionResult GetUserInfoAdmin()
        {
            string phoneNumber = HttpContext.User.Identity.Name;
            //var userIdentity = User.FindFirst(phoneNumber)?.Value;
            var user = GetAuthTokenFromDatabase(phoneNumber);
            return Json(user);
        }


        private User GetSmsCodeFromDatabase(string phoneNumber)
        {
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
            return user;
        }

     

        private void SaveAuthTokenToDatabase(string phoneNumber, string authToken, DateTime expirationTime)
        {
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                user = new User { PhoneNumber = phoneNumber };
                _dbContext.Users.Add(user);
            }

            user.AuthToken = authToken;
            user.ExpirationTime = expirationTime;
            user.Role = TokenHepler.GetUserRole(_configuration, phoneNumber);
            _dbContext.SaveChanges();
        }

        private User GetAuthTokenFromDatabase(string phoneNumber)
        {
            return _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        }
    }

}
