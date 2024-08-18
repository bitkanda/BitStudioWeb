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
using BitStudioWeb.ChatLogic;

namespace BitStudioWeb.Controllers
{





    public class GPTController : Controller
    {
        private readonly MysqlDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public GPTController(MysqlDBContext dbContext, IConfiguration configuration
            , IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _env = env;
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
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
        //https://platform.openai.com/docs/api-reference/images/create
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [Route("v1/images/generations")]
        [HttpPost]
        public Task<ActionResult> ImagesGenerations()
        {
            try
            { 
                String Body = "";
                using (var streamReader = new StreamReader(HttpContext.Request.BodyReader.AsStream()))
                {
                    Body = streamReader.ReadToEnd();
                }  
                //当余额够扣的时候，执行转发操作。
                return ImagePostData(Body);
            }
            finally
            {
                Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 完成接口调用");
            } 
        }

        /// <summary>
        /// 测试生成图片。
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = RoleConst.User)]
        [Route("v1/images/generationsTest")]
        [HttpPost]
        public IActionResult ImagesGenerationsTest()
        {
            imageTestHelper imageTestHelper = new imageTestHelper(_env,Request);
            var result = imageTestHelper.GetImageByRandomly();
            return Json( result);
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
        private async Task<ActionResult> ImagePostData(string requestContent)
        {

            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.Headers["Cache-Control"] = "no-cache";
            HttpContext.Response.Headers["Connection"] = "keep-alive";
            HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*"; // 允许跨域 
            var result= new GPTImageStreamWriterResult(HttpContext.Response.Body, _dbContext, _configuration, User.Identity.Name,
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
        /// <summary>
        /// 已用库存ID
        /// </summary>
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
