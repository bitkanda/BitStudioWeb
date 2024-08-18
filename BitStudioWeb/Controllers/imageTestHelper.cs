using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace BitStudioWeb.Controllers
{
    public class imageTestHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly HttpRequest Request;

        public imageTestHelper(IWebHostEnvironment e, HttpRequest r)
        {
            _env = e;
            Request = r;
        }


        public string GetDownloadUrl(string filePath)
        {
            var webRootPath = _env.WebRootPath;
            var fileUrl = filePath.Replace(webRootPath, "\\").Replace("\\", "/");
            fileUrl = fileUrl.TrimStart(new char[] { '/' });
            var downloadUrl = $"{Request.Scheme}://{Request.Host}/{fileUrl}";
            return downloadUrl;
        }

        public object GetImageByRandomly()
        {
            string webRootPath = _env.WebRootPath;
            string releaseFilePath = Path.Combine(webRootPath, "dall_e_3_images");
            string[] files = Directory.GetFiles(releaseFilePath);
            Random random = new Random();
            string randomFile = files[random.Next(files.Length)];
            var url = GetDownloadUrl(randomFile);
           
            //Randomly return an image.
            var result = new 
            {
                created= 1711641694,
                data =new object[]  
                {
                    new 
                    {
                        url=url,
                        revised_prompt="Generate a colour portrait of a cheerful, unidentified female with Chinese descent. She has highly expressive eyes and elegantly styled dark hair. She's dressed in modern fashion trends, which give her a warm and friendly appeal."
                    }
                }
               
            };
            return result;
        }

    }
}
