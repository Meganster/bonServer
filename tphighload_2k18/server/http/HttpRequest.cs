using System.Collections.Generic;

namespace server
{
    public class HttpRequest
    {
        public HttpRequest(string rawRequest)
        {
            this.RawRequest = rawRequest;
        }
        
        public string RawRequest { get; }
        
        public HttpMethod? HttpMethod { get; set; }
        
        public string Url { get; set; }
        
        public HttpVersion? HttpVersion { get; set; }
        
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        public bool UseCrLf { get; set; } = false;
    }
}