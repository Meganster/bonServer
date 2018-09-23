using System;
using System.Collections.Generic;
using System.IO;

namespace server
{
    public class ContentWrapper
    {
        private static Dictionary<string, string> _possibleContentTypes =
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

        private Settings _settings;

        public Settings Settings { get => _settings; set => _settings = value; }
        public Dictionary<string, string> PossibleContentTypes { get => _possibleContentTypes; }

        public ContentWrapper(Settings settings)
        {
            Settings = settings;
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
				response.HttpStatusCode = HttpStatusCode.NotAllowed;
                return;
            }

			bool isForbiden = false;
            if (path == "/")
            {
                path = Settings.DefaultDirectioryFile;
            }
            else if (path[path.Length - 1] == '/')
            {
				isForbiden = true;
                path = path + Settings.DefaultDirectioryFile;
            }

            if (path[0] == '/')
            {
                path = path.Substring(1);
            }

            string absolutePath = Path.Combine(Settings.Root, path);
            FileInfo fileInfo;

            try
            {
                fileInfo = new FileInfo(absolutePath);
            }
            catch (Exception)
            {
                fileInfo = null;
            }

            if (fileInfo == null || !fileInfo.Exists || !fileInfo.FullName.StartsWith(Settings.Root))
            {
				response.HttpStatusCode = isForbiden ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound;
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