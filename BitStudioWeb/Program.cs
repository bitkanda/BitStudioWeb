using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace bitkanda
{
    public class Program
    {


        public static void Main(string[] args)
        { 
           
            //需要迁移
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
               Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    //如果是linux的，需要启用该代码。https://www.cnblogs.com/bmyblogs/p/14284799.html
                    //webBuilder.UseKestrel(options =>
                    //{
                    //    options.AllowSynchronousIO = true;
                    //});
                    webBuilder.UseStartup<Startup>();
                });
        //WebHost.CreateDefaultBuilder(args)
        // // .UseUrls("http://localhost:5018")
        //    .UseStartup<Startup>();
    }
}
