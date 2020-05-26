#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using Seatbelt.Util;


namespace Seatbelt.Commands.Windows.EventLogs
{
    internal class ProcessCreationEventsCommand : CommandBase
    {
        public override string Command => "ProcessCreationEvents";
        public override string Description => "Process creation logs (4688) with sensitive data.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false; // TODO remote

        public ProcessCreationEventsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (!SecurityUtil.IsHighIntegrity())
            {
                WriteError("Unable to collect. Must be an administrator.");
                yield break;
            }

            WriteVerbose($"Searching process creation logs (EID 4688) for sensitive data.\n");

            // Get our "sensitive" cmdline regexes from a common helper function.
            Regex[] processCmdLineRegex = MiscUtil.GetProcessCmdLineRegex();

            var eventId = 4688;
            var query = String.Format("*[System/EventId={0}]", eventId);
            var eventLogQuery = new EventLogQuery("Security", PathType.LogName, query);
            eventLogQuery.ReverseDirection = true;
            var logReader = new EventLogReader(eventLogQuery);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                var commandLine = eventDetail.Properties[8].Value.ToString().Trim();
                if (commandLine != "")
                {
                    foreach (var reg in processCmdLineRegex)
                    {
                        var m = reg.Match(commandLine);
                        if (m.Success)
                        {
                            yield return new ProcessCreationDTO()
                            {
                                TimeCreated = eventDetail.TimeCreated,
                                EventID = eventDetail.Id,
                                UserID = $"{eventDetail.UserId}",
                                Match = m.Value
                            };
                        }
                    }
                }
            }
        }
    }

    internal class ProcessCreationDTO : CommandDTOBase
    {
        public DateTime? TimeCreated { get; set; }
        public int EventID { get; set; }
        public string UserID { get; set; }
        public string Match { get; set; }
    }
}
#nullable enable