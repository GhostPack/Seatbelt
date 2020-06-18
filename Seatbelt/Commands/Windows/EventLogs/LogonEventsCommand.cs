using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Seatbelt.Util;
using Seatbelt.Interop;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using static Seatbelt.Interop.Secur32;

// TODO: Group unique credentials together like we do with explicit logon events
namespace Seatbelt.Commands.Windows.EventLogs
{
    internal class LogonEventsCommand : CommandBase
    {
        public override string Command => "LogonEvents";
        public override string Description => "Logon events (Event ID 4624) from the security event log. Default of 10 days, argument == last X days.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false; // TODO remote


        public LogonEventsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (!SecurityUtil.IsHighIntegrity())
            {
                WriteError("Unable to collect. Must be an administrator/in a high integrity context.");
                yield break;
            }

            var NTLMv1Users = new HashSet<string>();
            var NTLMv2Users = new HashSet<string>();
            var KerberosUsers = new HashSet<string>();

            // grab events from the last X days - 10 for workstations, 30 for "-full" collection
            // Always use the user-supplied value, if specified
            var lastDays = 10;
            if (Shlwapi.IsWindowsServer())
            {
                lastDays = 1;
            }

            string? userRegex = null;

            if (args.Length >= 1)
            {
                if (!int.TryParse(args[0], out lastDays))
                {
                    throw new ArgumentException("Argument is not an integer");
                }
            }

            if (args.Length >= 2)
            {
                userRegex = args[1];
            }

            WriteVerbose($"Listing 4624 Account Logon Events for the last {lastDays} days.\n");

            if (userRegex != null)
            {
                WriteVerbose($"Username Filter: {userRegex}");
            }

            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;

            var query = $@"*[System/EventID=4624] and *[System[TimeCreated[@SystemTime >= '{startTime.ToUniversalTime():o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime.ToUniversalTime():o}']]]";
            var eventsQuery = new EventLogQuery("Security", PathType.LogName, query) { ReverseDirection = true };
            var logReader = new EventLogReader(eventsQuery);

            WriteHost("  TimeCreated,TargetUser,LogonType,IpAddress,SubjectUsername,AuthenticationPackageName,LmPackageName,TargetOutboundUser");

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                //var subjectUserSid = eventDetail.Properties[0].Value.ToString();
                var subjectUserName = eventDetail.Properties[1].Value.ToString();
                var subjectDomainName = eventDetail.Properties[2].Value.ToString();
                //var subjectLogonId = eventDetail.Properties[3].Value.ToString();
                //var targetUserSid = eventDetail.Properties[4].Value.ToString();
                var targetUserName = eventDetail.Properties[5].Value.ToString();
                var targetDomainName = eventDetail.Properties[6].Value.ToString();
                //var targetLogonId = eventDetail.Properties[7].Value.ToString();
                //var logonType = eventDetail.Properties[8].Value.ToString();
                var logonType = $"{(SECURITY_LOGON_TYPE)(int.Parse(eventDetail.Properties[8].Value.ToString()))}";
                //var logonProcessName = eventDetail.Properties[9].Value.ToString();
                var authenticationPackageName = eventDetail.Properties[10].Value.ToString();
                //var workstationName = eventDetail.Properties[11].Value.ToString();
                //var logonGuid = eventDetail.Properties[12].Value.ToString();
                //var transmittedServices = eventDetail.Properties[13].Value.ToString();
                var lmPackageName = eventDetail.Properties[14].Value.ToString();
                lmPackageName = lmPackageName == "-" ? "" : lmPackageName;
                //var keyLength = eventDetail.Properties[15].Value.ToString();
                //var processId = eventDetail.Properties[16].Value.ToString();
                //var processName = eventDetail.Properties[17].Value.ToString();
                var ipAddress = eventDetail.Properties[18].Value.ToString();
                //var ipPort = eventDetail.Properties[19].Value.ToString();
                //var impersonationLevel = eventDetail.Properties[20].Value.ToString();
                //var restrictedAdminMode = eventDetail.Properties[21].Value.ToString();

                var targetOutboundUserName = "-";
                var targetOutboundDomainName = "-";
                if (eventDetail.Properties.Count > 22)  // Not available on older versions of Windows
                {
                    targetOutboundUserName = eventDetail.Properties[22].Value.ToString();
                    targetOutboundDomainName = eventDetail.Properties[23].Value.ToString();
                }
                //var VirtualAccount = eventDetail.Properties[24].Value.ToString();
                //var TargetLinkedLogonId = eventDetail.Properties[25].Value.ToString();
                //var ElevatedToken = eventDetail.Properties[26].Value.ToString();

                // filter out SYSTEM, computer accounts, local service accounts, UMFD-X accounts, and DWM-X accounts (for now)
                var userIgnoreRegex = "^(SYSTEM|LOCAL SERVICE|NETWORK SERVICE|UMFD-[0-9]+|DWM-[0-9]+|ANONYMOUS LOGON|" + Environment.MachineName + "\\$)$";
                if (userRegex == null && Regex.IsMatch(targetUserName, userIgnoreRegex, RegexOptions.IgnoreCase))
                    continue;

                var domainIgnoreRegex = "^(NT VIRTUAL MACHINE)$";
                if (userRegex == null && Regex.IsMatch(targetDomainName, domainIgnoreRegex, RegexOptions.IgnoreCase))
                    continue;


                // Handle the user filter
                if (userRegex != null && !Regex.IsMatch(targetUserName, userRegex, RegexOptions.IgnoreCase))
                    continue;

                // Analyze the output
                if (logonType == "Network")
                {
                    var accountName = $"{targetDomainName}\\{targetUserName}";
                    if (authenticationPackageName == "NTLM")
                    {
                        switch (lmPackageName)
                        {
                            case "NTLM V1":
                                NTLMv1Users.Add(accountName);
                                break;
                            case "NTLM V2":
                                NTLMv2Users.Add(accountName);
                                break;
                        }
                    }
                    else if (authenticationPackageName == "Kerberos")
                    {
                        KerberosUsers.Add(accountName);
                    }
                }

                yield return new LogonEventsDTO(
                    eventDetail.TimeCreated?.ToUniversalTime(),
                    targetUserName,
                    targetDomainName,
                    logonType,
                    ipAddress,
                    subjectUserName,
                    subjectDomainName,
                    authenticationPackageName,
                    lmPackageName,
                    targetOutboundUserName,
                    targetOutboundDomainName
                );
            }


            // TODO: Move all of this into a Foramtter class
            if (NTLMv1Users.Count > 0 || NTLMv2Users.Count > 0)
            {
                WriteHost("\n  Other accounts authenticate to this machine using NTLM! NTLM-relay may be possible");
            }

            if (NTLMv1Users.Count > 0)
            {
                WriteHost("\n  Accounts authenticate to this machine using NTLM v1!");
                WriteHost("  You can obtain these accounts' **NTLM** hashes by sniffing NTLM challenge/responses and then cracking them!");
                WriteHost("  NTLM v1 authentication is 100% broken!\n");

                PrintUserSet(NTLMv1Users);
            }

            if (NTLMv2Users.Count > 0)
            {
                WriteHost("\n  Accounts authenticate to this machine using NTLM v2!");
                WriteHost("  You can obtain NetNTLMv2 for these accounts by sniffing NTLM challenge/responses.");
                WriteHost("  You can then try and crack their passwords.\n");

                PrintUserSet(NTLMv2Users);
            }

            if (KerberosUsers.Count > 0)
            {
                WriteHost("\n  The following users have authenticated to this machine using Kerberos.\n");
                PrintUserSet(KerberosUsers);
            }
        }

        private void PrintUserSet(HashSet<string> users)
        {
            var set = users.OrderBy(u => u).ToArray();

            var line = new StringBuilder();
            for (var i = 0; i < set.Length; i++)
            {
                if (i % 3 == 0)
                {
                    WriteHost(line.ToString());
                    line.Length = 0;
                    line.Append("    ");
                }

                line.Append(set.ElementAt(i).PadRight(30));
            }

            WriteHost(line.ToString());
            WriteHost();
        }
    }

    internal class LogonEventsDTO : CommandDTOBase
    {
        public LogonEventsDTO(DateTime? timeCreatedUtc, string targetUserName, string targetDomainName, string logonType, string ipAddress, string subjectUserName, string subjectDomainName, string authenticationPackage, string lmPackage, string targetOutboundUserName, string targetOutboundDomainName)
        {
            TimeCreatedUtc = timeCreatedUtc;
            TargetUserName = targetUserName;
            TargetDomainName = targetDomainName;
            LogonType = logonType;
            IpAddress = ipAddress;
            SubjectUserName = subjectUserName;
            SubjectDomainName = subjectDomainName;
            AuthenticationPackage = authenticationPackage;
            LmPackage = lmPackage;
            TargetOutboundUserName = targetOutboundUserName;
            TargetOutboundDomainName = targetOutboundDomainName;    
        }

        public DateTime? TimeCreatedUtc { get; set; }
        public string TargetUserName { get; set; }
        public string TargetDomainName { get; set; }
        public string LogonType { get; set; }
        public string IpAddress { get; set; }
        public string SubjectUserName { get; set; }
        public string SubjectDomainName { get; set; }
        public string AuthenticationPackage { get; set; }
        public string LmPackage { get; set; }
        public string TargetOutboundUserName { get; set; }
        public string TargetOutboundDomainName { get; set; }
    }

    [CommandOutputType(typeof(LogonEventsDTO))]
    internal class LogonEventsTextFormatter : TextFormatterBase
    {
        public LogonEventsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (LogonEventsDTO)result;
            var targetUser = dto.TargetDomainName + "\\" + dto.TargetUserName;
            var subjectUser = dto.SubjectDomainName + "\\" + dto.SubjectUserName;
            string targetOutboundUser = "";
            if (dto.TargetOutboundUserName != "-")
            {
                targetOutboundUser = dto.TargetOutboundDomainName + "\\" + dto.TargetOutboundUserName;
            }

            WriteLine($"  {dto.TimeCreatedUtc?.ToLocalTime()},{targetUser},{dto.LogonType},{dto.IpAddress},{subjectUser},{dto.AuthenticationPackage},{dto.LmPackage},{targetOutboundUser}");
        }
    }
}
