using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace bitkanda.Controllers
{
    /// <summary>
    /// 白皮书
    /// </summary>
    public class ResourcesController : Controller
    {
        public IActionResult Index()
        {

            return View();
        }
    }
}