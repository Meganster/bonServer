using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public class Server
    {
        private Settings _settings;
		private RequestWrapper _requestWrapper;
        private ContentWrapper _contentWrapper;
        private HeadersWrapper _headersWrapper;
        private ConnectionManager _connectionManager;
        private ResponseWrapper _responseWrapper;

        public Settings Settings { get => _settings; set => _settings = value; }
		public RequestWrapper RequestWrapper { get => _requestWrapper; set => _requestWrapper = value; }
		public ContentWrapper ContentWrapper { get => _contentWrapper; set => _contentWrapper = value; }
		public HeadersWrapper HeadersWrapper { get => _headersWrapper; set => _headersWrapper = value; }
        public ConnectionManager DefaultConnectionManager { get => _connectionManager; set => _connectionManager = value; }
		public ResponseWrapper ResponseWrapper { get => _responseWrapper; set => _responseWrapper = value; }

        public Server(Settings settings)
        {
            this.Settings = settings;
			this.RequestWrapper = new RequestWrapper();
			this.ContentWrapper = new ContentWrapper(settings);
			this.HeadersWrapper = new HeadersWrapper();
			this.DefaultConnectionManager = new ConnectionManager();
			this.ResponseWrapper = new ResponseWrapper();
        }
        
        public async Task Run()
        {
            #region OnStart log
            Console.WriteLine("Server started.");
            Console.WriteLine($"Current port: {Settings.Port}");
            Console.WriteLine($"Current root: {Settings.Root}");
            #endregion

            if (Settings.ThreadLimit > 0)
            {
				// магия
                // установка макс количества подключений к пулу потоков
                Console.WriteLine($"ThreadPool.SetMaxThreads({Settings.ThreadLimit}, {Settings.ThreadLimit})");
                ThreadPool.SetMaxThreads(Settings.ThreadLimit, Settings.ThreadLimit);
            }

            TcpListener tcpListener = null;
            try
            {
                tcpListener = TcpListener.Create(Settings.Port);
                tcpListener.Start();

                while (true)
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    ProcessingAsync(tcpClient);
                }
            }
            finally
            {
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                }
            }
        }

        private async Task ProcessingAsync(TcpClient tcpClient)
		{
            try
			{
				using (NetworkStream networkStream = tcpClient.GetStream())
                {
                    bool keepConnection = false;

                    do
                    {
                    	string rawContent = await ReadRequest(networkStream);
						Console.WriteLine("rawContent = {0}", rawContent);
						HttpRequest request = new HttpRequest(rawContent);
						HttpResponse response = new HttpResponse();

                        try
                        {
							RequestWrapper.Set(request, response);
							ContentWrapper.Set(request, response);
							HeadersWrapper.Set(response);
                            DefaultConnectionManager.Set(response);
							ResponseWrapper.Set(response);
                        }
                        catch (Exception)
                        {
							throw new Exception();
                        }
                  
                        // асинхронно отправим ответ
						await SendResponse(networkStream, 
						                   response.RawHeadersResponse, 
						                   response.ResponseContentFilePath, 
						                   response.ContentLength);                
                        keepConnection = response.KeepAlive;
                    } while (keepConnection);
                }
            }
            finally
            {
				tcpClient.Close();
            }
        }

		private static async Task<string> ReadRequest(NetworkStream networkStream)
        {
            var chunkOfData = new byte[Constants.BUFFER_SIZE];
            var readedData = new StringBuilder(Constants.DEFAULT_BUILDER_SIZE);

            using (var cancellationTokenSource = new CancellationTokenSource(Constants.RECEIVE_TIMEOUT_MS))
            {
				using (cancellationTokenSource.Token.Register(networkStream.Close))
                {
					do
					{
						int readpos = 0;

						do
						{
							readpos += await networkStream.ReadAsync(chunkOfData, readpos, Constants.BUFFER_SIZE - readpos, cancellationTokenSource.Token);
						} while (readpos < Constants.BUFFER_SIZE && networkStream.DataAvailable);

						readedData.Append(Encoding.UTF8.GetString(chunkOfData, 0, readpos));
					} while (networkStream.DataAvailable);
                }
            }
            
			return readedData.ToString();
        }

        private static async Task SendResponse(NetworkStream networkStream, string header, string contentFilePath, long contentLength)
        {
            int timeout = Constants.SEND_TIMEOUT_MS_PER_KB;

			if (contentFilePath != null && contentLength > 0)
            {
                timeout *= ((int)Math.Min(contentLength, int.MaxValue) / 1024) + 1;
            }

            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                // как только закроется сетевой поток
                // отменим передачу данных в него
				using (cancellationTokenSource.Token.Register(networkStream.Close))
                {
					byte[] headerBytes = Encoding.UTF8.GetBytes(header);
					await networkStream.WriteAsync(headerBytes, 0, headerBytes.Length, 
					                               cancellationTokenSource.Token);
                    
					if (contentFilePath != null)
                    {
						using (var fileStream = new FileStream(contentFilePath, FileMode.Open, FileAccess.Read))
                        {
							await fileStream.CopyToAsync(networkStream, Constants.DEFAULT_FILE_COPY_BUFFER, 
							                             cancellationTokenSource.Token);
                        }
                    }
                }
            }
        }      
    }
}
