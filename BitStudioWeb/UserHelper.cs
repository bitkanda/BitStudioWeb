using bitkanda.Dal;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SharpToken;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
namespace BitStudioWeb
{
    public class UserHelper
    {
        private static ConcurrentDictionary<string, User> concurrentDict = new ConcurrentDictionary<string, User>();

        public static User GetCurrentUserID(string phone, MysqlDBContext _dbContext)
        {
            User u;
            // 获取值
            if (concurrentDict.TryGetValue(phone, out u))
            {
            }
            else
            {
                lock(concurrentDict)
                {
                    u = (from c in _dbContext.Users
                         where c.PhoneNumber == phone
                         select c).FirstOrDefault();
                    if (u == null)
                        throw new Exception($"{phone}不存在！请先注册！");
                    //查询余额。
                    var t = getTotalValue(u.ID, _dbContext);
                    //更新余额。
                    u.Balance = t;
                    
                    // 更新值
                    concurrentDict[phone] = u;
                } 
            }

            return u;

        }

        /// <summary>
        /// 获取可用余额。
        /// </summary>
        /// <param name="userid">用户</param>
        /// <param name="_dbContext"></param>
        /// <returns></returns>
        public static decimal getTotalValue(long userid, MysqlDBContext _dbContext)
        {
            var now = DateTime.Now;
            var result = new object();
            //查询存在未过期的，或不限过期时间的库存。
            var query = from c in _dbContext.InvTotalMaster
                        join u in _dbContext.InvUsedMaster on
                        new { c.UserID, c.ProductId, c.SkuId, c.ExpDayTime } equals
                        new { u.UserID, u.ProductId, u.SkuId, u.ExpDayTime }
                        into x1
                        from used in x1.DefaultIfEmpty() 
                        where c.UserID == userid
                          && (c.ExpDayTime == DateTime.MinValue//不限制时间。
                         || c.ExpDayTime >= now)//没有过期的。
                        && (c.Count == 0 || (used == null) || (used != null && c.Count > used.Count))//不限使用次数的，没有使用过的，没有超过限制次数的。
                        && (used == null || (used != null && c.Value > used.Value))
                        select new
                        { 
                            c.ExpDayTime,
                            c.Count,
                            c.ProductId,
                            c.SkuId,
                            c.UserID,
                            c.Value,
                            c.CreateTime,
                            c.ModifyTime,
                            UsedValue = (used == null ? 0 : used.Value),
                            UsedCount = (used == null ? 0 : used.Count),
                            UsedExpDayTime = (used == null ? DateTime.MinValue : used.ExpDayTime),
                            UsedID = (used == null ? 0 : used.ID),
                            IsExpired = ((c.Count > 0 && c.Count <= (used == null ? 0 : used.Count))
                            || (c.Value <= (used == null ? 0 : used.Value)) ||
                            (c.ExpDayTime != DateTime.MinValue && c.ExpDayTime < now))
                        };
            //sqlit不支持decimal做sum运算，放内存计算。
            var total = query.ToList().Sum(e => e.Value - e.UsedValue);
            return total;

        }
    }
}
