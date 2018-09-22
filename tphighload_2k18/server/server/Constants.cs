namespace server
{
    public class Constants
    {
        public const int BUFFER_SIZE = 1024;
        public const int DEFAULT_BUILDER_SIZE = 32 * BUFFER_SIZE;
        public const int SEND_TIMEOUT_MS_PER_KB = 5_000;
        public const int RECEIVE_TIMEOUT_MS = 5_000;
		public const int DEFAULT_FILE_COPY_BUFFER = 81920;
    }
}
