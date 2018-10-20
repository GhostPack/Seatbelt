using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.UserChecks
{
    public class LogonSessions :IProbe
    {

        public static string ProbeName => "LogonSessions";


        public string List()
        {
            var sb = new StringBuilder();

            if (!Helpers.IsHighIntegrity())
                RunForHighIntegrity(sb);
            else
                RunForOtherIntegrity(sb);

            return sb.ToString();
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            // https://www.pinvoke.net/default.aspx/secur32.lsalogonuser

            // list user logons combined with logon session data via WMI

            var userDomainRegex = new Regex(@"Domain=""(.*)"",Name=""(.*)""");
            var logonIdRegex = new Regex(@"LogonId=""(\d+)""");

            sb.AppendProbeHeaderLine("Logon Sessions (via WMI)");

            var logonMap = new Dictionary<string, string[]>();

            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_LoggedOnUser");
                var data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    var m = logonIdRegex.Match(result["Dependent"].ToString());
                    if (m.Success == false) continue;

                    var logonId = m.Groups[1].ToString();
                    var m2 = userDomainRegex.Match(result["Antecedent"].ToString());
                    if (m2.Success == false) continue;

                    var domain = m2.Groups[1].ToString();
                    var user = m2.Groups[2].ToString();
                    logonMap.Add(logonId, new string[] { domain, user });
                }

                var wmiData2 = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_LogonSession");
                var data2 = wmiData2.Get();

                foreach (ManagementObject result2 in data2)
                {
                    var userDomain = logonMap[result2["LogonId"].ToString()];
                    var domain = userDomain[0];
                    var userName = userDomain[1];
                    var startTime = ManagementDateTimeConverter.ToDateTime(result2["StartTime"].ToString());

                    var logonType =
                        String.Format("{0}", ((SECURITY_LOGON_TYPE)(Int32.Parse(result2["LogonType"].ToString()))));

                    sb.AppendLine($"  UserName                 : {userName}");
                    sb.AppendLine($"  Domain                   : {domain}");
                    sb.AppendLine($"  LogonId                  : {result2["LogonId"]}");
                    sb.AppendLine($"  LogonType                : {logonType}");
                    sb.AppendLine($"  AuthenticationPackage    : {result2["AuthenticationPackage"]}");
                    sb.AppendLine($"  StartTime                : {startTime}");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            // heavily adapted from Jared Hill:
            //      https://www.codeproject.com/Articles/18179/Using-the-Local-Security-Authority-to-Enumerate-Us

            sb.AppendProbeHeaderLine("Logon Sessions (via LSA)");

            try
            {
                var systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate
                UInt64 count;
                var luidPtr = IntPtr.Zero;
                var iter = luidPtr;

                uint ret = NativeMethods.LsaEnumerateLogonSessions(out count, out luidPtr); // get an array of pointers to LUIDs

                for (ulong i = 0; i < count; i++)
                {
                    IntPtr sessionData;

                    ret = NativeMethods.LsaGetLogonSessionData(luidPtr, out sessionData);
                    SECURITY_LOGON_SESSION_DATA data =
                        (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                    // if we have a valid logon
                    if (data.PSiD != IntPtr.Zero)
                    {
                        // get the account username
                        var username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();

                        // convert the security identifier of the user
                        var sid = new SecurityIdentifier(data.PSiD);

                        // domain for this account
                        var domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();

                        // authentication package
                        var authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();

                        // logon type
                        SECURITY_LOGON_TYPE logonType = (SECURITY_LOGON_TYPE)data.LogonType;

                        // datetime the session was logged in
                        var logonTime = systime.AddTicks((long)data.LoginTime);

                        // user's logon server
                        var logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();

                        // logon server's DNS domain
                        var dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();

                        // user principalname
                        var upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                        sb.AppendLine($"  UserName                 : {username}");
                        sb.AppendLine($"  Domain                   : {domain}");
                        sb.AppendLine($"  LogonId                  : {data.LoginID.LowPart}");
                        sb.AppendLine($"  UserSID                  : {sid.AccountDomainSid}");
                        sb.AppendLine($"  AuthenticationPackage    : {authpackage}");
                        sb.AppendLine($"  LogonType                : {logonType}");
                        sb.AppendLine($"  LogonType                : {logonTime}");
                        sb.AppendLine($"  LogonServer              : {logonServer}");
                        sb.AppendLine($"  LogonServerDNSDomain     : {dnsDomainName}");
                        sb.AppendLine($"  UserPrincipalName        : {upn}");
                        sb.AppendLine();
                    }

                    // move the pointer forward
                    luidPtr = (IntPtr)((long)luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                    NativeMethods.LsaFreeReturnBuffer(sessionData);
                }

                NativeMethods.LsaFreeReturnBuffer(luidPtr);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }
        }
    }
}
