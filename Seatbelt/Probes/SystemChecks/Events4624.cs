using System;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.RegularExpressions;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class Events4624 : IProbe
    {
        public static string ProbeName => "4624Events";
        
        public string List()
        {
            var sb = new StringBuilder();

            var eventId = "4624";

            // grab events from the last X days - 7 for default, 30 for "full" collection
            var lastDays = 7;

            if (!FilterResults.Filter)
            {
                lastDays = 30;
            }

            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;

            sb.AppendProbeHeaderLine($"4624 Account Logon Events (last {lastDays} days)");

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
                    //string SubjectUserSid = eventdetail.Properties[0].Value.ToString();
                    //string SubjectUserName = eventdetail.Properties[1].Value.ToString();
                    //string SubjectDomainName = eventdetail.Properties[2].Value.ToString();
                    //string SubjectLogonId = eventdetail.Properties[3].Value.ToString();
                    var TargetUserSid = eventdetail.Properties[4].Value.ToString();
                    var TargetUserName = eventdetail.Properties[5].Value.ToString();
                    var TargetDomainName = eventdetail.Properties[6].Value.ToString();
                    //string TargetLogonId = eventdetail.Properties[7].Value.ToString();
                    //string LogonType = eventdetail.Properties[8].Value.ToString();
                    var LogonType = String.Format("{0}", (SECURITY_LOGON_TYPE)(Int32.Parse(eventdetail.Properties[8].Value.ToString())));
                    //string LogonProcessName = eventdetail.Properties[9].Value.ToString();
                    var AuthenticationPackageName = eventdetail.Properties[10].Value.ToString();
                    var WorkstationName = eventdetail.Properties[11].Value.ToString();
                    //string LogonGuid = eventdetail.Properties[12].Value.ToString();
                    //string TransmittedServices = eventdetail.Properties[13].Value.ToString();
                    var LmPackageName = eventdetail.Properties[14].Value.ToString();
                    //string KeyLength = eventdetail.Properties[15].Value.ToString();
                    //string ProcessId = eventdetail.Properties[16].Value.ToString();
                    var ProcessName = eventdetail.Properties[17].Value.ToString();
                    //string IpAddress = eventdetail.Properties[18].Value.ToString();
                    //string IpPort = eventdetail.Properties[19].Value.ToString();
                    //string ImpersonationLevel = eventdetail.Properties[20].Value.ToString();
                    //string RestrictedAdminMode = eventdetail.Properties[21].Value.ToString();
                    //string TargetOutboundUserName = eventdetail.Properties[22].Value.ToString();
                    //string TargetOutboundDomainName = eventdetail.Properties[23].Value.ToString();
                    //string VirtualAccount = eventdetail.Properties[24].Value.ToString();
                    //string TargetLinkedLogonId = eventdetail.Properties[25].Value.ToString();
                    //string ElevatedToken = eventdetail.Properties[26].Value.ToString();

                    // filter out SYSTEM, computer accounts, local service accounts, UMFD-X accounts, and DWM-X accounts (for now)
                    var ignoreRegex = new Regex(@"SYSTEM|\$$|LOCAL SERVICE|NETWORK SERVICE|UMFD-[0-9]+|DWM-[0-9]+|ANONYMOUS LOGON");
                    var m = ignoreRegex.Match(TargetUserName);

                    if (!m.Success)
                    {
                        sb.AppendLine($"  UserName          : {TargetUserName}");
                        sb.AppendLine($"  UserDomain        : {TargetDomainName}");
                        sb.AppendLine($"  UserSID           : {TargetUserSid}");
                        sb.AppendLine($"  ProcessName       : {ProcessName}");
                        sb.AppendLine($"  LogonType         : {LogonType}");
                        sb.AppendLine($"  AuthPKG           : {AuthenticationPackageName}");
                        sb.AppendLine($"  LmPackageName     : {LmPackageName}");
                        sb.AppendLine($"  WorkstationName   : {WorkstationName}");
                        sb.AppendLine($"  TimeCreated       : {eventdetail.TimeCreated.ToString()}");
                        sb.AppendLine();

                        //Console.WriteLine(eventdetail.FormatDescription());
                        //break;
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
