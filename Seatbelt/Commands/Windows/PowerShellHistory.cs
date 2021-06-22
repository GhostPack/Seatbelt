using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Seatbelt.Util;

namespace Seatbelt.Commands.Windows
{
    internal class PowerShellHistoryCommand : CommandBase
    { 

        public override string Command => "PowerShellHistory";
        public override string Description => "Searches PowerShell console history files for sensitive regex matches.";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public PowerShellHistoryCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // get our "sensitive" cmdline regexes from a common helper function.
            var powershellRegex = MiscUtil.GetProcessCmdLineRegex();

            var context = 3; // number of lines around the match to display

            if (args.Length >= 1)
            {
                var allArgs = String.Join(" ", args);
                powershellRegex = new[] { new Regex(allArgs, RegexOptions.IgnoreCase & RegexOptions.Multiline) };
            }

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

                var consoleHistoryPath = $"{dir}\\AppData\\Roaming\\Microsoft\\Windows\\PowerShell\\PSReadline\\ConsoleHost_history.txt";

                if (!File.Exists(consoleHistoryPath)) 
                    continue;

                var content = File.ReadAllText(consoleHistoryPath);

                foreach (var reg in powershellRegex)
                {
                    var m = reg.Match(content);
                    if (!m.Success)
                        continue;

                    var contextLines = new List<string>();

                    var scriptBlockParts = content.Split('\n');
                    for (var i = 0; i < scriptBlockParts.Length; i++)
                    {
                        if (!scriptBlockParts[i].Contains(m.Value))
                            continue;

                        var printed = 0;
                        for (var j = 1; i - j > 0 && printed < context; j++)
                        {
                            if (scriptBlockParts[i - j].Trim() == "")
                                continue;

                            contextLines.Add(scriptBlockParts[i - j].Trim());
                            printed++;
                        }

                        printed = 0;
                        contextLines.Add(m.Value.Trim());
                        for (var j = 1; printed < context && i + j < scriptBlockParts.Length; j++)
                        {
                            if (scriptBlockParts[i + j].Trim() == "")
                                continue;

                            contextLines.Add(scriptBlockParts[i + j].Trim());
                            printed++;
                        }
                        break;
                    }

                    var contextJoined = string.Join("\n", contextLines.ToArray());

                    yield return new PowerShellHistoryDTO(
                        userName,
                        consoleHistoryPath,
                        m.Value,
                        contextJoined
                    );
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