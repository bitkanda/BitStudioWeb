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
    public class StreamWriterResult : ActionResult
    {
        private readonly Stream _responseBody;

        public StreamWriterResult(Stream responseBody)
        {
            _responseBody = responseBody;
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            var writer = new StreamWriter(_responseBody);
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
}
