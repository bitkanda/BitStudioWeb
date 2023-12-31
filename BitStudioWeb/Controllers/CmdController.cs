using bitkanda.Dal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace BitStudioWeb.Controllers
{
    //[ApiController]
    [Route("api/[controller]")]
    public class CmdController : Controller
    {
        private readonly MysqlDBContext _dbContext;

        public CmdController(MysqlDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 管理员权限才允许执行脚本。
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.Admin)]
        [HttpPost("Exe")]
        public ActionResult Exe( )
        { 

            try
            {
               
                using (var streamReader = new StreamReader(HttpContext.Request.BodyReader.AsStream()))
                { 
                    string sql = streamReader.ReadToEnd();
                    var rows = _dbContext.Database.ExecuteSqlRaw(sql);
                    return Json(new { rows = rows });
 
                }
               
            }
            catch (Exception ex)
            {
                return BadRequest("Error executing SQL: " + ex.Message);
            }

        }
    }
}
