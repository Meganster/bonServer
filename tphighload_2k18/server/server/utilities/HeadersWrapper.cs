using System;

namespace server
{
    public class HeadersWrapper
    {
		public void Set(HttpResponse response)
        {
            response.Headers["Server"] = "server";
            response.Headers["Date"] = DateTime.UtcNow.ToString("r");
        }
    }
}