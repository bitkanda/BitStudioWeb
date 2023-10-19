using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using bitkanda.Models;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using BitStudioWeb;
using BitStudioWeb.Models;

namespace bitkanda.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly IWebHostEnvironment _env;
        public HomeController(IStringLocalizer<HomeController> localizer, IWebHostEnvironment env)
        {
            _localizer = localizer;
            _env = env;
        }


        public static string GetDisplay()
        {
            if (!Startup.lans.ContainsKey(CultureInfo.CurrentCulture.Name))
                return "English";
            return Startup.lans[CultureInfo.CurrentCulture.Name];
        }
        public IActionResult Index(string culture)
        {
            //CultureInfo.CurrentCulture.Name
            var items=  CultureInfo.GetCultures(CultureTypes.AllCultures);


            ReleaseHelper helper = new ReleaseHelper(_env, Request);
            IEnumerable<ReleaseModel> releaseresult = helper.GetAllRelease("");
            var g = from c in releaseresult
                    group c by new { c.os }
                    into x1
                    select new ReleaseGroup
                    {
                        os = x1.Key.os,
                        releaseModel = x1.OrderByDescending(e => e.time).FirstOrDefault()
                    };

            IndexModel result = new IndexModel(g.ToList()); 
            return View(result);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            var msg = _localizer["Message"];
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
