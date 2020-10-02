using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;

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

        public static byte[] Combine(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
        }

        public static bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
        }

        // from https://stackoverflow.com/questions/2106877/is-there-a-faster-way-than-this-to-find-all-the-files-in-a-directory-and-all-sub
        public static IEnumerable<string> GetFileList(string fileSearchPatterns, string rootFolderPath)
        {
            // |-delineated search terms
            string[] searchPatterns = fileSearchPatterns.Split('|');

            if (Directory.Exists(rootFolderPath))
            {
                Queue<string> pending = new Queue<string>();
                pending.Enqueue(rootFolderPath);
                string[] tmp;
                while (pending.Count > 0)
                {
                    rootFolderPath = pending.Dequeue();

                    foreach (string searchPattern in searchPatterns)
                    {
                        try
                        {
                            tmp = Directory.GetFiles(rootFolderPath, searchPattern);
                        }
                        catch
                        {
                            continue;
                        }
                        for (int i = 0; i < tmp.Length; i++)
                        {
                            yield return tmp[i];
                        }
                    }

                    try
                    {
                        tmp = Directory.GetDirectories(rootFolderPath);
                    }
                    catch
                    {
                        continue;
                    }
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        pending.Enqueue(tmp[i]);
                    }
                }
            }
        }

        public static Regex[] GetProcessCmdLineRegex()
        {
            // helper that returns the set of "sensitive" cmdline regular expressions
            // adapted from @djhohnstein's EventLogParser project - https://github.com/djhohnstein/EventLogParser/blob/master/EventLogParser/EventLogHelpers.cs
            // combined with scraping from https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/windows-commands

            Regex[] processCmdLineRegex =
            {
                //new Regex(@"(New-Object.*System.Management.Automation.PSCredential.*)", RegexOptions.IgnoreCase & RegexOptions.Multiline),
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
