using System;
using System.Text;
using Microsoft.Win32;

namespace Seatbelt.Probes.UserChecks
{
    public class RecentRunCommands :IProbe
    {

        public static string ProbeName => "RecentRunCommands";


        public string List()
        {
            var sb = new StringBuilder();
            
            // lists recently run commands via the RunMRU registry key

            if (Helpers.IsHighIntegrity())
                RunForHighIntegrity(sb);
            else
                RunForOtherIntegrity(sb);

            return sb.ToString();
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Recent Typed RUN Commands (Current User)");

            var recentCommands = Helpers.GetRegValues("HKCU", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU");
            if ((recentCommands == null) || (recentCommands.Count == 0)) return;

            foreach (var kvp in recentCommands)
            {
                sb.AppendLine($"    {kvp.Key,-10} :  {kvp.Value}");
            }
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Recent Typed RUN Commands (All Users)");

            var SIDs = Registry.Users.GetSubKeyNames();
            foreach (var SID in SIDs)
            {
                if (!SID.StartsWith("S-1-5") || SID.EndsWith("_Classes")) continue;

                var recentCommands = Helpers.GetRegValues("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU", SID));

                if ((recentCommands != null) && (recentCommands.Count != 0))
                {
                    sb.AppendLine();
                    sb.AppendLine($"    {SID} :");
                    foreach (var kvp in recentCommands)
                    {
                        sb.AppendLine($"      {kvp.Key,-10} :  {kvp.Value}");
                    }
                }
            }
        }
    }
}
