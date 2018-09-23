using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace server
{
    public class Settings
    {
        private string _root = "/var/www/html";
        private string _defaultDirectioryFile = "index.html";
        private short _port = 80;
        private short _threadLimit = 0;

        public string Root
        {
            get
            {
                return _root;
            }
            set
            {
                _root = value;
            }
        }

        public string DefaultDirectioryFile
        {
            get
            {
                return _defaultDirectioryFile;
            }
            set
            {
                _defaultDirectioryFile = value;
            }
        }

        public short Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }

        public short ThreadLimit
        {
            get
            {
                return _threadLimit;
            }
            set
            {
                _threadLimit = value;
            }
        }

        public Settings(
            string directoryRoot, string defaultUrl,
            short port, short threadLimit)
        {
            Root = String.IsNullOrEmpty(directoryRoot) ? directoryRoot : throw new ArgumentNullException();
            DefaultDirectioryFile = String.IsNullOrEmpty(defaultUrl) ? defaultUrl : throw new ArgumentNullException();
            Port = port;
            ThreadLimit = threadLimit;
        }

        public Settings(Dictionary<string, string> settings)
        {
            if (settings.TryGetValue("listen", out var portStr)
                && short.TryParse(portStr, out var port))
            {
                Port = port;
            }

            if (settings.ContainsKey("cpu_limit"))
            {
                Console.WriteLine("cpu_limit does not use");
            }

            if (settings.TryGetValue("thread_limit", out var threadLimitStr)
                && short.TryParse(threadLimitStr, out var threadLimit))
            {
                ThreadLimit = threadLimit;
            }

            if (settings.TryGetValue("document_root", out var documentRoot))
            {
                // обрежем '/' если он имеется в конце
                if (documentRoot.EndsWith("/"))
                {
                    documentRoot = documentRoot.Substring(0, documentRoot.Length - 1);
                }

                DirectoryInfo directoryInfo;
                try
                {
                    directoryInfo = new DirectoryInfo(documentRoot);
                }
                catch (Exception)
                {
                    directoryInfo = null;
                }

                if (!directoryInfo.Exists || directoryInfo == null)
                {
                    Console.WriteLine("Bad document_root");
                }
                else
                {
                    Root = documentRoot;
                }
            }
        }
        
		public static Settings LoadSettings(string settingsFilePath)
        {
            try
            {
				var regex = new Regex(@"^(?<property>\w*)\s(?<value>[^#]*)",
				                      RegexOptions.Compiled | RegexOptions.Singleline);

                Dictionary<string, string> settings = new Dictionary<string, string>();
                var lines = File.ReadAllLines(settingsFilePath);

                foreach (var line in lines)
                {
                    var match = regex.Match(line);

                    if (match.Success)
                    {
						var key = match.Groups["property"].Value.Trim();
                        var value = match.Groups["value"].Value.Trim();
                        settings[key] = value;
                    }
                }

                return new Settings(settings);
            }
            catch (FileNotFoundException)
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
