using System;
using System.Text;
using Microsoft.Win32;

namespace Seatbelt.Probes.UserChecks
{
    public class SavedRDPConnections :IProbe
    {

        public static string ProbeName => "SavedRDPConnections";


        public string List()
        {
            var sb = new StringBuilder();
            
            //shows saved RDP connections, including username hints (if present)

            if (Helpers.IsHighIntegrity())
                RunForHighIntegrity(sb);
            else
                RunForOtherIntegrity(sb);

            return sb.ToString();
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Saved RDP Connection Information (Current User)");

            var subkeys = Helpers.GetRegSubkeys("HKCU", "Software\\Microsoft\\Terminal Server Client\\Servers");
            if (subkeys == null) return;

            foreach (var host in subkeys)
            {
                sb.AppendLine($"  Host           : {host}");

                var usernameHint = Helpers.GetRegValue("HKCU", String.Format("Software\\Microsoft\\Terminal Server Client\\Servers\\{0}", host), "UsernameHint");
                if (usernameHint != "")
                    sb.AppendLine($"    UsernameHint : {usernameHint}");
            }
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            var SIDs = Registry.Users.GetSubKeyNames();
            foreach (var SID in SIDs)
            {
                if (!SID.StartsWith("S-1-5") || SID.EndsWith("_Classes")) continue;

                var subkeys = Helpers.GetRegSubkeys("HKU", String.Format("{0}\\Software\\Microsoft\\Terminal Server Client\\Servers", SID));
                if (subkeys == null) continue;

                sb.AppendProbeHeaderLine($"Saved RDP Connection Information ({SID})");

                foreach (var host in subkeys)
                {
                    sb.AppendLine($"  Host           : {host}");

                    var usernameHint = Helpers.GetRegValue("HKCU",
                        String.Format("Software\\Microsoft\\Terminal Server Client\\Servers\\{0}", host), "UsernameHint");
                    if (usernameHint != "")
                        sb.AppendLine($"    UsernameHint : {usernameHint}");
                }
            }
        }
    }
}
