#nullable disable
using static Seatbelt.Interop.Netapi32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using System;
using System.IO;
using Seatbelt.Util;
using System.Text.RegularExpressions;

namespace Seatbelt.Commands.Windows
{
    internal class PowerShellHistoryCommand : CommandBase
    { 

        public override string Command => "PowerShellHistory";
        public override string Description => "Searches PowerShell console history files for sensitive regex matches.";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;
        public Runtime ThisRunTime;

        public PowerShellHistoryCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // get our "sensitive" cmdline regexes from a common helper function.
            var powershellRegex = MiscUtil.GetProcessCmdLineRegex();

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

                var consoleHistoryPath = $"{dir}\\AppData\\Roaming\\Microsoft\\Windows\\PowerShell\\PSReadline\\ConsoleHost_history.txt";
                
                if (File.Exists(consoleHistoryPath))
                {
                    string content = System.IO.File.ReadAllText(consoleHistoryPath);

                    foreach (var reg in powershellRegex)
                    {
                        var matches = reg.Matches(content);
                        foreach(Match match in matches)
                        {
                            string context = content.Substring(match.Index - 100, 200 + match.Length);

                            yield return new PowerShellHistoryDTO(
                                userName,
                                consoleHistoryPath,
                                match.ToString(),
                                context
                            );
                        }
                    }
                }
            }
        }

        internal class PowerShellHistoryDTO : CommandDTOBase
        {
            public PowerShellHistoryDTO(string? userName, string? fileName, string? match, string? context)
            {
                UserName = userName;
                FileName = fileName;
                Match = match;
                Context = context;
            }

            public string? UserName { get; set; }
            public string? FileName { get; set; }
            public string? Match { get; set; }
            public string? Context { get; set; }
        }
    }
}
#nullable enable