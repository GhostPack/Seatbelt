using System;
using System.Text;
using Microsoft.Win32;

namespace Seatbelt.Probes.UserChecks
{
    public class PuttySSHHostKeys : IProbe
    {

        public static string ProbeName => "PuttySSHHostKeys";


        public string List()
        {
            var sb = new StringBuilder();

            // extracts saved putty host keys (via the registry)
            if (Helpers.IsHighIntegrity())
                RunForHighIntegrity(sb);
            else
                RunForOtherIntegrity(sb);

            //Console.WriteLine("\r\n\r\n=== Putty SSH Host Key Recent Hosts ===\r\n");

            //Dictionary<string, object> sessions = GetRegValues("HKCU", "Software\\SimonTatham\\PuTTY\\SshHostKeys\\");
            //if (sessions != null)
            //{
            //    foreach (KeyValuePair<string, object> kvp in sessions)
            //    {
            //        Console.WriteLine("    {0,-10}", kvp.Key);
            //    }
            //}

            return sb.ToString();
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Putty SSH Host Hosts (All Users)");

            var SIDs = Registry.Users.GetSubKeyNames();
            foreach (var SID in SIDs)
            {
                if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                {
                    var hostKeys = Helpers.GetRegValues("HKU",
                        String.Format("{0}\\Software\\SimonTatham\\PuTTY\\SshHostKeys\\", SID));
                    if ((hostKeys != null) && (hostKeys.Count != 0))
                    {
                        sb.AppendLine($"    {SID} :");
                        foreach (var kvp in hostKeys)
                        {
                            sb.AppendLine($"      {kvp.Key,-10}");
                        }
                    }
                }
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Putty SSH Host Key Recent Hosts (Current User)");

            var hostKeys = Helpers.GetRegValues("HKCU", "Software\\SimonTatham\\PuTTY\\SshHostKeys\\");
            if ((hostKeys != null) && (hostKeys.Count != 0))
            {
                foreach (var kvp in hostKeys)
                {
                    Console.WriteLine($"    {kvp.Key,-10}");
                }
            }
        }

    }
}
