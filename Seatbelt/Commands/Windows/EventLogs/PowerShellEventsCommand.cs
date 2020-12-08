using Seatbelt.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;

namespace Seatbelt.Commands.Windows.EventLogs
{
    internal class PowerShellEventsCommand : CommandBase
    {
        public override string Command => "PowerShellEvents";
        public override string Description => "PowerShell script block logs (4104) with sensitive data.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public PowerShellEventsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // adapted from @djhohnstein's EventLogParser project
            //  https://github.com/djhohnstein/EventLogParser/blob/master/EventLogParser/EventLogHelpers.cs
            // combined with scraping from https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/windows-commands

            WriteVerbose($"Searching script block logs (EID 4104) for sensitive data.\n");

            var context = 3; // number of lines around the match to display

            string[] powershellLogs = { "Microsoft-Windows-PowerShell/Operational", "Windows PowerShell" };

            // Get our "sensitive" cmdline regexes from a common helper function.
            var powershellRegex = MiscUtil.GetProcessCmdLineRegex();

            if (args.Length >= 1)
            {
                string allArgs = String.Join(" ", args);
                powershellRegex = new Regex [] { new Regex(allArgs, RegexOptions.IgnoreCase & RegexOptions.Multiline) };
            }

            foreach (var logName in powershellLogs)
            {
                var query = "*[System/EventID=4104]";

                var logReader = ThisRunTime.GetEventLogReader(logName, query);

                for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
                {
                    var scriptBlock = eventDetail.Properties[2].Value.ToString();

                    foreach (var reg in powershellRegex)
                    {
                        var m = reg.Match(scriptBlock);
                        if (!m.Success)
                            continue;

                        var contextLines = new List<string>();

                        var scriptBlockParts = scriptBlock.Split('\n');
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

                        yield return new PowerShellEventsDTO(
                            eventDetail.TimeCreated,
                            eventDetail.Id,
                            $"{eventDetail.UserId}",
                            m.Value,
                            contextJoined
                        );
                    }
                }
            }
        }
    }

    internal class PowerShellEventsDTO : CommandDTOBase
    {
        public PowerShellEventsDTO(DateTime? timeCreated, int eventId, string userId, string match, string context)
        {
            TimeCreated = timeCreated;
            EventId = eventId;
            UserId = userId;
            Match = match;
            Context = context;
        }
        public DateTime? TimeCreated { get; }
        public int EventId { get; }
        public string UserId { get; }
        public string Match { get; }
        public string Context { get; }
    }
}
