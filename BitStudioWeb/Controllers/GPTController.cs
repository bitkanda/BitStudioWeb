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

namespace BitStudioWeb.Controllers
{
    public class StreamWriterResult : ActionResult
    {
        private readonly Stream _responseBody;

        public StreamWriterResult(Stream responseBody)
        {
            _responseBody = responseBody;
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            var writer = new StreamWriter(_responseBody) ;
            int i = 0;
            while (i < 30) // 无限循环，模拟持续的服务器端发送
            {
                var line = $"data: { DateTimeOffset.Now },i={i}";
                Debug.WriteLine(line);
                await writer.WriteLineAsync(line);
                await writer.FlushAsync();
                await Task.Delay(1000); // 每秒发送一次
                i++;
            }


        }
    }


    public class GPTStreamWriterResult : ActionResult
    {
        private readonly Stream _responseBody;
        private readonly MysqlDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly string UserIdentityName = "";
        private readonly string RequestBody = "";
        public  GPTStreamWriterResult(Stream responseBody, 
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
            JObject tokeninfo = Newtonsoft.Json.JsonConvert.DeserializeObject(RequestBody) as JObject;
            var model = tokeninfo.Value<string>("model");
            var stream = tokeninfo.Value<bool?>("stream");
            if (stream == null)
                stream = false;

            await AsyncPost(RequestBody, stream.Value, _responseBody);
            //var writer = new StreamWriter(_responseBody);
            //int i = 0;
            //while (i < 30) // 无限循环，模拟持续的服务器端发送
            //{
            //    await writer.WriteLineAsync($"data: { DateTimeOffset.Now },i={i}");
            //    await writer.FlushAsync();
            //    await Task.Delay(1000); // 每秒发送一次
            //    i++;
            //}


        }

        #region gpt

        private async Task AsyncPost(string requestContent, bool stream, Stream outputStream)
        {
            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            var token = _configuration["GPTToken"];
            var url = _configuration["GPTUrl"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var chatGptResponse = await client.PostAsync(url, content);

            //if (chatGptResponse.IsSuccessStatusCode)
            //{
            var responseStream = await chatGptResponse.Content.ReadAsStreamAsync();

            await WriteMsg(responseStream, outputStream, stream, requestContent);

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
        private async Task WriteMsg(Stream responseStream, Stream outputStream,
            bool stream, string requestContent)
        {
            string Body = "";
            List<String> messages = new List<string>();
            using (StreamReader sr = new StreamReader(responseStream))
            {
                using (StreamWriter sw = new StreamWriter(outputStream))
                {
                    if (stream)
                    {
                        string line;
                        while ((line = await sr.ReadLineAsync()) != null)
                        {
                            if (line.StartsWith("data:"))
                            {
                                var lineBody = line.TrimStart(new char[] { 'd', 'a', 't', 'a', ':' });
                                messages.Add(lineBody);
                            }
                            else if (line.StartsWith("{\"error\""))
                            {
                                //如果出现错误，直接返回。
                                await sw.WriteLineAsync(line);
                                await sw.FlushAsync();
                                return;
                            }

                            Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 异步写入消息： { line}");
                            await sw.WriteLineAsync(line);
                            await sw.FlushAsync();
                        }
                    }
                    else
                    {
                        Body = sr.ReadToEnd();
                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 异步写入消息： { Body}");
                        await sw.WriteLineAsync(Body);
                        await sw.FlushAsync();
                    }


                }
            }

            if (stream)
            {
                Body = messages.FirstOrDefault();
                var Content = "";
                JObject tokeninfo = Newtonsoft.Json.JsonConvert.DeserializeObject(Body) as JObject;
                var id = tokeninfo.Value<string>("id");
                var @object = tokeninfo.Value<string>("object");
                var model = tokeninfo.Value<string>("model");
                if (messages.Count > 0)
                    messages.RemoveAt(messages.Count - 1);
                foreach (var one in messages)
                {
                    JObject i = Newtonsoft.Json.JsonConvert.DeserializeObject(one) as JObject;
                    JArray choices = i["choices"] as JArray;
                    foreach (JObject c in choices)
                    {
                        JObject delta = c["delta"] as JObject;
                        string content = delta.Value<string>("content");
                        Content += content;
                    }
                }
                var m = new JArray();
                var cobject = new JObject();
                cobject["content"] = Content;
                m.Add(cobject);
                var completion_tokens = NumTokensFromMessages(m, model, true);


                JObject requestInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(requestContent) as JObject;

                var requestMssages = requestInfo["messages"] as Newtonsoft.Json.Linq.JArray;
                var prompt_tokens = NumTokensFromMessages(requestMssages, model);
                var total_tokens = prompt_tokens + completion_tokens;

                await AddUsedLogs(id, @object, model, prompt_tokens, completion_tokens, total_tokens);
            }
            else
            {
                JObject tokeninfo = Newtonsoft.Json.JsonConvert.DeserializeObject(Body) as JObject;
                var id = tokeninfo.Value<string>("id");
                var @object = tokeninfo.Value<string>("object");
                var model = tokeninfo.Value<string>("model");
                var usage = tokeninfo["usage"] as JObject;
                var prompt_tokens = usage.Value<int>("prompt_tokens");
                var completion_tokens = usage.Value<int>("completion_tokens");
                var total_tokens = usage.Value<int>("total_tokens");

                await AddUsedLogs(id, @object, model, prompt_tokens, completion_tokens, total_tokens);

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
                    result = new KeyValuePair<bool, string>(false, "您的算力不足，请及时充值！");
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
                    var readValue = one.Value - one.UsedValue;
                    if (readValue >= total_power)
                    {
                        one.UsedValue = one.UsedValue + total_power;
                        total_power = total_power - total_power;
                    }
                    else
                    {
                        one.UsedValue = one.UsedValue + readValue;
                        total_power = total_power - readValue;
                    }
                    one.IsEdit = true;
                }

                if (total_power > 0)
                {
                    result = new KeyValuePair<bool, string>(false, "您的算力不足支付账单，请及时充值！");
                    goto Exit;
                }
                //开始更新库存。
                var update = q.Where(e => e.IsEdit).ToList();
                foreach (var row in update)
                {
                    if (row.UsedID == 0)
                    {
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
                            used.Count = used.Count + 1;
                        }
                        used.Value = row.UsedValue;
                        used.ModifyTime = now;
                    }
                }
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
        private async Task<KeyValuePair<bool, string>> AddUsedLogs(string id, string @object, string model, int prompt_tokens,
            int completion_tokens, int total_tokens)
        {
            var user = UserHelper.GetCurrentUserID(UserIdentityName, _dbContext);


            //写入消费记录。
            await _dbContext.UsedLogs.AddAsync(new UsedLog
            {
                CompletionTokens = completion_tokens,
                CreateTime = DateTime.Now,
                Model = model,
                Object = @object,
                PromptTokens = prompt_tokens,
                TotalTokens = total_tokens,
                RequestID = id,
                UserID = user.ID,
            });
            if (total_tokens > 0)
            {
                //把token换算成算力，进行扣费。
                var (isSuccess, powerValue, Msg) = GetPowerValue(model, prompt_tokens, completion_tokens);
                if (!isSuccess)
                {
                    return await Task.FromResult(new KeyValuePair<bool, string>(isSuccess, Msg));
                }
                var r = await UpdateInvUsedMaster(user, powerValue);
                if (r.Key == false)
                    return await Task.FromResult(r);
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
        private (bool, decimal, string) GetPowerValue(string model, int prompt_tokens, int completion_tokens)
        {
            var PriceList = new List<ModelPrice>
            {
                new ModelPrice{ Model="gpt-3.5",InputPrice=0.0010m,InputTokens=1000m,OutPrice=0.0020m,OutTokens=1000m } ,
                new ModelPrice{ Model="gpt-4",InputPrice=0.01m,InputTokens=1000m,OutPrice=0.030m,OutTokens=1000m } ,

                 
                //在数据库里维护好价格表。
            };
            var query = (from c in PriceList
                         where model.Contains(c.Model, StringComparison.OrdinalIgnoreCase)
                         select c).FirstOrDefault();
            if (query == null)
            {
                return (false, 0, $"{model}未设置价格，请联系运营！");
            }
            var inputPrice = query.InputPrice / query.InputTokens;
            var outPrice = query.OutPrice / query.OutTokens;
            //计算费用，单位美元。
            var amount = (inputPrice * prompt_tokens + outPrice * completion_tokens);
            //把美元转换成人民币。
            var resultYan = amount * USDConvertYan;

            //人民币转换成算力.
            var resultPower = resultYan * YanConvertPower;
            return (true, resultPower, "成功");
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

    public class GPTController : Controller
    {
        private readonly MysqlDBContext _dbContext;
        private readonly IConfiguration _configuration;
        public GPTController(MysqlDBContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.Admin)]
        [Route("/v1/chat/completions")]
        [HttpPost]
        public Task<ActionResult> ChatCompletions()
        {
            try
            {
                String Body = "";
                using (var streamReader = new StreamReader(HttpContext.Request.BodyReader.AsStream()))
                {
                    Body = streamReader.ReadToEnd();
                }
              

                //var messages = tokeninfo["messages"] as Newtonsoft.Json.Linq.JArray;
                // Get encoding by model name
                //var encoding = GptEncoding.GetEncodingForModel(token.Model);
                //var encoded = encoding.Encode(token.Text);
                //var n = NumTokensFromMessages(messages, model);
                /*
                  注意：
                  这个是计算上下文的token(prompt_tokens)数量算法，
                  并不包含回复的token(completion_tokens) 
                  计算回复的token ：直接使用函数encoding.Encode(回复字符串).Count
                  总消耗token(total_tokens)= prompt_tokens+completion_tokens
                */
                //写入消费记录。
                //_dbContext.UsedLogs.Add(new UsedLog 
                //{

                //});
                //_dbContext.SaveChanges();

                //当余额够扣的时候，执行转发操作。
                return PostData(Body);
            }
            finally
            {
                Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 完成接口调用");
            }
            // return Json(new { success = true, prompt_tokens= n,model= model });
        }




        private async Task<ActionResult> PostDataBak(string requestContent)
        {
            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");
            using var client = new HttpClient();
            var token = _configuration["GPTToken"];
            var url = _configuration["GPTUrl"];
            // 设置 Authorization 头部
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var chatGptResponse = await client.PostAsync(url, content);
            var stream = await chatGptResponse.Content.ReadAsStreamAsync();
            return File(stream, "text/event-stream");

            //if (chatGptResponse.IsSuccessStatusCode)
            //{
            //    var stream = await chatGptResponse.Content.ReadAsStreamAsync();
            //    return File(stream, "application/octet-stream");
            //    //Response.Headers.Add("Content-Type", "text/event-stream");
            //    //var stream = await chatGptResponse.Content.ReadAsStreamAsync();
            //    //var buffer = new byte[1024];
            //    //int bytesRead;
            //    //while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            //    //{
            //    //    var chatResponseChunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            //    //    await Response.WriteAsync($"data: {chatResponseChunk}\n\n");
            //    //    await Response.Body.FlushAsync();
            //    //}
            //}
            //else
            //{
            //    string errorResponse = await chatGptResponse.Content.ReadAsStringAsync();
            //    return BadRequest($"无法获取文件流,代码为：{chatGptResponse.StatusCode}");
            //}
        }
        private async Task<ActionResult> PostData(string requestContent)
        {

            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.Headers["Cache-Control"] = "no-cache";
            HttpContext.Response.Headers["Connection"] = "keep-alive";
            HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*"; // 允许跨域 
            var result= new GPTStreamWriterResult(HttpContext.Response.Body, _dbContext, _configuration, User.Identity.Name,
                requestContent);
            //var result = new StreamWriterResult(HttpContext.Response.Body);
            return await Task.FromResult(result);

            // memStream.Seek(0, SeekOrigin.Begin); 
            // return new  (memStream, "text/event-stream"); 
            //var outputStream = Response.Body; 
            //var t = Task.Run(async () =>
            //  {
            //      await AsyncPost(requestContent, stream, outputStream);
            //    // 在这里可以继续执行其他操作
            //}); 
            //return File(outputStream, "text/event-stream"); 
        }

        private async Task WriteTimeData(Stream writer)
        {

            while (true)
            {
                var line = System.Text.Encoding.UTF8.GetBytes($"data: {DateTime.Now}");
                await writer.WriteAsync(line, 0, line.Length);
                //await writer.WriteLineAsync();
                //
                await writer.FlushAsync();
                await Task.Delay(1000); // 异步等待1秒

            }

        }
        //[Authorize]
        //[HttpPost("test")]
        //public ActionResult Test()
        //{
        //    // 获取当前用户的标识
        //    var userIdentity = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    return Ok(new { result= "Authorize" });
        //}


    }

    internal class InvSearch
    {
        public DateTime ExpDayTime { get; }
        public int Count { get; }
        public long ProductId { get; }
        public long SkuId { get; }
        public long UserID { get; }
        public decimal Value { get; }
        public DateTime CreateTime { get; }
        public DateTime ModifyTime { get; }
        public decimal UsedValue { get; set; }
        public int UsedCount { get; }
        public DateTime UsedExpDayTime { get; }
        public long UsedID { get; }
        /// <summary>
        /// 是否需要更新数据库已使用库存。
        /// </summary>
        public bool IsEdit { get; set; }

        /// <summary>
        /// 是否失效。1 失效，0有效。
        /// </summary>
        public bool IsExpired { get; internal set; }

        public InvSearch(DateTime expDayTime, int count, long productId, long skuId, long userID, decimal value,
            DateTime createTime, DateTime modifyTime, decimal usedValue, int usedCount,
            DateTime usedExpDayTime, long usedID, bool isExpired)
        {
            ExpDayTime = expDayTime;
            Count = count;
            ProductId = productId;
            SkuId = skuId;
            UserID = userID;
            Value = value;
            CreateTime = createTime;
            ModifyTime = modifyTime;
            UsedValue = usedValue;
            UsedCount = usedCount;
            UsedExpDayTime = usedExpDayTime;
            UsedID = usedID;
            IsExpired = isExpired;
        }

        public override bool Equals(object obj)
        {
            return obj is InvSearch other &&
                   ExpDayTime == other.ExpDayTime &&
                   Count == other.Count &&
                   ProductId == other.ProductId &&
                   SkuId == other.SkuId &&
                   UserID == other.UserID &&
                   Value == other.Value &&
                   CreateTime == other.CreateTime &&
                   ModifyTime == other.ModifyTime &&
                   UsedValue == other.UsedValue &&
                   UsedCount == other.UsedCount &&
                   UsedExpDayTime == other.UsedExpDayTime &&
                   UsedID == other.UsedID &&
                   IsEdit == other.IsEdit
                   ;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(ExpDayTime);
            hash.Add(Count);
            hash.Add(ProductId);
            hash.Add(SkuId);
            hash.Add(UserID);
            hash.Add(Value);
            hash.Add(CreateTime);
            hash.Add(ModifyTime);
            hash.Add(UsedValue);
            hash.Add(UsedCount);
            hash.Add(UsedExpDayTime);
            hash.Add(UsedID);
            hash.Add(IsEdit);
            return hash.ToHashCode();
        }
    }
}
