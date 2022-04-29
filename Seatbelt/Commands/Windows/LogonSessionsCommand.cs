using Seatbelt.Util;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static Seatbelt.Interop.Secur32;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class LogonSessionsCommand : CommandBase
    {
        public override string Command => "LogonSessions";
        public override string Description => "Windows logon sessions";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public LogonSessionsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (!SecurityUtil.IsHighIntegrity() || ThisRunTime.ISRemote())
            {
                // https://www.pinvoke.net/default.aspx/secur32.lsalogonuser

                // list user logons combined with logon session data via WMI
                var userDomainRegex = new Regex(@"Domain=""(.*)"",Name=""(.*)""");
                var logonIdRegex = new Regex(@"LogonId=""(\d+)""");

                WriteHost("Logon Sessions (via WMI)\r\n");

                var logonMap = new Dictionary<string, string[]>();

                var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_LoggedOnUser");
                var data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    var m = logonIdRegex.Match(result["Dependent"].ToString());
                    if (!m.Success)
                    {
                        continue;
                    }

                    var logonId = m.Groups[1].ToString();
                    var m2 = userDomainRegex.Match(result["Antecedent"].ToString());
                    if (!m2.Success)
                    {
                        continue;
                    }

                    var domain = m2.Groups[1].ToString();
                    var user = m2.Groups[2].ToString();
                    logonMap.Add(logonId, new[] { domain, user });
                }

                var wmiData2 = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_LogonSession");
                var data2 = wmiData2.Get();

                foreach (var o in data2)
                {
                    var result2 = (ManagementObject)o;
                    var userDomain = new string[2] { "", "" };
                    try
                    {
                        userDomain = logonMap[result2["LogonId"].ToString()];
                    }
                    catch { }
                    var domain = userDomain[0];
                    var userName = userDomain[1];
                    var startTime = new DateTime();
                    var logonType = "";

                    try
                    {
                        startTime = ManagementDateTimeConverter.ToDateTime(result2["StartTime"].ToString());
                    }
                    catch { }

                    try
                    {
                        logonType = $"{((SECURITY_LOGON_TYPE)(int.Parse(result2["LogonType"].ToString())))}";
                    }
                    catch { }

                    yield return new LogonSessionsDTO(
                        "WMI",
                        userName,
                        domain,
                        result2["LogonId"].ToString(),
                        logonType,
                        result2["AuthenticationPackage"].ToString(),
                        startTime,
                        null,
                        null,
                        null,
                        null,
                        null
                    );
                }
            }
            else
            {
                // heavily adapted from from Jared Hill:
                //      https://www.codeproject.com/Articles/18179/Using-the-Local-Security-Authority-to-Enumerate-Us

                WriteHost("Logon Sessions (via LSA)\n\n");

                var logonSessions = new List<LogonSessionsDTO>();

                var systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate

                var ret = LsaEnumerateLogonSessions(out var count, out var luidPtr);  // get an array of pointers to LUIDs

                for (ulong i = 0; i < count; i++)
                {
                    // TODO: Check return value
                    ret = LsaGetLogonSessionData(luidPtr, out var sessionData);
                    var data = (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                    // if we have a valid logon
                    if (data.PSiD != IntPtr.Zero)
                    {
                        // get the account username
                        var username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();

                        // convert the security identifier of the user
                        var sid = new System.Security.Principal.SecurityIdentifier(data.PSiD);

                        // domain for this account
                        var domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();

                        // authentication package
                        var authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();

                        // logon type
                        var logonType = (SECURITY_LOGON_TYPE)data.LogonType;

                        // datetime the session was logged in
                        var logonTime = systime.AddTicks((long)data.LoginTime);

                        // user's logon server
                        var logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();

                        // logon server's DNS domain
                        var dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();

                        // user principalname
                        var upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                        var logonID = "";
                        try { logonID = data.LoginID.LowPart.ToString(); }
                        catch { }

                        var userSID = "";
                        try { userSID = sid.Value; }
                        catch { }

                        yield return new LogonSessionsDTO(
                            "LSA",
                            username,
                            domain,
                            logonID,
                            logonType.ToString(),
                            authpackage,
                            null,
                            logonTime,
                            logonServer,
                            dnsDomainName,
                            upn,
                            userSID
                        );
                    }

                    // move the pointer forward
                    luidPtr = (IntPtr)((long)luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                    LsaFreeReturnBuffer(sessionData);
                }
                LsaFreeReturnBuffer(luidPtr);
            }
        }

        internal class LogonSessionsDTO : CommandDTOBase
        {
            public LogonSessionsDTO(string enumerationMethod, string userName, string domain, string logonId, string logonType, string authenticationPackage, DateTime? startTime, DateTime? logonTime, string? logonServer, string? logonServerDnsDomain, string? userPrincipalName, string? userSid)
            {
                EnumerationMethod = enumerationMethod;
                UserName = userName;
                Domain = domain;
                LogonId = logonId;
                LogonType = logonType;
                AuthenticationPackage = authenticationPackage;
                StartTime = startTime;
                LogonTime = logonTime;
                LogonServer = logonServer;
                LogonServerDnsDomain = logonServerDnsDomain;
                UserPrincipalName = userPrincipalName;
                UserSID = userSid;
            }
            public string EnumerationMethod { get; }

            public string UserName { get; }

            public string Domain { get; }

            public string LogonId { get; }

            public string LogonType { get; }

            public string AuthenticationPackage { get; }

            public DateTime? StartTime { get; }

            public DateTime? LogonTime { get; }

            public string? LogonServer { get; }

            public string? LogonServerDnsDomain { get; }

            public string? UserPrincipalName { get; }
            public string? UserSID { get; }

        }

        [CommandOutputType(typeof(LogonSessionsDTO))]
        internal class LogonSessionsTextFormatter : TextFormatterBase
        {
            public LogonSessionsTextFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (LogonSessionsDTO)result;

                if (dto.EnumerationMethod.Equals("WMI"))
                {
                    WriteLine("\n  UserName              : {0}", dto.UserName);
                    WriteLine("  Domain                : {0}", dto.Domain);
                    WriteLine("  LogonId               : {0}", dto.LogonId);
                    WriteLine("  LogonType             : {0}", dto.LogonType);
                    WriteLine("  AuthenticationPackage : {0}", dto.AuthenticationPackage);
                    WriteLine($"  StartTime             : {dto.StartTime}");
                    WriteLine("  UserPrincipalName     : {0}", dto.UserPrincipalName);

                }
                else
                {
                    // LSA enumeration
                    WriteLine("\n  UserName              : {0}", dto.UserName);
                    WriteLine("  Domain                : {0}", dto.Domain);
                    WriteLine("  LogonId               : {0}", dto.LogonId);
                    WriteLine("  UserSID               : {0}", dto.UserSID);
                    WriteLine("  AuthenticationPackage : {0}", dto.AuthenticationPackage);
                    WriteLine("  LogonType             : {0}", dto.LogonType);
                    WriteLine("  LogonTime             : {0}", dto.LogonTime);
                    WriteLine("  LogonServer           : {0}", dto.LogonServer);
                    WriteLine("  LogonServerDNSDomain  : {0}", dto.LogonServerDnsDomain);
                    WriteLine("  UserPrincipalName     : {0}", dto.UserPrincipalName);
                }
            }
        }
    }
}
