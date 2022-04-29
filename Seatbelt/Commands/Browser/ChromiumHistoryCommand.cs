using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Browser
{
    internal class ChromiumHistoryCommand : CommandBase
    {
        public override string Command => "ChromiumHistory";
        public override string Description => "Parses any found Chrome/Edge/Brave/Opera history files";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc, CommandGroup.Chromium };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ChromiumHistoryCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                string[] paths = {
                    "\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\",
                    "\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default\\",
                    "\\AppData\\Local\\BraveSoftware\\Brave-Browser\\User Data\\Default\\",
                    "\\AppData\\Roaming\\Opera Software\\Opera Stable\\"
                };

                foreach (string path in paths)
                {
                    var userChromiumHistoryPath = $"{dir}{path}History";

                    // parses a Chrome history file via regex
                    if (File.Exists(userChromiumHistoryPath))
                    {
                        var historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");
                        var URLs = new List<string>();

                        try
                        {
                            using var fs = new FileStream(userChromiumHistoryPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var r = new StreamReader(fs);

                            string line;
                            while ((line = r.ReadLine()) != null)
                            {
                                var m = historyRegex.Match(line);
                                if (m.Success)
                                {
                                    URLs.Add($"{m.Groups[0].ToString().Trim()}");
                                }
                            }
                        }
                        catch (IOException exception)
                        {
                            WriteError($"IO exception, history file likely in use (i.e. browser is likely running): {exception.Message}");
                        }
                        catch (Exception exception)
                        {
                            WriteError(exception.ToString());
                        }

                        yield return new ChromiumHistoryDTO(
                            userName,
                            userChromiumHistoryPath,
                            URLs
                        );
                    }
                }
            }
        }

        internal class ChromiumHistoryDTO : CommandDTOBase
        {
            public ChromiumHistoryDTO(string userName, string filePath, List<string> urLs)
            {
                UserName = userName;
                FilePath = filePath;
                URLs = urLs;
            }
            public string UserName { get; }
            public string FilePath { get; }
            public List<string> URLs { get; }
        }

        [CommandOutputType(typeof(ChromiumHistoryDTO))]
        internal class ChromiumHistoryFormatter : TextFormatterBase
        {
            public ChromiumHistoryFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (ChromiumHistoryDTO)result;

                WriteLine($"History ({dto.FilePath}):\n");

                foreach (var url in dto.URLs)
                {
                    WriteLine($"  {url}");
                }
                WriteLine();
            }
        }
    }
}
