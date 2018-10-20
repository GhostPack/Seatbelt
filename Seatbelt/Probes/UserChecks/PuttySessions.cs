using System;
using System.Text;
using Microsoft.Win32;

namespace Seatbelt.Probes.UserChecks
{
    public class PuttySessions : IProbe
    {

        public static string ProbeName => "PuttySessions";


        public string List()
        {
            var sb = new StringBuilder();

            // extracts saved putty sessions and basic configs (via the registry)
            if (Helpers.IsHighIntegrity())
                RunForHighIntegrity(sb);
            else
                RunForOtherIntegrity(sb);

            return sb.ToString();
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Putty Saved Session Information (All Users)");

            var SIDs = Registry.Users.GetSubKeyNames();
            foreach (var SID in SIDs)
            {
                if (!SID.StartsWith("S-1-5") || SID.EndsWith("_Classes")) continue;

                var subKeys = Helpers.GetRegSubkeys("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\", SID));

                foreach (var sessionName in subKeys)
                {
                    sb.AppendLine($"    {"User SID",-20}  :  {SID}");
                    sb.AppendLine($"    {"SessionName",-20}  :  {sessionName}");

                    string[] keys =
                    {
                        "HostName",
                        "UserName",
                        "PublicKeyFile",
                        "PortForwardings",
                        "ConnectionSharing"
                    };

                    foreach (var key in keys)
                    {
                        var result = Helpers.GetRegValue("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\{1}", SID, sessionName), key);

                        if (!String.IsNullOrEmpty(result))
                            sb.AppendLine($"    {key,-20}  :  {result}");
                    }

                    sb.AppendLine();
                }
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Putty Saved Session Information (Current User)");

            var subKeys = Helpers.GetRegSubkeys("HKCU", "Software\\SimonTatham\\PuTTY\\Sessions\\");
            foreach (var sessionName in subKeys)
            {
                sb.AppendLine($"    {"SessionName",-20}  :  {sessionName}");

                string[] keys =
                {
                    "HostName",
                    "UserName",
                    "PublicKeyFile",
                    "PortForwardings",
                    "ConnectionSharing"
                };

                foreach (var key in keys)
                {
                    var result = Helpers.GetRegValue("HKCU", String.Format("Software\\SimonTatham\\PuTTY\\Sessions\\{0}", sessionName), key);

                    if (!String.IsNullOrEmpty(result))
                        sb.AppendLine($"    {key,-20}  :  {result}");

                }

                sb.AppendLine();
            }
        }
    }
}
