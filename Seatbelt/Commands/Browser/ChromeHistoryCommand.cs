using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Browser
{
    internal class ChromeHistoryCommand : CommandBase
    {
        public ChromeHistoryCommand(Runtime runtime) : base(runtime)
        {
        }

        public override string Command => "ChromeHistory";
        public override string Description => "Parses any found Chrome history files";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc, CommandGroup.Chrome };
        public override bool SupportRemote => false;

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);
            foreach (var dir in dirs)
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                var userChromeHistoryPath = $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History";

                // parses a Chrome history file via regex
                if (File.Exists(userChromeHistoryPath))
                {
                    var historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");
                    var URLs = new List<string>();

                    try
                    {
                        using (var r = new StreamReader(userChromeHistoryPath))
                        {
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
                    }
                    catch (IOException exception)
                    {
                        WriteError($"IO exception, history file likely in use (i.e. Browser is likely running): {exception.Message}");
                    }
                    catch (Exception exception)
                    {
                        WriteError(exception.ToString());
                    }

                    yield return new ChromeHistoryDTO(
                        userName,
                        URLs
                    );
                }
            }
        }

        internal class ChromeHistoryDTO : CommandDTOBase
        {
            public ChromeHistoryDTO(string userName, List<string> urLs)
            {
                UserName = userName;
                URLs = urLs;
            }
            public string UserName { get; }
            public List<string> URLs { get; }
        }

        [CommandOutputType(typeof(ChromeHistoryDTO))]
        internal class ChromeHistoryFormatter : TextFormatterBase
        {
            public ChromeHistoryFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (ChromeHistoryDTO)result;

                WriteLine($"History ({dto.UserName}):\n");

                foreach (var url in dto.URLs)
                {
                    WriteLine($"  {url}");
                }
                WriteLine();
            }
        }
    }
}
