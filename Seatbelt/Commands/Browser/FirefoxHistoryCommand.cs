using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Browser
{
    internal class FirefoxHistoryCommand : CommandBase
    {
        public override string Command => "FirefoxHistory";
        public override string Description => "Parses any found FireFox history files";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false;

        public FirefoxHistoryCommand(Runtime runtime) : base(runtime)
        {
        }

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

                var userFirefoxBasePath = $"{dir}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\";

                // parses a Firefox history file via regex
                if (Directory.Exists(userFirefoxBasePath))
                {
                    var history = new List<string>();
                    var directories = Directory.GetDirectories(userFirefoxBasePath);

                    foreach (var directory in directories)
                    {
                        var firefoxHistoryFile = $"{directory}\\places.sqlite";
                        var historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

                        try
                        {
                            using (var r = new StreamReader(firefoxHistoryFile))
                            {
                                string line;
                                while ((line = r.ReadLine()) != null)
                                {
                                    var m = historyRegex.Match(line);
                                    if (m.Success)
                                    {
                                        // WriteHost("      " + m.Groups[0].ToString().Trim());
                                        history.Add(m.Groups[0].ToString().Trim());
                                    }
                                }
                            }
                        }
                        catch (IOException exception)
                        {
                            WriteError($"IO exception, places.sqlite file likely in use (i.e. Firefox is likely running). {exception.Message}");
                        }
                        catch (Exception e)
                        {
                            WriteError(e.ToString());
                        }
                    }

                    yield return new FirefoxHistoryDTO(
                        userName,
                        history
                    );
                }
            }
        }

        internal class FirefoxHistoryDTO : CommandDTOBase
        {
            public FirefoxHistoryDTO(string userName, List<string> history)
            {
                UserName = userName;
                History = history;
            }
            public string UserName { get; }
            public List<string> History { get; }
        }

        [CommandOutputType(typeof(FirefoxHistoryDTO))]
        internal class InternetExplorerFavoritesFormatter : TextFormatterBase
        {
            public InternetExplorerFavoritesFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (FirefoxHistoryDTO)result;

                WriteLine($"\n    History ({dto.UserName}):\n");

                foreach (var history in dto.History)
                {
                    WriteLine($"       {history}");
                }
                WriteLine();
            }
        }
    }
}
