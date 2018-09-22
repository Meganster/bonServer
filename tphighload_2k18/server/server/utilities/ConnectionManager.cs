namespace server
{
    public class ConnectionManager
    {
		public void Set(HttpResponse response)
        {
            response.KeepAlive = false;
            response.Headers["Connection"] = "close";
        }
    }
}