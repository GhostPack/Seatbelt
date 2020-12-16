using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;


namespace Seatbelt.Commands.Windows.EventLogs
{
    internal class ProcessCreationEventsCommand : CommandBase
    {
        public override string Command => "ProcessCreationEvents";
        public override string Description => "Process creation logs (4688) with sensitive data.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ProcessCreationEventsCommand(Runtime runtime) : base(runtime)
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

            WriteVerbose($"Searching process creation logs (EID 4688) for sensitive data.");
            WriteVerbose($"Format: Date(Local time),User,Command line.\n");

            // Get our "sensitive" cmdline regexes from a common helper function.
            var processCmdLineRegex = MiscUtil.GetProcessCmdLineRegex();

            if (args.Length >= 1)
            {
                string allArgs = String.Join(" ", args);
                processCmdLineRegex = new Regex[] { new Regex(allArgs, RegexOptions.IgnoreCase & RegexOptions.Multiline) };
            }

            var query = $"*[System/EventID=4688]";
            var logReader = ThisRunTime.GetEventLogReader("Security", query);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                var user = eventDetail.Properties[1].Value.ToString().Trim();
                var commandLine = eventDetail.Properties[8].Value.ToString().Trim();

                foreach (var reg in processCmdLineRegex)
                {
                    var m = reg.Match(commandLine);
                    if (m.Success)
                    {
                        yield return new ProcessCreationEventDTO(
                            eventDetail.TimeCreated?.ToUniversalTime(),
                            eventDetail.Id,
                            user,
                            commandLine
                        );
                    }
                }
            }
        }
    }

    internal class ProcessCreationEventDTO : CommandDTOBase
    {
        public ProcessCreationEventDTO(DateTime? timeCreatedUtc, int eventId, string user, string match)
        {
            TimeCreatedUtc = timeCreatedUtc;
            EventID = eventId;
            User = user;
            Match = match;
        }
        public DateTime? TimeCreatedUtc { get; set; }
        public int EventID { get; set; }
        public string User { get; set; }
        public string Match { get; set; }
    }

    [CommandOutputType(typeof(ProcessCreationEventDTO))]
    internal class ProcessCreationEventTextFormatter : TextFormatterBase
    {
        public ProcessCreationEventTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ProcessCreationEventDTO)result;

            WriteLine($"  {dto.TimeCreatedUtc?.ToLocalTime(),-22}  {dto.User,-30} {dto.Match}");
        }
    }
}
