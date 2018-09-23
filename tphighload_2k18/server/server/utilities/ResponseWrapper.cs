using System;
using System.Text;

namespace server
{
    public class ResponseWrapper
    {
        public void Set(HttpResponse response)
        {
			// set server and date headers
			response.Headers["Server"] = "server";
            response.Headers["Date"] = DateTime.UtcNow.ToString("r");

            // set connection headers
			response.KeepAlive = false;
            response.Headers["Connection"] = "close";

            // set RawHeaders
            string newLine = response.UseCrLf ? "\r\n" : "\n";
			StringBuilder headers = new StringBuilder();
            
			headers.Append(response.HttpVersion?.GetCaption() ?? HttpVersion.Http11.GetCaption());
            headers.Append(" ");
            headers.Append(response.HttpStatusCode?.GetCaption());
            headers.Append(newLine);

            foreach (var header in response.Headers)
            {
                headers.Append(header.Key);
                headers.Append(": ");
                headers.Append(header.Value);
                headers.Append(newLine);
            }

            headers.Append(newLine);
            response.RawHeadersResponse = headers.ToString();
        }
    }
}