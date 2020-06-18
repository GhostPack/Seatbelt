#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands.Windows.EventLogs
{
    internal class PoweredOnEventsCommand : CommandBase
    {
        public override string Command => "PoweredOnEvents";
        public override string Description => "Reboot and sleep schedule based on the System event log EIDs 1, 12, 13, 42, and 6008. Default of 7 days, argument == last X days.";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO remote

        public PoweredOnEventsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // queries event IDs 12 (kernel boot) and 13 (kernel shutdown), sorts, and gives reboot schedule
            // grab events from the last X days - 15 for default
            var lastDays = 7;

            if (!Runtime.FilterResults)
            {
                lastDays = 30;
            }

            if (args.Length == 1)
            {
                if (!int.TryParse(args[0], out lastDays))
                {
                    WriteError("Could not parse number");
                    yield break;
                }
            }

            WriteHost($"Collecting kernel boot (EID 12) and shutdown (EID 13) events from the last {lastDays} days\n");
            WriteHost("Powered On Events (Time is local time)");

            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;


            // eventID 1 == sleep
            var query = $@"
((*[System[(EventID=12 or EventID=13) and Provider[@Name='Microsoft-Windows-Kernel-General']]] or *[System/EventID=42]) or (*[System/EventID=6008]) or (*[System/EventID=1] and *[System[Provider[@Name='Microsoft-Windows-Power-Troubleshooter']]])) and *[System[TimeCreated[@SystemTime >= '{startTime.ToUniversalTime():o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime.ToUniversalTime():o}']]]
";

            var eventsQuery = new EventLogQuery("System", PathType.LogName, query);
            var logReader = new EventLogReader(eventsQuery);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                string action = null;
                switch (eventDetail.Id)
                {
                    case 1:
                        action = "awake";
                        break;
                    case 12:
                        action = "startup";
                        break;
                    case 13:
                        action = "shutdown";
                        break;
                    case 42:
                        action = "sleep";
                        break;
                    case 6008:
                        action = "shutdown(UnexpectedShutdown)";
                        break;
                }

                yield return new PoweredOnEventsDTO()
                {
                    DateUtc = (DateTime)eventDetail.TimeCreated?.ToUniversalTime(),
                    Description = action
                };
            }
        }
    }

    internal class PoweredOnEventsDTO : CommandDTOBase
    {
        public DateTime DateUtc { get; set; }
        public string Description { get; set; }
    }

    [CommandOutputType(typeof(PoweredOnEventsDTO))]
    internal class PoweredOnEventsTextFormatter : TextFormatterBase
    {
        private string _currentDay = "";

        public PoweredOnEventsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {

            var dto = (PoweredOnEventsDTO)result;
            if (_currentDay != dto.DateUtc.ToShortDateString())
            {
                _currentDay = dto.DateUtc.ToShortDateString();
                WriteLine();
            }

            WriteLine($"  {dto.DateUtc.ToLocalTime(),-23} :  {dto.Description}");
        }
    }
}
#nullable enable