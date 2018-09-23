using System;

namespace server
{
    class Program
    {
		public static void Main(string[] args)
		{
			// попробуем найти путь к файлу настроек
			string settingsFilePath = string.Empty;
			if (args.Length == 0)
			{
				Console.WriteLine("Bad parameters. Cannot find config file.\nStart with default httpd.conf");
				settingsFilePath = "/etc/httpd.conf";
			}
			else
			{
				settingsFilePath = args[0];
			}

			// загрузка настроек из файла
			Settings settings = Settings.LoadSettings(settingsFilePath);
			if (settings != null)
			{
				new Server(settings).Run().Wait();
			}
		}
    }
}
