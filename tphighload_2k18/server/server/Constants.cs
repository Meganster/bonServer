namespace server
{
    public class Constants
    {
        public const int READ_CHUNK_SIZE = 1024;
		public const int READ_BUFFER_SIZE = 32 * READ_CHUNK_SIZE;
        public const int SEND_TIMEOUT_MS_PER_KB = 5000;
        public const int RECEIVE_TIMEOUT_MS = 5000;
		public const int DEFAULT_FILE_COPY_BUFFER = 81920;
    }
}
