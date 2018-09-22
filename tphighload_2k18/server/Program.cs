using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace server
{
    class Program
    {
        private const string _key = "key";
        private const string _val = "val";
        private const string _regex = @"^\s*(?<" + _key + @">\w*)\s(?<" + _val + ">[^#]*).*$";

        public static void Main(string[] args)
        {
            // попробуем найти путь к файлу настроек
            string settingsFilePath = GetSettingsFilePath(args);

            // загрузка настроек из файла
            Settings settings = LoadSettings(settingsFilePath);
            if (settings != null)
            {
                Console.WriteLine("Here server start");
                //// запуск сервера с настройками
                //new HttpServer(settings)
                //    .RunServer()
                //    .Wait();
            }
        }

        private static string GetSettingsFilePath(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Bad parameters. Cannot find config file.\nStart with default httpd.conf");
                return "httpd.conf";
            }

            return args[0];
        }

        private static Settings LoadSettings(string settingsFilePath)
        {
            try
            {
                var regex = new Regex(_regex, RegexOptions.Compiled | RegexOptions.Singleline);

                Dictionary<string, string> settings = new Dictionary<string, string>();
                var lines = File.ReadAllLines(settingsFilePath);

                foreach (var line in lines)
                {
                    var match = regex.Match(line);

                    if (match.Success)
                    {
                        var key = match.Groups[_key].Value.Trim();
                        var value = match.Groups[_val].Value.Trim();
                        settings[key] = value;
                    }
                }

                return new Settings(settings);
            }
            catch(FileNotFoundException)
            {
                Console.WriteLine("Cannot load config file. Is the specified path '{0}' correct?", settingsFilePath);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Oops, something going wrong.\n{0}", ex);
                return null;
            }
        }
    }
}
