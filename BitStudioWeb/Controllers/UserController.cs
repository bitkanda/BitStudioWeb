using bitkanda.Dal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Linq;

namespace BitStudioWeb.Controllers
{
    public class UserController : Controller
    {
        private readonly MysqlDBContext _dbContext;

        public UserController(MysqlDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 获取手机验证码接口
        [HttpPost]
        public ActionResult GetSmsCode(string phoneNumber)
        {
            // 生成随机验证码
            string smsCode = GenerateSmsCode();

            // TODO: 调用发送短信的接口，将验证码发送到用户手机上

            // 将验证码保存到数据库
            SaveSmsCodeToDatabase(phoneNumber, smsCode);

            return Json(new { success = true, message = "验证码已发送" });
        }

        // 短信登录接口
        [HttpPost]
        public ActionResult LoginSms(string phoneNumber, string smsCode)
        {
            // 从数据库中获取保存的验证码
            string savedSmsCode = GetSmsCodeFromDatabase(phoneNumber);

            if (smsCode == savedSmsCode)
            {
                // 验证码正确，生成登录凭证和过期时间
                string authToken = GenerateAuthToken();
                DateTime expirationTime = DateTime.UtcNow.AddMinutes(30); // 过期时间为当前时间后的30分钟

                // 将登录凭证和过期时间保存到数据库
                SaveAuthTokenToDatabase(phoneNumber, authToken, expirationTime);

                return Json(new { success = true, authToken, expirationTime });
            }
            else
            {
                return Json(new { success = false, message = "验证码错误" });
            }
        }

        // 验证登录凭证接口
        [HttpPost]
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

        private string GenerateSmsCode()
        {
            // TODO: 生成随机短信验证码的逻辑
            return "123456";
        }

        private void SaveSmsCodeToDatabase(string phoneNumber, string smsCode)
        {
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                user = new User { PhoneNumber = phoneNumber,AddTime=DateTime.UtcNow };
                _dbContext.Users.Add(user);
            }

            user.SmsCode = smsCode;
            _dbContext.SaveChanges();
        }

        private string GetSmsCodeFromDatabase(string phoneNumber)
        {
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
            return user?.SmsCode;
        }

        private string GenerateAuthToken()
        {
            // TODO: 生成登录凭证的逻辑
            return "abc123";
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
            _dbContext.SaveChanges();
        }

        private User GetAuthTokenFromDatabase(string phoneNumber)
        {
            return _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        }
    }

}
