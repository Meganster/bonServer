using System;

namespace server
{
    public class HeadersWrapper
    {
        #region invoke
        
        public void Invoke(HttpRequest request, HttpResponse response)
        {
            response.Headers["Server"] = "server";
            response.Headers["Date"] = DateTime.UtcNow.ToString("r");
        }
        
        #endregion
    }
}