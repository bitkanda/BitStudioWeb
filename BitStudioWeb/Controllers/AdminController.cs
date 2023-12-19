using bitkanda.Dal;
using BitStudioWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitStudioWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly MysqlDBContext _dbContext;
        private readonly IConfiguration _configuration;

        public AdminController(MysqlDBContext dbContext,IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        // GET: AdminController
        public ActionResult Index()
        {
            return View();
        }

        private User GetSmsCodeFromDatabase(string phoneNumber)
        {
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
            return user;
        }


        private void SaveAuthTokenToDatabase(string phoneNumber)
        {
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                user = new User { PhoneNumber = phoneNumber, Role = RoleConst.User };
                _dbContext.Users.Add(user);
            }
            else
            {

            }
            user.Role= TokenHepler.GetUserRole(_configuration,phoneNumber);
            _dbContext.SaveChanges();
        }

      

        [HttpPost]
        public async Task<IActionResult> Index(VerificationModel model)
        {
            if(model==null)
            {
                ModelState.AddModelError("PhoneNumber", "手机号错误！");
                goto Check;
            }

             

            // 从数据库中获取保存的验证码
            var saved = GetSmsCodeFromDatabase(model.PhoneNumber);

            if (saved == null)
            {
                ModelState.AddModelError("PhoneNumber", "请点击获取验证码！");
                goto Check;
            }

            var savedSmsCode = saved?.SmsCode;

            if (string.IsNullOrWhiteSpace(savedSmsCode))
            {
                ModelState.AddModelError("Code", "验证码没有发送成功，请联系客服！");
                goto Check;
            }

            if (model.Code.ToString()  == savedSmsCode)
            {
                var sendCodeTime = saved.LastSendSmsTime;
                if (DateTime.UtcNow.Subtract(sendCodeTime).TotalSeconds > 60)
                {
                    ModelState.AddModelError("Code", "验证码过期，请重新获取！");
                    goto Check;
                }
                
            }
            else
            {
                ModelState.AddModelError("PhoneNumber", "验证码错误！");
                goto Check;
            }

            Check:
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 将登录凭证和过期时间保存到数据库
            SaveAuthTokenToDatabase(model.PhoneNumber);
            var role= TokenHepler.GetUserRole(_configuration,model.PhoneNumber);
            var principal = TokenHepler.GetClaimsIdentity(model.PhoneNumber, role);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction("Details", "admin");
        }


       
        [Authorize(AuthenticationSchemes = "Cookies")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: AdminController/Details/5
        [Authorize(AuthenticationSchemes = "Cookies")]
        public ActionResult Details()
        {
            string phoneNumber = HttpContext.User.Identity.Name;
            User user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
            return View(user);
        }



        // GET: AdminController/Create
        [Authorize(AuthenticationSchemes = "Cookies",Roles =RoleConst.Admin)]
        public ActionResult CreateAdmin()
        {
            return Content("超级管理员");
        }

        [Authorize(AuthenticationSchemes = "Cookies", Roles = RoleConst.User)]
        public ActionResult CreateUser()
        {
            return Content("普通用户");
        }


        // POST: AdminController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdminController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AdminController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdminController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AdminController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
