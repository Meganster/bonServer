using System;

namespace server
{
    public enum HttpVersion
    {
        Http10,
        Http11,
    }

    public static class HttpVersionExtensions
    {
        public static string GetCaption(this HttpVersion version)
        {
            switch (version)
            {
                case HttpVersion.Http10:
                    return "HTTP/1.0";
                case HttpVersion.Http11:
                    return "HTTP/1.1";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
   
}