using System.Text;

namespace server
{
    public class ResponseWrapper
    {
        public void Set(HttpResponse response)
        {
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