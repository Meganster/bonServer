using System;

namespace server
{
    public enum HttpStatusCode
    {
        Ok,         // status 200
        Forbidden,  // status 403
        NotFound,   // status 404
        NotAllowed, // status 405
    }

    public static class HttpStatusCodeExtensions
    {
        public static string GetCaption(this HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Ok:
                    return "200 OK";
                case HttpStatusCode.Forbidden:
                    return "403 Forbidden";
                case HttpStatusCode.NotFound:
                    return "404 Not Found";
                case HttpStatusCode.NotAllowed:
                    return "405 Method Not Allowed";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}