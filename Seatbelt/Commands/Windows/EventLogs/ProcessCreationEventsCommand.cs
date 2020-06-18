using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
            var processCmdLineRegex = MiscUtil.GetProcessCmdLineRegex();

            var query = $"*[System/EventID=4688]";
            var eventLogQuery = new EventLogQuery("Security", PathType.LogName, query);
            eventLogQuery.ReverseDirection = true;
            var logReader = new EventLogReader(eventLogQuery);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                var commandLine = eventDetail.Properties[8].Value.ToString().Trim();
                if (string.IsNullOrEmpty(commandLine))
                    continue;

                foreach (var reg in processCmdLineRegex)
                {
                    var m = reg.Match(commandLine);
                    if (m.Success)
                    {
                        yield return new ProcessCreationDTO(
                            eventDetail.TimeCreated,
                            eventDetail.Id,
                            $"{eventDetail.UserId}",
                            m.Value
                        );
                    }
                }
            }
        }
    }

    internal class ProcessCreationDTO : CommandDTOBase
    {
        public ProcessCreationDTO(DateTime? timeCreated, int eventId, string userId, string match)
        {
            TimeCreated = timeCreated;
            EventId = eventId;
            UserId = userId;
            Match = match;
        }

        public DateTime? TimeCreated { get; set; }
        public int EventId { get; set; }
        public string UserId { get; set; }
        public string Match { get; set; }
    }
}
