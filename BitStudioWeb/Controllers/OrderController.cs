using bitkanda.Dal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;

namespace BitStudioWeb.Controllers
{

    public class AddOrder
    {

        /// <summary>
        /// 买家留言
        /// </summary>
        [Display(Name = "买家留言")]
        public string BuyerMsg { get; set; }


        /// <summary>
        /// 手机
        /// </summary>
       [Display(Name = "手机")]
        [Required]
        public string Mobile { get; set; }


        /// <summary>
        /// 订单明细。
        /// </summary>

        public AddOrderDetal[] Details { get; set; }

    }

    public class AddOrderDetal
    {
        
        [Required]
        public long ProductId { get; set; }


        [Required]
        public long SkuId { get; set; }

        /// <summary>
        /// 购买数量
        /// </summary>
        [Required]
        public long Qty { get; set; }

        /// <summary>
        /// 购买商品名称
        /// </summary>
        [Required]
        public string Name { get; set; }

    }

    [ApiController]
    [Route("api/[controller]")]
    public class OrderController:Controller
    {
        private readonly MysqlDBContext _dbContext;

        public OrderController(MysqlDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.Admin)]
        [HttpGet("getorder")]
        public ActionResult GetOrder(long? orderStatus)
        {
            var data = GetOrder(orderStatus, null);
            return Json(new { success = true, Result = data });
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [HttpGet("getMyOrder")]
        public ActionResult GetMyOrder(long? orderStatus)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name);
            var data = GetOrder(orderStatus, user.ID);
            return Json(new { success = true, Result = data });
        }

        private List<Order> GetOrder(long? orderStatus,long? UserID )
        {
            IQueryable<Order> data = _dbContext.Orders.AsQueryable();
            if (orderStatus != null)
                data = data.Where(o => o.OrderStatus == orderStatus.Value);
            if (UserID !=null)
                data = data.Where(e => e.UserId == UserID.Value);

            var r = data.ToList();
            foreach (var one in data)
            {
                var details = from c in _dbContext.OrderDetals
                              where c.OrderId == one.ID
                              select c;
                one.Details = details.ToList();
            }
            return   r ;
        }


        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [HttpGet("getorder/{id:int}")]
        public ActionResult GetOrder(long id)
        {
            Order data = GetOrderById(id);

            return Json(new { success = true, Result = data });
        }

        private Order GetOrderById(long id)
        {
            var data = _dbContext.Orders.Find(id);
            var details = from c in _dbContext.OrderDetals
                          where c.OrderId == id
                          select c;
            data.Details = details.ToList();
            return data;
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [HttpPost("addorder")]
        public ActionResult AddOrder(AddOrder order)
        {
            bool data;
            string msg = "";
            string innermsg = "";
            long orderId = 0;
            if (order == null)
            {
                data = false;
            }
            else
            {
                var user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name);
                if (user == null)
                {
                    data = false;
                    msg = $"用户{User.Identity.Name}不存在";
                    goto Exit;
                }
                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        //检查用户是否有未完成的订单。
                        var exitsOrder =( from c in _dbContext.Orders
                                         where c.OrderStatus == 0 && c.UserId == user.ID
                                         select c).FirstOrDefault();
                        if(exitsOrder!=null)
                        {
                            data = false;
                            msg = $"存在未付款的订单，订单号：{exitsOrder.ID},如果拍错可以先取消原来的订单再下单。";
                            goto Exit;
                        }
                        //检查传入的订单信息是否正确。
                        var newOrder = new Order
                        {
                            ModifyTime = DateTime.Now,
                            CreateTime = DateTime.Now,
                            UserId = user.ID,
                            BuyerMsg = order.BuyerMsg,
                            Mobile=order.Mobile,
                            SellerMsg = "", 
                            Qty = order.Details.Sum(e=>e.Qty)
                        };

                        _dbContext.Orders.Add(newOrder);
                        _dbContext.SaveChanges();
                        orderId = newOrder.ID;

                        foreach (var detail in order.Details)
                        {
                            var sku = (from c in _dbContext.ProductSkus
                                       where c.ID == detail.SkuId && c.ProductId == detail.ProductId
                                       select c).FirstOrDefault();
                            if (sku == null)
                            {
                                data = false;
                                msg = $"商品{sku.Name}不存在！或不可销售！";
                                throw new Exception(msg);
                            }
                            var newDetail = new OrderDetal
                            {
                                ProductId = detail.ProductId,
                                SkuId = detail.SkuId,
                                Count = sku.Count,
                                CreateTime = DateTime.Now,
                                ExpDay = sku.ExpDay,
                                Name = sku.Name,
                                OrderId = newOrder.ID,
                                PayAmount = sku.Price * detail.Qty,
                                Price = sku.Price,
                                Qty = detail.Qty,
                                PromotionAmount = 0,
                                RetailAmount = sku.Price * detail.Qty,
                                Value = sku.Value
                            };

                            newOrder.RetailAmount += newDetail.RetailAmount;
                            newOrder.PayAmount += newDetail.PayAmount;
                            _dbContext.OrderDetals.Add(newDetail);
                        }

                        data = _dbContext.SaveChanges() > 0;

                        transaction.Commit(); // 提交事务
                    }
                    catch (Exception error)
                    {
                        data = false;
                        msg = error.Message;
                        innermsg = error.InnerException?.Message;
                        transaction.Rollback();
                    }

                }
            }
        Exit:
            Order orderData = null;
            if(orderId>0)
                orderData = GetOrderById(orderId);
            return Json(new { success = data,orderId= orderId,msg=msg,innermsg= innermsg,data= orderData });
        }


       public class PayOrderModel 
        {
            [Required]
        public long orderId { get; set; }
            [Required]
            public string PayOrderNo { get; set; }
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.Admin)]
        [HttpPost("payorder")]
        public ActionResult PayOrder(PayOrderModel PayOrder)
        {
          
            var user=  _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name);
            var msg = "";
            bool data;
            var dbOrder = _dbContext.Orders.Find(PayOrder.orderId);
            if (dbOrder == null) { 
                data = false;
                msg = "订单不存在！";
            }
            else
            {
                
                dbOrder.PayTime = DateTime.Now;
                dbOrder.ModifyTime = DateTime.Now;
                dbOrder.PayUserId = user.ID;
                dbOrder.PayOrderNo = PayOrder. PayOrderNo;
                dbOrder.OrderStatus = 1;
                data =_dbContext.SaveChanges()>0;
            }
            return Json(new { success = data,msg= msg });

        }
    }
}
