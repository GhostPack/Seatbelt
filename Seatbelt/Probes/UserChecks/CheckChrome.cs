using System;
using System.IO;
using System.Text;

namespace Seatbelt.Probes.UserChecks
{
    public class CheckChrome :IProbe
    {
        public static string ProbeName => "CheckChrome";

        public string List()
        {
            var sb = new StringBuilder();

            // checks if Chrome has a history database
            try
            {

                if (Helpers.IsHighIntegrity())
                    RunForHighIntegrity(sb);
                else
                    RunForOtherIntegrity(sb);

            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Chrome (All Users)");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
            var dirs = Directory.GetDirectories(userFolder);
            foreach (var dir in dirs)
            {
                var found = false;
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                      dir.EndsWith("All Users")))
                {
                    var userChromeHistoryPath =
                        String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", dir);
                    if (File.Exists(userChromeHistoryPath))
                    {
                        sb.AppendLine($"  [*] Chrome history file exists at {userChromeHistoryPath}");
                        sb.AppendLine("      Run the 'TriageChrome' command").AppendLine();
                        found = true;
                    }

                    var userChromeCookiesPath =
                        String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", dir);
                    if (File.Exists(userChromeCookiesPath))
                    {
                        sb.AppendLine($"  [*] Chrome cookies database exists at {userChromeCookiesPath}");
                        sb.AppendLine("      Run the Mimikatz \"dpapi::chrome\" module").AppendLine();
                        found = true;
                    }

                    var userChromeLoginDataPath =
                        String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", dir);
                    if (File.Exists(userChromeLoginDataPath))
                    {
                        sb.AppendLine($"  [*] Chrome saved login database exists at {userChromeLoginDataPath}");
                        sb.AppendLine("      Run the Mimikatz \"dpapi::chrome\" module or SharpWeb (https://github.com/djhohnstein/SharpWeb)");
                        found = true;
                    }

                    if (found)
                        sb.AppendLine();
                }
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Chrome (Current User)");

            var userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            if (File.Exists(userChromeHistoryPath))
            {
                sb.AppendLine($"  [*] Chrome history file exists at {userChromeHistoryPath}");
                sb.AppendLine("      Run the 'TriageChrome' command").AppendLine();
            }

            var userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            if (File.Exists(userChromeCookiesPath))
            {
                sb.AppendLine($"  [*] Chrome cookies database exists at {userChromeCookiesPath}");
                sb.AppendLine("      Run the Mimikatz \"dpapi::chrome\" module").AppendLine();
            }

            var userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            if (File.Exists(userChromeLoginDataPath))
            {
                sb.AppendLine($"  [*] Chrome saved login database exists at {userChromeLoginDataPath}");
                sb.AppendLine("      Run the Mimikatz \"dpapi::chrome\" module or SharpWeb (https://github.com/djhohnstein/SharpWeb)");
            }
        }
    }
}
