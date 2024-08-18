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
            //按下单时间排序。
            var r = data.OrderByDescending(e=>e.ID).ToList();
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
                                ImgUrl= sku.ImgUrl,
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
            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                var PayUser = UserHelper.GetCurrentUserID(User.Identity.Name, _dbContext);
                
                var msg = "";
                bool data;
                var dbOrder = _dbContext.Orders.Find(PayOrder.orderId);
                if (dbOrder == null)
                {
                    data = false;
                    msg = "订单不存在！";
                }
                else
                {

                    dbOrder.PayTime = DateTime.Now;
                    dbOrder.ModifyTime = DateTime.Now;
                    dbOrder.PayUserId = PayUser.ID;
                    dbOrder.PayOrderNo = PayOrder.PayOrderNo;
                    dbOrder.OrderStatus = 1;
                    data = _dbContext.SaveChanges() > 0;

                    //更新库存。
                    var details =( from c in _dbContext.OrderDetals
                                 where c.OrderId == PayOrder.orderId
                                 select c).ToList();
                     
                    foreach (var one in details)
                    {
                        var ExpDayTime = DateTime.MinValue;
                        if(one.ExpDay>0)
                        {
                            var t = dbOrder.PayTime.Value.ToString("yyyy-MM-dd"); 
                            ExpDayTime= DateTime.Parse(t).AddDays((double)one.ExpDay);
                        }

                        InvTotalMaster m = null;
                        m=( from c in _dbContext.InvTotalMaster
                                where c.UserID == dbOrder.UserId && c.ProductId == one.ProductId
                                && c.SkuId == one.SkuId && c.ExpDayTime == ExpDayTime && c.Count == one.Count
                                select c).FirstOrDefault();
                        if(m==null)
                        {
                            m = new InvTotalMaster
                            {
                                Count = one.Count,
                                CreateTime = dbOrder.PayTime.Value,
                                ExpDayTime = ExpDayTime,
                                ProductId = one.ProductId, 
                                SkuId = one.SkuId,
                                UserID = dbOrder.UserId,
                                Value = one.Value*one.Qty,
                                ModifyTime= DateTime.MinValue
                            };
                            _dbContext.InvTotalMaster.Add(m);
                        }
                        else
                        {
                            m.Value += one.Value * one.Qty;
                            m.ModifyTime = dbOrder.PayTime.Value;
                        }
                       
                        _dbContext.SaveChanges();
                        var orderUser= _dbContext.Users.Find(dbOrder.UserId);
                        var user = UserHelper.GetCurrentUserID(orderUser.PhoneNumber, _dbContext);
                       
                        lock (user)
                        { 
                            //更新欠费状态。这个后期再优化。
                            user.IsDebt = false; 
                            //更新余额。
                            user.Balance+= one.Value * one.Qty;
                        }
                    }

                  
                    transaction.Commit(); // 提交事务
                }
                return Json(new { success = data, msg = msg });
            }
        }


        /// <summary>
        /// 取消我的未付款订单。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [HttpGet("cancelMyOrder")]
        public ActionResult cancelMyOrder(long id)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == User.Identity.Name); 
            var msg = "";
            bool data;
            var dbOrder =( from c in _dbContext.Orders
                          where c.ID == id
                          select c).FirstOrDefault();

            if (dbOrder == null)
            {
                data = false;
                msg = "订单不存在！";
                goto Exit;
            }
            if (dbOrder.OrderStatus!=0)
            {
                data = false;
                msg = "只有未付款的订单才能取消！";
                goto Exit;
            }
            
            dbOrder.ModifyTime = DateTime.Now; 
            dbOrder.OrderStatus = 5;
            data = _dbContext.SaveChanges() > 0;
            data = true;
            msg = "成功！";
            Exit:
            return Json(new { success = data, msg = msg }); 
        }

    }
}
