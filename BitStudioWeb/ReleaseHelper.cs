using BitStudioWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitStudioWeb
{
    public class ReleaseHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly HttpRequest Request;

        public ReleaseHelper(IWebHostEnvironment e, HttpRequest r)
        {
            _env = e;
            Request = r;
        }

        public JArray GetJsonFromDirectory(string directoryPath)
        {
            //var result = new JObject();
            var array = new JArray();
            var files = Directory.GetFiles(directoryPath, "version.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var contents = System.IO.File.ReadAllText(file);
                var directory = Path.GetDirectoryName(file);
                var o = JObject.Parse(contents);
                var url = GetDowndUrl(file, o);
                var downdurl = GetDownloadUrl(url);
                o.Add("url", downdurl);
                array.Add(o);
                //result[directory] = JObject.Parse(contents);
            }
            return array;
            //return result;

        }



        public string GetDownloadUrl(string filePath)
        {
            var webRootPath = _env.WebRootPath;
            var fileUrl = filePath.Replace(webRootPath, "\\").Replace("\\", "/");
            fileUrl = fileUrl.TrimStart(new char[] { '/' });
            var downloadUrl = $"{Request.Scheme}://{Request.Host}/{fileUrl}";
            return downloadUrl;
        }


        public string GetDowndUrl(string path, JToken j)
        {
            var name = j["name"].Value<string>();
            var dic = System.IO.Path.GetDirectoryName(path);
            var r = System.IO.Path.Combine(dic, name);
            return r;
        }
        public IEnumerable<ReleaseModel> GetAllRelease(string os)
        {
            string webRootPath = _env.WebRootPath;
            string releaseFilePath = Path.Combine(webRootPath, "release");

            var j = GetJsonFromDirectory(releaseFilePath);
            IEnumerable<ReleaseModel> release = from c in j
                                                select new ReleaseModel
                                                {
                                                    time = c["time"].Value<string>(),
                                                    versionName = c["versionName"].Value<string>(),
                                                    name = c["name"].Value<string>(),
                                                    os = c["os"].Value<string>(),
                                                    url = c["url"].Value<string>(),
                                                };
            IEnumerable<ReleaseModel> releaseresult = release;
            if (!string.IsNullOrWhiteSpace(os))
            {
                releaseresult = from c in release
                                where c.os == os
                                select c;
            }

            return releaseresult;
        }
    }
}
