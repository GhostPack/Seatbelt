using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using Seatbelt.Util;

namespace Seatbelt.Commands.Windows.EventLogs.ExplicitLogonEvents
{
    internal class ExplicitLogonEventsCommand : CommandBase
    {
        public override string Description => "Explicit Logon events (Event ID 4648) from the security event log. Default of 7 days, argument == last X days.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc, CommandGroup.Remote };
        public override string Command => "ExplicitLogonEvents";
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ExplicitLogonEventsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            const string eventId = "4648";
            string? userFilterRegex = null;

            // grab events from the last X days - 7 for default, 30 for "-full" collection
            // Always use the user-supplied value, if specified
            var lastDays = 7;
            if (args.Length >= 1)
            {
                if (!int.TryParse(args[0], out lastDays))
                {
                    WriteError("Argument is not an integer");

                    yield break;
                }
            }
            else
            {
                if (!Runtime.FilterResults)
                {
                    lastDays = 30;
                }
            }


            if (!SecurityUtil.IsHighIntegrity() && !ThisRunTime.ISRemote())
            {
                WriteError("Unable to collect. Must be an administrator.");
                yield break;
            }

            WriteHost("Listing 4648 Explicit Credential Events - A process logged on using plaintext credentials");

            if (args.Length >= 2)
            {
                userFilterRegex = args[1];
                WriteHost($"Username Filter: {userFilterRegex}");
            }
            WriteHost("Output Format:");
            WriteHost("  --- TargetUser,ProcessResults,SubjectUser,IpAddress ---");
            WriteHost("  <Dates the credential was used to logon>\n\n");


            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;

            var query = $@"*[System/EventID={eventId}] and *[System[TimeCreated[@SystemTime >= '{startTime.ToUniversalTime():o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime.ToUniversalTime():o}']]]";

            var logReader = ThisRunTime.GetEventLogReader("Security", query);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                //string subjectUserSid = eventDetail.Properties[0].Value.ToString();
                var subjectUserName = eventDetail.Properties[1].Value.ToString();
                var subjectDomainName = eventDetail.Properties[2].Value.ToString();
                //var subjectLogonId = eventDetail.Properties[3].Value.ToString();
                //var logonGuid = eventDetail.Properties[4].Value.ToString();
                var targetUserName = eventDetail.Properties[5].Value.ToString();
                var targetDomainName = eventDetail.Properties[6].Value.ToString();
                //var targetLogonGuid = eventDetail.Properties[7].Value.ToString();
                //var targetServerName = eventDetail.Properties[8].Value.ToString();
                //var targetInfo = eventDetail.Properties[9].Value.ToString();
                //var processId = eventDetail.Properties[10].Value.ToString();
                var processName = eventDetail.Properties[11].Value.ToString();
                var ipAddress = eventDetail.Properties[12].Value.ToString();
                //var IpPort = eventDetail.Properties[13].Value.ToString();

                // Ignore the current machine logging on and 
                if (Runtime.FilterResults && Regex.IsMatch(targetUserName, Environment.MachineName) ||
                    Regex.IsMatch(targetDomainName, @"^(Font Driver Host|Window Manager)$"))
                {
                    continue;
                }

                if (userFilterRegex != null && !Regex.IsMatch(targetUserName, userFilterRegex))
                    continue;

                yield return new ExplicitLogonEventsDTO()
                {
                    TimeCreatedUtc = eventDetail.TimeCreated?.ToUniversalTime(),
                    SubjectUser = subjectUserName,
                    SubjectDomain = subjectDomainName,
                    TargetUser = targetUserName,
                    TargetDomain = targetDomainName,
                    Process = processName,
                    IpAddress = ipAddress
                };
            }
        }
    }
}
