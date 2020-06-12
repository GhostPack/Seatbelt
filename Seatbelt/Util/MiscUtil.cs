using System;
using System.Text.RegularExpressions;

namespace Seatbelt.Util
{
    class MiscUtil
    {
        public static DateTime UnixEpochToDateTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            try
            {
                return epoch.AddMilliseconds(unixTime).ToLocalTime();
            }
            catch
            {
                return epoch;
            }
        }

        public static Regex[] GetProcessCmdLineRegex()
        {
            // helper that returns the set of "sensitive" cmdline regular expressions
            // adapted from @djhohnstein's EventLogParser project - https://github.com/djhohnstein/EventLogParser/blob/master/EventLogParser/EventLogHelpers.cs
            // combined with scraping from https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/windows-commands

            Regex[] processCmdLineRegex =
            {
                new Regex(@"(New-Object.*System.Management.Automation.PSCredential.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(ConvertTo-SecureString.*AsPlainText.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(net(.exe)?.*user .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(net(.exe)?.*use .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(cmdkey(.exe)?.*/pass:.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(ssh(.exe)?.*-i .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(psexec(.exe)?.*-p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(psexec64(.exe)?.*-p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(winrm(.vbs)?.*-p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(winrs(.exe)?.*/p(assword)? .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(putty(.exe)?.*-pw .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(pscp(.exe)?.*-pw .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(kitty(.exe)?.*(-pw|-pass) .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(bitsadmin(.exe)?.*(/RemoveCredentials|/SetCredentials) .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(bootcfg(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(certreq(.exe)?.*-p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(certutil(.exe)?.*-p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(driverquery(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(eventcreate(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(getmac(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(gpfixup(.exe)?.*/pwd:.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(gpresult(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(mapadmin(.exe)?.*-p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(mount(.exe)?.*-p:.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(nfsadmin(.exe)?.*-p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(openfiles(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(cscript.*-w .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(schtasks(.exe)?.*(/p|/rp) .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(setx(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(systeminfo(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(takeown(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(taskkill(.exe)?.*/p .*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(tscon(.exe)?.*/password:.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(wecutil(.exe)?.*(/up|/cup|/p):.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
                new Regex(@"(wmic(.exe)?.*/password:.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline)
            };

            return processCmdLineRegex;
        }
    }
}
