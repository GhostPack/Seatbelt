using System;
using System.IO;
using System.Text;


namespace Seatbelt.Probes.UserChecks
{
    public class CheckFirefox :IProbe
    {
        public static string ProbeName => "CheckFirefox";

        public string List()
        {
            var sb = new StringBuilder();

            // checks if Firefox has a history database
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

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Firefox (Current User)");

            var userName = Environment.GetEnvironmentVariable("USERNAME");
            var userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\",
                Environment.GetEnvironmentVariable("USERPROFILE"));

            if (!Directory.Exists(userFirefoxBasePath)) return;

            var directories = Directory.GetDirectories(userFirefoxBasePath);
            foreach (var directory in directories)
            {
                var firefoxHistoryFile = String.Format("{0}\\{1}", directory, "places.sqlite");
                if (File.Exists(firefoxHistoryFile))
                {
                    sb.AppendLine($"  [*] Firefox history file exists at {firefoxHistoryFile}");
                    sb.AppendLine("      Run the 'TriageFirefox' command").AppendLine();
                }

                var firefoxCredentialFile3 = String.Format("{0}\\{1}", directory, "key3.db");
                if (File.Exists(firefoxCredentialFile3))
                {
                    sb.AppendLine($"  [*] Firefox credential file exists at {firefoxCredentialFile3}");
                    sb.AppendLine("      Run SharpWeb (https://github.com/djhohnstein/SharpWeb)").AppendLine();
                }

                var firefoxCredentialFile4 = String.Format("{0}\\{1}", directory, "key4.db");
                if (File.Exists(firefoxCredentialFile4))
                {
                    sb.AppendLine($"  [*] Firefox credential file exists at {firefoxCredentialFile4}");
                    sb.AppendLine("      Run SharpWeb (https://github.com/djhohnstein/SharpWeb)").AppendLine();
                }
            }
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Firefox (All Users)");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
            var dirs = Directory.GetDirectories(userFolder);
            foreach (var dir in dirs)
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];

                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users")) continue;

                var userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", dir);
                if (!Directory.Exists(userFirefoxBasePath)) continue;

                var found = false;

                var directories = Directory.GetDirectories(userFirefoxBasePath);
                foreach (var directory in directories)
                {
                    var firefoxHistoryFile = String.Format("{0}\\{1}", directory, "places.sqlite");
                    if (File.Exists(firefoxHistoryFile))
                    {
                        sb.AppendLine($"  [*] Firefox history file exists at {firefoxHistoryFile}");
                        sb.AppendLine("      Run the 'TriageFirefox' command").AppendLine();

                        found = true;
                    }

                    var firefoxCredentialFile3 = String.Format("{0}\\{1}", directory, "key3.db");
                    if (File.Exists(firefoxCredentialFile3))
                    {
                        sb.AppendLine($"  [*] Firefox credential file exists at {firefoxCredentialFile3}");
                        sb.AppendLine("      Run SharpWeb (https://github.com/djhohnstein/SharpWeb)").AppendLine();

                        found = true;
                    }

                    var firefoxCredentialFile4 = String.Format("{0}\\{1}", directory, "key4.db");
                    if (File.Exists(firefoxCredentialFile4))
                    {
                        sb.AppendLine($"  [*] Firefox credential file exists at {firefoxCredentialFile4}");
                        sb.AppendLine("      Run SharpWeb (https://github.com/djhohnstein/SharpWeb)").AppendLine();

                        found = true;
                    }
                }

                if (found)
                    sb.AppendLine();
            }
        }
    }
}
