namespace server
{
    public class ConnectionManager
    {
        public void Invoke(HttpRequest request, HttpResponse response)
        {
            response.KeepAlive = false;
            response.Headers["Connection"] = "close";
        }
    }
}