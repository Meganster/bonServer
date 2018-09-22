using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private RequestParser _requestParser;
        private ContentWrapper _contentWrapper;
        private HeadersWrapper _defaultHeadersWriter;
        private ConnectionManager _connectionManager;
        private ResponseWrapper _responseWrapper;

        public Settings Settings { get => _settings; set => _settings = value; }
        public RequestParser RequestParser { get => _requestParser; set => _requestParser = value; }
        public ContentWrapper ContentSearch { get => _contentWrapper; set => _contentWrapper = value; }
        public HeadersWrapper DefaultHeadersWriter { get => _defaultHeadersWriter; set => _defaultHeadersWriter = value; }
        public ConnectionManager DefaultConnectionManager { get => _connectionManager; set => _connectionManager = value; }
        public ResponseWrapper ResponseWriter { get => _responseWrapper; set => _responseWrapper = value; }

        public Server(Settings settings)
        {
            this.Settings = settings;
            this.ContentSearch = new ContentWrapper(settings);
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
                    Processing(tcpClient);
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

        private async Task Processing(TcpClient tcpClient)
        {
            var connectionId = Guid.NewGuid().ToString();

            try
            {
                Log(connectionId, "begin request");

                using (NetworkStream stream = tcpClient.GetStream())
                {
                    bool keepConnection = false;

                    do
                    {
                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();

                        var rawContent = await ReadRequest(stream);

                        LogRequest(connectionId, rawContent);

                        var request = new HttpRequest(rawContent);
                        var response = new HttpResponse();

                        try
                        {
                            RequestParser.Invoke(request, response);
                            ContentSearch.Invoke(request, response);
                            DefaultHeadersWriter.Invoke(request, response);
                            DefaultConnectionManager.Invoke(request, response);
                            ResponseWriter.Invoke(request, response);
                        }
                        catch (Exception)
                        {
                            //Log(connectionId, $" in middleware {m.GetType().FullName}");
                            throw;
                        }

                        LogResponse(connectionId, response.RawHeadersResponse);

                        // асинхронно отправим ответ
                        await SendResponse(stream, response.RawHeadersResponse, response.ResponseContentFilePath, response.ContentLength);

                        stopWatch.Stop();
                        Log(connectionId, $"response complete in {stopWatch.ElapsedMilliseconds} ms");

                        keepConnection = response.KeepAlive;
                    } while (keepConnection);
                }
            }
            catch (Exception e)
            {
                Error(connectionId, e);
            }
            finally
            {
                try
                {
                    tcpClient.Close();
                    Log(connectionId, "connection close");
                }
                catch (Exception e)
                {
                    Error(connectionId, e);
                }
            }
        }

        private static async Task<string> ReadRequest(NetworkStream stream)
        {
            var buf = new byte[Constants.BUFFER_SIZE];
            var builder = new StringBuilder(Constants.DEFAULT_BUILDER_SIZE);

            using (var cancellationTokenSource = new CancellationTokenSource(Constants.RECEIVE_TIMEOUT_MS))
            {
                using (cancellationTokenSource.Token.Register(stream.Close))
                {
                    do
                    {
                        var readpos = 0;

                        do
                        {
                            readpos += await stream.ReadAsync(buf, readpos, Constants.BUFFER_SIZE - readpos, cancellationTokenSource.Token);
                        } while (readpos < Constants.BUFFER_SIZE && stream.DataAvailable);

                        builder.Append(Encoding.UTF8.GetString(buf, 0, readpos));
                    } while (stream.DataAvailable);
                }
            }

            return builder.ToString();
        }

        private static async Task SendResponse(NetworkStream stream, string head, string contentFilename, long contentLength)
        {
            var timeout = Constants.SEND_TIMEOUT_MS_PER_KB;

            if (contentFilename != null && contentLength > 0)
            {
                timeout *= ((int)Math.Min(contentLength, int.MaxValue) / 1024) + 1;
            }

            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                // как только закроется сетевой поток
                // отменим передачу данных в него
                using (cancellationTokenSource.Token.Register(stream.Close))
                {
                    var bytes = Encoding.UTF8.GetBytes(head);
                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationTokenSource.Token);

                    if (contentFilename != null)
                    {
                        using (var fileStream = new FileStream(contentFilename, FileMode.Open, FileAccess.Read))
                        {
                            await fileStream.CopyToAsync(stream, 81920, cancellationTokenSource.Token);
                        }
                    }
                }
            }
        }

        #region Logging
        private static void Log(string id, string text)
        {
            // Console.WriteLine($"[{id}]: {text}.");
        }

        private static void Error(string id, Exception e)
        {
            Console.WriteLine($"[{id}]: Exception occured {e.GetType().FullName}\n{e.Message}\n{e.StackTrace}.");
        }

        private static void LogRequest(string id, string content)
        {
            // Console.WriteLine($"[{id}]: Request:\n{content}");
        }

        private static void LogResponse(string id, string response)
        {
            // Console.WriteLine($"[{id}]: Response headers:\n{response}");
        }
        #endregion

    }
}
