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
            //long UserID = 0;
            // 获取值
            if (concurrentDict.TryGetValue(phone, out u))
            {
                //UserID = u.ID;
                //true;
            }
            else
            {

                u = (from c in _dbContext.Users
                     where c.PhoneNumber == phone
                     select c).FirstOrDefault();
                //UserID = u.ID;
                // 更新值
                concurrentDict[phone] = u;
            }

            return u;

        }
    }
}
