using bitkanda.Dal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BitStudioWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController:Controller
    {
        private readonly MysqlDBContext _dbContext;

        public OrderController(MysqlDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("getorder")]
        public ActionResult GetOrder(bool isPay)
        {
            var data = _dbContext.Orders.Where(o => o.IsPay == isPay).ToList();
            return Json(new { success = true, Result = data });
        }
     
        [HttpGet("getorder/{id:int}")]
        public ActionResult GetOrder(long id)
        {
            var data = _dbContext.Orders.Find(id);
            return Json(new { success = true, Result = data });
        }

        [Authorize]
        [HttpPost("addorder")]
        public ActionResult AddOrder(Order order)
        {
            bool data;
            if (order == null)
            {
                data = false;
            }
            else
            {
                _dbContext.Orders.Add(order);
               data=_dbContext.SaveChanges() > 0;
            }
            return Json(new { success = data });
        }

        [Authorize]
        [HttpPost("payorder")]
        public ActionResult PayOrder(long id)
        {
            bool data;
            var dbOrder = _dbContext.Orders.Find(id);
            if (dbOrder == null) { 
                data = false;
            }
            else
            {
                dbOrder.IsPay = true;
                dbOrder.PayTime = DateTime.Now;
                data=_dbContext.SaveChanges()>0;
            }
            return Json(new { success = data });

        }
    }
}
