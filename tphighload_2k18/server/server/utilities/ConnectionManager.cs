namespace server
{
    public class ConnectionManager
    {
		public void Set(HttpRequest request, HttpResponse response)
        {
            response.KeepAlive = false;
            response.Headers["Connection"] = "close";
        }
    }
}