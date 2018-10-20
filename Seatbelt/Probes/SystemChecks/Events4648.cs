using System;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.RegularExpressions;

namespace Seatbelt.Probes.SystemChecks
{
    public class Events4648 :IProbe
    {
        public static string ProbeName => "4648Events";

        public string List()
        {
            var sb = new StringBuilder();

            var eventId = "4648";

            // grab events from the last X days - 7 for default, 30 for "full" collection
            var lastDays = 7;

            if (!FilterResults.Filter)
            {
                lastDays = 30;
            }

            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;

            sb.AppendProbeHeaderLine($"4624 Explicit Credential Events (last {lastDays} days) - Runas or Outbound RDP");

            var query = string.Format(@"*[System/EventID={0}] and *[System[TimeCreated[@SystemTime >= '{1}']]] and *[System[TimeCreated[@SystemTime <= '{2}']]]",
                eventId,
                startTime.ToUniversalTime().ToString("o"),
                endTime.ToUniversalTime().ToString("o"));

            var eventsQuery = new EventLogQuery("Security", PathType.LogName, query);
            eventsQuery.ReverseDirection = true;

            try
            {
                var logReader = new EventLogReader(eventsQuery);

                for (var eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                {
                    var SubjectUserSid = eventdetail.Properties[0].Value.ToString();
                    var SubjectUserName = eventdetail.Properties[1].Value.ToString();
                    var SubjectDomainName = eventdetail.Properties[2].Value.ToString();
                    //string SubjectLogonId = eventdetail.Properties[3].Value.ToString();
                    //string LogonGuid = eventdetail.Properties[4].Value.ToString();
                    var TargetUserName = eventdetail.Properties[5].Value.ToString();
                    var TargetDomainName = eventdetail.Properties[6].Value.ToString();
                    //string TargetLogonGuid = eventdetail.Properties[7].Value.ToString();
                    var TargetServerName = eventdetail.Properties[8].Value.ToString();
                    //string TargetInfo = eventdetail.Properties[9].Value.ToString();
                    //string ProcessId = eventdetail.Properties[10].Value.ToString();
                    var ProcessName = eventdetail.Properties[11].Value.ToString();
                    //string IpAddress = eventdetail.Properties[12].Value.ToString();
                    //string IpPort = eventdetail.Properties[13].Value.ToString();

                    // filter out accounts (for now)
                    var ignoreRegex = new Regex(@"\$$");
                    var m = ignoreRegex.Match(SubjectUserName);
                    if (!m.Success)
                    {
                        sb.AppendLine($"  SubjectUserName        : {SubjectUserName}");
                        sb.AppendLine($"  SubjectDomainName      : {SubjectDomainName}");
                        sb.AppendLine($"  SubjectUserSid         : {SubjectUserSid}");
                        sb.AppendLine($"  TargetUserName         : {TargetUserName}");
                        sb.AppendLine($"  TargetDomainName       : {TargetDomainName}");
                        sb.AppendLine($"  TargetServerName       : {TargetServerName}");
                        sb.AppendLine($"  ProcessName            : {ProcessName}");
                        sb.AppendLine($"  TimeCreated            : {eventdetail.TimeCreated.ToString()}");
                        sb.AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

    }
}
