using bitkanda.Dal;
using BitStudioWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitStudioWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly MysqlDBContext _dbContext;


        public AdminController(MysqlDBContext dbContext)
        {
            _dbContext = dbContext;
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
                user = new User { PhoneNumber = phoneNumber };
                _dbContext.Users.Add(user);
            }
            
            _dbContext.SaveChanges();
        }

        [HttpPost]
        public async Task<IActionResult> Index(VerificationModel model)
        {
            // 从数据库中获取保存的验证码
            var saved = GetSmsCodeFromDatabase(model.PhoneNumber);
            var savedSmsCode = saved?.SmsCode;

            if (model.Code.ToString()  == savedSmsCode)
            {
                var sendCodeTime = saved.LastSendSmsTime;
                if (DateTime.UtcNow.Subtract(sendCodeTime).TotalSeconds > 60)
                {
                    ModelState.AddModelError("Code", "验证码过期，请重新获取！"); 
                }
                
            }
            else
            {
                ModelState.AddModelError("PhoneNumber", "验证码或手机号错误！"); 
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 将登录凭证和过期时间保存到数据库
            SaveAuthTokenToDatabase(model.PhoneNumber);

            var principal = TokenHepler.GetClaimsIdentity(model.PhoneNumber);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction("Index", "Home");
        }

        // GET: AdminController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminController/Create
        public ActionResult Create()
        {
            return View();
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
