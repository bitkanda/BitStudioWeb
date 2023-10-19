using BitStudioWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitStudioWeb.Controllers
{

    public class UpdateController : Controller
    {

        private readonly IWebHostEnvironment _env;

        public UpdateController(IWebHostEnvironment env)
        {
         
               _env = env;
        }


        public IActionResult Index(string os)
        {
            ReleaseHelper helper = new ReleaseHelper(_env, Request);
            IEnumerable<ReleaseModel> releaseresult = helper.GetAllRelease(os);

            var result = new
            {
                isError = false,
                applicationId = "com.bitstudio",
                errorMsg = "",
                release = releaseresult
            };

            return Json(result);
        }

       
    }
}
