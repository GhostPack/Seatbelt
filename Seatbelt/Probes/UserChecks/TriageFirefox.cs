using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Seatbelt.Probes.UserChecks
{
    public class TriageFirefox :IProbe
    {
        public static string ProbeName => "TriageFirefox";


        public string List()
        {
            var sb = new StringBuilder();

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
            sb.AppendProbeHeaderLine("Firefox (All Users)");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                      dir.EndsWith("All Users")))
                {
                    var userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", dir);
                    ParseFirefoxHistory(userFirefoxBasePath, userName, sb);
                }
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Firefox (Current User)");

            var userName = Environment.GetEnvironmentVariable("USERNAME");

            var userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            ParseFirefoxHistory(userFirefoxBasePath, userName, sb);
        }

        private static void ParseFirefoxHistory(string path, string user, StringBuilder sb)
        {
            // parses a Firefox history file via regex
            if (!Directory.Exists(path)) return;

            var historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

            foreach (var directory in Directory.GetDirectories(path))
            {
                var firefoxHistoryFile = String.Format("{0}\\{1}", directory, "places.sqlite");

                sb.AppendLine($"    History ({user}):");
                sb.AppendLine();

                try
                {
                    using (var streamReader = new StreamReader(firefoxHistoryFile))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            var match = historyRegex.Match(line);
                            if (match.Success)
                            {
                                sb.AppendLine($"      {match.Groups[0].ToString().Trim()}");
                            }
                        }
                    }
                }
                catch (IOException ioException)
                {
                    sb.AppendLine();
                    sb.AppendLine("    [x] IO exception, places.sqlite file likely in use (i.e. Firefox is likely running)." + ioException.Message);
                }
                catch (Exception ex)
                {
                    sb.AppendExceptionLine(ex);
                }
            }
        }

    }
}
