using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace Seatbelt.Probes.UserChecks
{
    public class MasterKeys :IProbe
    {
        public static string ProbeName => "MasterKeys";


        public string List()
        {
            var sb = new StringBuilder();

            // lists any found DPAPI master keys


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
            sb.AppendProbeHeaderLine("Checking for DPAPI Master Keys (All Users)");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users")) continue;

                var userDPAPIBasePath = String.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", dir);
                if (!Directory.Exists(userDPAPIBasePath)) continue;

                var directories = Directory.GetDirectories(userDPAPIBasePath);
                foreach (var directory in directories)
                {
                    sb.AppendLine($"    Folder       : {directory}");
                    sb.AppendLine();

                    foreach (var file in Directory.GetFiles(directory))
                    {
                        if (Regex.IsMatch(file,
                            @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                        {
                            var lastAccessed = File.GetLastAccessTime(file);
                            var lastModified = File.GetLastWriteTime(file);
                            var fileName = Path.GetFileName(file);
                            sb.AppendLine($"    MasterKey    : {fileName}");
                            sb.AppendLine($"        Accessed : {lastAccessed}");
                            sb.AppendLine($"        Modified : {lastModified}");
                            sb.AppendLine();
                        }
                    }

                    sb.AppendLine();
                }
            }

            sb.AppendLine("  [*] Use the Mimikatz \"dpapi::masterkey\" module with appropriate arguments (/pvk or /rpc) to decrypt");
            sb.AppendLine("  [*] You can also extract many DPAPI masterkeys from memory with the Mimikatz \"sekurlsa::dpapi\" module");
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for DPAPI Master Keys (Current User)");

            var userName = Environment.GetEnvironmentVariable("USERNAME");
            var userDPAPIBasePath = String.Format("{0}\\AppData\\Roaming\\Microsoft\\Protect\\", Environment.GetEnvironmentVariable("USERPROFILE"));

            if (Directory.Exists(userDPAPIBasePath))
            {
                var directories = Directory.GetDirectories(userDPAPIBasePath);
                foreach (var directory in directories)
                {
                    sb.AppendLine($"    Folder       : {directory}");
                    sb.AppendLine();

                    foreach (var file in Directory.GetFiles(directory))
                    {
                        if (Regex.IsMatch(file,
                            @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                        {
                            var lastAccessed = File.GetLastAccessTime(file);
                            var lastModified = File.GetLastWriteTime(file);
                            var fileName = Path.GetFileName(file);
                            sb.AppendLine($"    MasterKey    : {fileName}");
                            sb.AppendLine($"        Accessed : {lastAccessed}");
                            sb.AppendLine($"        Modified : {lastModified}");
                            sb.AppendLine();
                        }
                    }
                }
            }

            sb.AppendLine("  [*] Use the Mimikatz \"dpapi::masterkey\" module with appropriate arguments (/rpc) to decrypt");
        }
    }
}
