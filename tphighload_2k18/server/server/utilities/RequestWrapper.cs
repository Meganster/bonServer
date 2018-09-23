using System;
using System.IO;
using System.Text;

namespace server
{
	public class RequestWrapper
    {
        private const char Space = ' ';
        private const char Cr = '\r';
        private const char Plus = '+';
        private const char Colon = ':';
        private const char QuestionMark = '?';
        private const char Percentage = '%';
        private const char Zero = '0';
        private const char Nine = '9';
        private const char a = 'a';
        private const char f = 'f';
        private const char A = 'A';
        private const char F = 'F';

		public void Set(HttpRequest request, HttpResponse response)
        {
            if (!response.Success)
            {
                return;
            }

            foreach (var ch in request.RawRequest)
            {
                if (ch == '\r')
                {
                    request.UseCrLf = true;
                    response.UseCrLf = true;
                }
                else if (ch == '\n')
                {
                    break;
                }
            }
            
            int lineCount = 0;
            using (var stream = new StringReader(request.RawRequest))
            {
				string firstLine = stream.ReadLine();

                // проверим есть ли в строке данные
                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    response.Success = false;
					response.HttpStatusCode = HttpStatusCode.NotAllowed;
                    return;
                }

                // парсим метод, урл, версию протокола http
                if (!ParseRequestLine(firstLine, request, response))
                {
                    return;
                }

                response.HttpVersion = request.HttpVersion;
                lineCount++;

                string line;
                var headers = request.Headers;

                while ((line = stream.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
                {
                    int colonIndex = line.IndexOf(Colon);

                    // двоеточие отсутствует или не задано само значение
                    // строка битая, вернем ошибку
                    if (colonIndex < 1 || line.Length == colonIndex - 1)
                    {
						response.HttpStatusCode = HttpStatusCode.NotAllowed;
                        return;
                    }

                    headers[line.Substring(0, colonIndex).Trim()] = line.Substring(colonIndex + 1).Trim();
                }
            }

            // пустой запрос
            if (lineCount == 0)
            {
                response.Success = false;
				response.HttpStatusCode = HttpStatusCode.NotAllowed;
            }
        }

        private static bool ParseRequestLine(string line, HttpRequest request, HttpResponse response)
        {
            int position = 0;

            // пропускаем пустые символы
            for (; position < line.Length && line[position] == Space; position++)
            { }

            if (!ParseMethod(line, request, response, ref position))
            {
                return false;
            }

            position++;

            if (!ParseUrl(line, request, response, ref position))
            {
                return false;
            }

            position++;

            return ParseVersion(line, request, response, ref position);
        }

        private static bool ParseMethod(string line, HttpRequest request, HttpResponse response, ref int position)
        {
            int startPosition = position;
			bool isGetMethod = true;
            bool isHeadMethod = true;
            string getCaption = HttpMethod.Get.GetCaption();
            string headCaption = HttpMethod.Head.GetCaption();

            for (; position < line.Length && line[position] != Space && line[position] != Cr; position++)
            {
                if (isGetMethod
                    && ((position - startPosition) >= getCaption.Length 
				        || line[position] != getCaption[position - startPosition]))
                {
                    isGetMethod = false;
                }

                if (isHeadMethod
                    && ((position - startPosition) >= headCaption.Length 
				        || line[position] != headCaption[position - startPosition]))
                {
                    isHeadMethod = false;
                }
            }

            if (isGetMethod && position - startPosition == getCaption.Length)
            {
                request.HttpMethod = HttpMethod.Get;
                return true;
            }

            if (isHeadMethod && position - startPosition == headCaption.Length)
            {
                request.HttpMethod = HttpMethod.Head;
                return true;
            }

            response.Success = false;
            response.HttpStatusCode = HttpStatusCode.NotAllowed;
            return false;
        }

        private static bool ParseUrl( string line, HttpRequest request, HttpResponse response, ref int position)
        {
            int startPosition = position;
            bool pathEncoded = false;

            for (; position < line.Length && line[position] != Space && line[position] != Cr; position++)
            {
                var ch = line[position];

                if (ch == QuestionMark)
                {
                    if (position == startPosition)
                    {
                        // Начинаться с ? некорректно
                        response.Success = false;
						response.HttpStatusCode = HttpStatusCode.NotAllowed;
                        return false;
                    }

                    break;
                }

                // если нашли '%' 
                // значит url закодирован
                if (ch == Percentage)
                {
                    if (position == startPosition)
                    {
                        // Начинаться с % некорректно
                        response.Success = false;
						response.HttpStatusCode = HttpStatusCode.NotAllowed;
                        return false;
                    }

                    pathEncoded = true;
                }
            }

            if (position == startPosition)
            {
                // пустой URL
                response.Success = false;
				response.HttpStatusCode = HttpStatusCode.NotAllowed;
                return false;
            }

            // декодирование url
            request.Url = pathEncoded 
				? new Decoder(position - startPosition).Decode(line, startPosition, position) 
				: line.Substring(startPosition, position - startPosition);         

            // т.к. get параметры не нужны, то игнорируем их
            for (; position < line.Length && line[position] != Space; position++)
            {
            }

            return true;
        }

        private static bool ParseVersion( string line, HttpRequest request, HttpResponse response, ref int position)
        {
            int startPosition = position;
            bool isHttp10 = true;
			bool isHttp11 = true;
            string http10Caption = HttpVersion.Http10.GetCaption();
			string http11Caption = HttpVersion.Http11.GetCaption();

            for (; position < line.Length && line[position] != Space && line[position] != Cr; position++)
            {
                if (isHttp10 && ((position - startPosition) >= http10Caption.Length
                        || line[position] != http10Caption[position - startPosition]))
                {
                    isHttp10 = false;
                }

                if (isHttp11 && ((position - startPosition) >= http11Caption.Length
                        || line[position] != http11Caption[position - startPosition]))
                {
                    isHttp11 = false;
                }
            }

            if (startPosition == position)
            {
                // значение по умолчанию, когда нет ничего
                request.HttpVersion = HttpVersion.Http11;
                return true;
            }

            if (isHttp10 && position - startPosition == http10Caption.Length)
            {
                request.HttpVersion = HttpVersion.Http10;
                return true;
            }

            if (isHttp11 && position - startPosition == http11Caption.Length)
            {
                request.HttpVersion = HttpVersion.Http11;
                return true;
            }

            response.Success = false;
			response.HttpStatusCode = HttpStatusCode.NotAllowed;
            return false;
        }

        #region decoder
        private class Decoder
        {
            private int _bufferSize;
            private int _numChars;
            private char[] _charBuffer;
            private int _numBytes;
            private byte[] _byteBuffer;

            public int BufferSize { get => _bufferSize; set => _bufferSize = value; }
            public int NumChars { get => _numChars; set => _numChars = value; }
            public char[] CharBuffer { get => _charBuffer; set => _charBuffer = value; }
            public int NumBytes { get => _numBytes; set => _numBytes = value; }
            public byte[] ByteBuffer { get => _byteBuffer; set => _byteBuffer = value; }


            private void FlushBytes()
            {
                if (NumBytes > 0)
                {
                    NumChars += Encoding.UTF8.GetChars(
                        ByteBuffer,
                        0,
                        NumBytes,
                        CharBuffer,
                        NumChars
                        );

                    NumBytes = 0;
                }
            }

            public Decoder(int bufferSize)
            {
                BufferSize = bufferSize;
                CharBuffer = new char[bufferSize];
            }

            public string Decode(string value, int from, int to)
            {
                if (value == null)
                {
                    return null;
                }

                int count = value.Length;
                for (int pos = from; pos < to; pos++)
                {
                    char ch = value[pos];

                    if (ch == Plus)
                    {
                        ch = Space;
                    }
                    else if (ch == Percentage && pos < count - 2)
                    {
                        int h1 = HexToInt(value[pos + 1]);
                        int h2 = HexToInt(value[pos + 2]);

                        if (h1 >= 0 && h2 >= 0)
                        {
                            byte b = (byte)((h1 << 4) | h2);
                            pos += 2;

                            AddByte(b);
                            continue;
                        }
                    }

                    if ((ch & 0xFF80) == 0)
                    {
                        AddByte((byte)ch);
                    }
                    else
                    {
                        AddChar(ch);
                    }
                }

                return GetString();
            }

            private void AddChar(char ch)
            {
                if (NumBytes > 0)
                {
                    FlushBytes();
                }

                CharBuffer[NumChars++] = ch;
            }

            private void AddByte(byte b)
            {
                if (ByteBuffer == null)
                {
                    ByteBuffer = new byte[BufferSize];
                }

                ByteBuffer[NumBytes++] = b;
            }

            private string GetString()
            {
                if (NumBytes == 0)
                {
                    return string.Empty;
                }

                FlushBytes();
                return new string(CharBuffer, 0, NumChars);
            }

            private static int HexToInt(char h)
            {
                if (h >= Zero && h <= Nine)
                {
                    return h - Zero;
                }

                if (h >= a && h <= f)
                {
                    return h - a + 10;
                }

                if (h >= A && h <= F)
                {
                    return h - A + 10;
                }

                return -1;
            }
        }

        #endregion

    }
}