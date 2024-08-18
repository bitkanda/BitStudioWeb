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
using Microsoft.Extensions.Configuration;
using BitStudioWeb.Controllers;
using Microsoft.AspNetCore.Hosting;
namespace BitStudioWeb.ChatLogic
{
    public class GPTImageStreamWriterResult : ActionResult
    {
        private readonly Stream _responseBody;
        private readonly MysqlDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly string UserIdentityName = "";
        private readonly string RequestBody = "";
        public GPTImageStreamWriterResult(Stream responseBody,
            MysqlDBContext dbContext,
            IConfiguration configuration,
            string userIdentityName,
            string requestBody
            )
        {
            _responseBody = responseBody;
            _dbContext = dbContext;
            _configuration = configuration;
            UserIdentityName = userIdentityName;
            RequestBody = requestBody;


        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            //JObject tokeninfo = Newtonsoft.Json.JsonConvert.DeserializeObject(RequestBody) as JObject;
            //var model = tokeninfo.Value<string>("model");
            //var stream = tokeninfo.Value<bool?>("stream");
            //if (stream == null)
            //    stream = false;

            await AsyncPost(RequestBody, _responseBody);


        }

        #region gpt

        private async Task AsyncPost(string requestContent, Stream outputStream)
        {
            //判断是否欠费，欠费的用户，或没有余额的用户不能调用.
            var user = UserHelper.GetCurrentUserID(UserIdentityName, _dbContext);
            if (user.Balance == 0 || user.IsDebt)
            {
                using (System.IO.MemoryStream ms = new MemoryStream())
                {
                    StreamWriter sr = new StreamWriter(ms);
                    sr.WriteLine("{\"error\":{\"message\": \"余额不足,请先充值哦！\"}}");
                    sr.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    await WriteMsg(ms, outputStream, requestContent);
                    return;
                }

            }

            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            var token = _configuration["GPTToken"];
            var url = _configuration["GPTImageUrl"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var chatGptResponse = await client.PostAsync(url, content);

            var responseStream = await chatGptResponse.Content.ReadAsStreamAsync();

            await WriteMsg(responseStream, outputStream, requestContent);

            return;
        }

        /// <summary>
        /// 先使用再扣费原则，前提是账户有余额。
        /// </summary>
        /// <param name="responseStream"></param>
        /// <param name="outputStream"></param>
        /// <param name="stream"></param>
        /// <param name="requestContent"></param>
        /// <returns></returns>
        private async Task WriteMsg(Stream responseStream, Stream outputStream
            , string requestContent)
        {
            string Body = "";
            List<String> messages = new List<string>();
            using (StreamReader sr = new StreamReader(responseStream))
            {
                using (StreamWriter sw = new StreamWriter(outputStream))
                { 
                        Body = sr.ReadToEnd();
                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 异步写入消息： { Body}");
                        await sw.WriteLineAsync(Body);
                        await sw.FlushAsync(); 
                }
            }

            //计费逻辑
            {
                JObject tokeninfo = Newtonsoft.Json.JsonConvert.DeserializeObject(Body) as JObject;
                //判断是否有错，如果有错，不计费。
                if (!tokeninfo.ContainsKey("error"))
                {  
                    var id = tokeninfo.Value<long>("created");
                    JObject requestInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(requestContent) as JObject;
                    var quality = requestInfo.Value<string>("quality");
                    var model = requestInfo.Value<string>("model");
                    var size = requestInfo.Value<string>("size");
                    var n = requestInfo.Value<int>("n");
                    await AddUsedLogs(id.ToString(), model, quality, size, n);
                }

            }

        }

        /// <summary>
        /// 扣费,成功返回true.
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="total_power">要扣掉的算力</param>
        private async Task<KeyValuePair<bool, string>> UpdateInvUsedMaster(User user, decimal total_power)
        {
            KeyValuePair<bool, string> result;
            var now = DateTime.Now;
            lock (user)
            {


                //查询存在未过期的，或不限过期时间的库存。
                var query = from c in _dbContext.InvTotalMaster
                            join u in _dbContext.InvUsedMaster on
                            new { c.UserID, c.ProductId, c.SkuId, c.ExpDayTime } equals
                            new { u.UserID, u.ProductId, u.SkuId, u.ExpDayTime }
                            into x1
                            from used in x1.DefaultIfEmpty()
                            where c.UserID == user.ID
                            && (c.ExpDayTime == DateTime.MinValue//不限制时间。
                            || c.ExpDayTime >= now)//没有过期的。
                            && (c.Count == 0 || (used == null) || (used != null && c.Count > used.Count))//不限使用次数的，没有使用过的，没有超过限制次数的。
                            && (used == null || (used != null && c.Value > used.Value))
                            select new InvSearch(
                                c.ExpDayTime,
                                c.Count,
                                c.ProductId,
                                c.SkuId,
                                c.UserID,
                                c.Value,
                                c.CreateTime,
                                c.ModifyTime,
                               (used == null ? 0 : used.Value),
                               (used == null ? 0 : used.Count),
                               (used == null ? DateTime.MinValue : used.ExpDayTime),
                               (used == null ? 0 : used.ID),
                               false
                            );
                var q = query.ToList();
                foreach (var one in q)
                {
                    //是否失效。
                    one.IsExpired = (one.Count > 0 && one.Count <= one.UsedCount) || (one.Value <= one.UsedCount) ||
                        (one.ExpDayTime != DateTime.MinValue && one.ExpDayTime < now);
                }
                if (q.Count == 0)
                {
                    //调用接口前先判断有余额的，说明是有充值了的。存在时间差才会运行到这里。

                    result = new KeyValuePair<bool, string>(false, "您的算力不足，请及时充值！");
                    user.IsDebt = true;
                    //内存扣减余额。
                    user.Balance = user.Balance - total_power;
                    goto Exit;
                }
                //优先从有过期时间的扣。
                var ExpDayTimeQuery = q.Where(e => e.ExpDayTime >= now).OrderBy(e => e.ExpDayTime).ToList();
                var NotExpDayTimeQuery = q.Where(e => e.ExpDayTime < now).OrderBy(e => e.CreateTime).ToList();
                var all = new List<InvSearch>();
                all.AddRange(ExpDayTimeQuery);
                all.AddRange(NotExpDayTimeQuery);
                foreach (var one in all)
                {
                    if (total_power == 0)
                        break;
                    //计算可用余额。
                    var readValue = one.Value - one.UsedValue;
                    if (readValue >= total_power)
                    {
                        one.UsedValue = one.UsedValue + total_power;
                        total_power = total_power - total_power;
                    }
                    else
                    {
                        //可用余额不够扣，直接扣掉可用余额，然后再从其它的产品上扣。
                        one.UsedValue = one.UsedValue + readValue;
                        total_power = total_power - readValue;
                    }
                    one.IsEdit = true;
                }
                //余额不够扣，直接返回余额不足。
                if (total_power > 0)
                {
                    result = new KeyValuePair<bool, string>(false, "您的算力不足支付账单，请及时充值！");
                    user.IsDebt = true;
                    goto Exit;
                }
                //开始更新库存。
                var update = q.Where(e => e.IsEdit).ToList();
                foreach (var row in update)
                {
                    if (row.UsedID == 0)
                    {
                        //新增库存记录。
                        _dbContext.InvUsedMaster.Add(new InvUsedMaster
                        {
                            Count = row.Count > 0 ? 1 : 0,//套餐有限制次数，需要记录已经调用的次数。
                            CreateTime = now,
                            ExpDayTime = row.ExpDayTime,
                            ProductId = row.ProductId,
                            SkuId = row.SkuId,
                            UserID = user.ID,
                            Value = row.UsedValue,

                        });
                    }
                    else
                    {
                        var used = (from c in _dbContext.InvUsedMaster
                                    where c.ID == row.UsedID
                                    select c).FirstOrDefault();
                        if (row.Count > 0)
                        {
                            //使用次数累加
                            used.Count = used.Count + 1;
                        }
                        //修改库存数量。
                        used.Value = row.UsedValue;
                        used.ModifyTime = now;
                    }
                }
                //提交库存修改。
                _dbContext.SaveChanges();
                result = new KeyValuePair<bool, string>(true, "");
            }
        Exit:
            return await Task.FromResult(result);
        }





        /// <summary>
        /// 写入算力流水
        /// </summary>
        /// <param name="id"></param>
        /// <param name="object"></param>
        /// <param name="model"></param>
        /// <param name="prompt_tokens"></param>
        /// <param name="completion_tokens"></param>
        /// <param name="total_tokens"></param>
        /// <returns></returns>
        private async Task<KeyValuePair<bool, string>> AddUsedLogs(string id, string model, string Quality, string Resolution,int n)
        {
            var user = UserHelper.GetCurrentUserID(UserIdentityName, _dbContext);
            Quality = FormatQuality(model, Quality);
            //是否扣费成功。
            bool IsPay = false;
            var @object = $"quality={Quality},size={Resolution},n={n}";
            //写入消费记录。
            var l = new UsedLog
            {
                CompletionTokens = 0,
                CreateTime = DateTime.Now,
                Model = model,
                Object = @object,
                PromptTokens = n,
                TotalTokens = 0,
                RequestID = id,
                UserID = user.ID,
                IsPay = IsPay
            };
            await _dbContext.UsedLogs.AddAsync(l);
            await _dbContext.SaveChangesAsync();
           
                //把token换算成算力，进行扣费。
                var (isSuccess, powerValue, Msg) = GetPowerValue(model, Quality, Resolution,n);
                if (!isSuccess)
                {
                    return await Task.FromResult(new KeyValuePair<bool, string>(isSuccess, Msg));
                }
                var r = await UpdateInvUsedMaster(user, powerValue);
                IsPay = r.Key;
            l.PowerValue = powerValue;
            if (r.Key == false)
                    return await Task.FromResult(r);
                else
                {
                    l.IsPay = true;
                }
            
            await _dbContext.SaveChangesAsync();


            return await Task.FromResult(new KeyValuePair<bool, string>(true, ""));



        }

        public class ModelPrice
        {
            public string Model { get; set; }

            public decimal InputPrice { get; set; }
            public decimal InputTokens { get; set; }

            public decimal OutPrice { get; set; }
            public decimal OutTokens { get; set; }
        }

        public class ImagePrice
        {
            public string Model { get; set; }

            public string Quality { get; set; }


            public string Resolution { get; set; }

            public decimal Price { get; set; }

        }


        /// <summary>
        /// 美元换算人民币汇率。
        /// </summary>
        private const decimal USDConvertYan = 7.1944m;

        /// <summary>
        /// 人民币转换算力单位。
        /// </summary>
        private const decimal YanConvertPower = 1000m;

        /// <summary>
        /// 根据token换算为算力。
        /// </summary>
        /// <param name="model">模型</param>
        /// <param name="prompt_tokens">输入token数量</param>
        /// <param name="completion_tokens">输出token数量</param>
        /// <returns></returns>
        private (bool, decimal, string) GetPowerValue(string model, string Quality, string Resolution,int n)
        {
            var PriceList = new List<ImagePrice>
            {
                new ImagePrice{ Model="dall-e-3", Price=0.040m, Quality="Standard", Resolution="1024x1024"} ,
                new ImagePrice{ Model="dall-e-3", Price=0.080m, Quality="Standard", Resolution="1024x1792" } ,
                new ImagePrice{ Model="dall-e-3", Price=0.080m, Quality="Standard", Resolution="1792x1024" } ,
                new ImagePrice{ Model="dall-e-3", Price=0.080m, Quality="HD", Resolution="1024x1024"} ,
                new ImagePrice{ Model="dall-e-3", Price=0.120m, Quality="HD", Resolution="1024x1792" } ,
                new ImagePrice{ Model="dall-e-3", Price=0.120m, Quality="HD", Resolution="1792x1024" } ,
                new ImagePrice{ Model="dall-e-2", Price=0.020m, Quality="", Resolution="1024x1024" } ,
                new ImagePrice{ Model="dall-e-2", Price=0.018m, Quality="", Resolution="512x512" } ,
                new ImagePrice{ Model="dall-e-2", Price=0.016m, Quality="", Resolution="256x256" } ,

                 
                //在数据库里维护好价格表。
            };
            //Quality = FormatQuality(model, Quality);
            var query = (from c in PriceList
                         where model.Contains(c.Model, StringComparison.OrdinalIgnoreCase)
                         && Quality == c.Quality && c.Resolution == Resolution
                         select c).FirstOrDefault();
            if (query == null)
            {
                //默认使用第一种价格表。
                query = PriceList.FirstOrDefault();
                //return (false, 0, $"{model}未设置价格，请联系运营！");
            }

            //把美元转换成人民币。
            var resultYan = n * query.Price * USDConvertYan;

            //人民币转换成算力.
            var resultPower = resultYan * YanConvertPower;
            return (true, resultPower, "成功");
        }

        private static string FormatQuality(string model, string Quality)
        {
            if (model.Equals("dall-e-3", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Quality))
                Quality = "Standard";
            if (model.Equals("dall-e-2", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Quality))
                Quality = "";
            return Quality;
        }

        /// <summary>
        /// 计算花费算力。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="model"></param>
        /// <param name="isResponse"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static int NumTokensFromMessages(Newtonsoft.Json.Linq.JArray data,
            string model = "gpt-3.5-turbo-0613", bool isResponse = false)
        {
            GptEncoding encoding = null;
            try
            {
                encoding = GptEncoding.GetEncodingForModel(model);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("Warning: model not found. Using cl100k_base encoding.");
                encoding = GptEncoding.GetEncoding("cl100k_base");
            }

            int tokensPerMessage = 0;
            int tokensPerName = 0;

            if (new List<string> { "gpt-3.5-turbo-0613", "gpt-3.5-turbo-16k-0613", "gpt-4-0314", "gpt-4-32k-0314", "gpt-4-0613", "gpt-4-32k-0613" }.Contains(model))
            {
                tokensPerMessage = 3;
                tokensPerName = 1;
            }
            else if (model == "gpt-3.5-turbo-0301")
            {
                tokensPerMessage = 4;
                tokensPerName = -1;
            }
            else if (model.Contains("gpt-3.5-turbo"))
            {
                Console.WriteLine("Warning: gpt-3.5-turbo may update over time. Returning num tokens assuming gpt-3.5-turbo-0613.");
                return NumTokensFromMessages(data, "gpt-3.5-turbo-0613", isResponse);
            }
            else if (model.Contains("gpt-4"))
            {
                Console.WriteLine("Warning: gpt-4 may update over time. Returning num tokens assuming gpt-4-0613.");
                return NumTokensFromMessages(data, "gpt-4-0613", isResponse);
            }
            else
            {
                throw new NotImplementedException($"num_tokens_from_messages() is not implemented for model {model}. See https://github.com/openai/openai-python/blob/main/chatml.md for information on how messages are converted to tokens.");
            }

            int numTokens = 0;
            foreach (Newtonsoft.Json.Linq.JObject message in data)
            {
                if (!isResponse)
                    numTokens += tokensPerMessage;
                foreach (KeyValuePair<string, Newtonsoft.Json.Linq.JToken> pair in message)
                {
                    numTokens += encoding.Encode(pair.Value.ToString()).Count;
                    if (!isResponse)
                        if (pair.Key == "name")
                        {
                            numTokens += tokensPerName;
                        }
                }
            }
            if (!isResponse)
                numTokens += 3;
            return numTokens;
        }

        #endregion

    }
}
