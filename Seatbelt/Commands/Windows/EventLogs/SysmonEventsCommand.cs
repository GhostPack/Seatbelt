#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using Seatbelt.Util;


namespace Seatbelt.Commands.Windows.EventLogs
{
    internal class SysmonEventCommand : CommandBase
    {
        public override string Command => "SysmonEvents";
        public override string Description => "Sysmon process creation logs (1) with sensitive data.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public SysmonEventCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (!SecurityUtil.IsHighIntegrity() && !ThisRunTime.ISRemote())
            {
                WriteError("Unable to collect. Must be an administrator.");
                yield break;
            }

            WriteVerbose($"Searching Sysmon process creation logs (Sysmon ID 1) for sensitive data.\n");

            // Get our "sensitive" cmdline regexes from a common helper function.
            Regex[] processCmdLineRegex = MiscUtil.GetProcessCmdLineRegex();

            if (args.Length >= 1)
            {
                string allArgs = String.Join(" ", args);
                processCmdLineRegex = new Regex[] { new Regex(allArgs, RegexOptions.IgnoreCase & RegexOptions.Multiline) };
            }

            var query = "*[System/EventID=1]";
            EventLogReader logReader = null;
            try
            {
                logReader = ThisRunTime.GetEventLogReader("Microsoft-Windows-Sysmon/Operational", query);
            }
            catch
            {
                WriteError("Unable to query Sysmon event logs, Sysmon likely not installed.");
                yield break;
            }

            var i = 0;

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                ++i;
                var commandLine = eventDetail.Properties[10].Value.ToString().Trim();
                if (commandLine != "")
                {
                    foreach (var reg in processCmdLineRegex)
                    {
                        var m = reg.Match(commandLine);
                        if (m.Success)
                        {
                            var userName = eventDetail.Properties[12].Value.ToString().Trim();
                            yield return new SysmonEventDTO()
                            {
                                TimeCreated = eventDetail.TimeCreated,
                                EventID = eventDetail.Id,
                                UserName = userName,
                                Match = m.Value
                            };
                        }
                    }
                }
            }
        }
    }

    internal class SysmonEventDTO : CommandDTOBase
    {
        public DateTime? TimeCreated { get; set; }
        public int EventID { get; set; }
        public string UserName { get; set; }
        public string Match { get; set; }
    }
}
#nullable enable