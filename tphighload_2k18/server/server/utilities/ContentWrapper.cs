using System;
using System.Collections.Generic;
using System.IO;

namespace server
{
    public class ContentWrapper
    {
        private static Dictionary<string, string> PossibleContentTypes =
            new Dictionary<string, string>
            {
                [".html"] = "text/html",
                [".css"] = "text/css",
                [".js"] = "application/javascript",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".gif"] = "image/gif",
                [".swf"] = "application/x-shockwave-flash",
            };

        private Settings settings;


        public ContentWrapper(Settings settings)
        {
            this.settings = settings;
        }

		public void Set(HttpRequest request, HttpResponse response)
        {
            if (!response.Success)
            {
                return;
            }

            string path = request.Url;
            if (string.IsNullOrWhiteSpace(path))
            {
                response.HttpStatusCode = HttpStatusCode.BadRequest;
                return;
            }

			bool need403 = false;
            if (path == "/")
            {
                path = this.settings.DefaultDirectioryFile;
            }
            else if (path[path.Length - 1] == '/')
            {
                need403 = true;
                path = path + this.settings.DefaultDirectioryFile;
            }

            if (path[0] == '/')
            {
                path = path.Substring(1);
            }

            var absolutePath = Path.Combine(this.settings.Root, path);
            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(absolutePath);
            }
            catch (Exception)
            {
                fileInfo = null;
            }

            if (fileInfo == null || !fileInfo.Exists || !fileInfo.FullName.StartsWith(this.settings.Root))
            {
                response.HttpStatusCode = need403 ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound;
                return;
            }

            response.HttpStatusCode = HttpStatusCode.Ok;
            response.Headers["Content-Length"] = fileInfo.Length.ToString();
            response.ContentLength = fileInfo.Length;

            if (PossibleContentTypes.TryGetValue(fileInfo.Extension, out var ct))
            {
                response.Headers["Content-Type"] = ct;
            }

            if (Equals(request.HttpMethod, HttpMethod.Get))
            {
                response.ResponseContentFilePath = fileInfo.FullName;
            }
        }
    }
}