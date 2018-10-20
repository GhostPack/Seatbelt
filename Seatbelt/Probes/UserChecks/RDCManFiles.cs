using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Seatbelt.Probes.UserChecks
{
    public class RDCManFiles :IProbe
    {

        public static string ProbeName => "RDCManFiles";


        public string List()
        {
            var sb = new StringBuilder();

            // lists any found files in Local\Microsoft\Credentials\*

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
            sb.AppendProbeHeaderLine("Checking for RDCMan Settings Files (All Users)");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
            var found = false;

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users")) continue;

                var userRDManFile = String.Format("{0}\\AppData\\Local\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings", dir);
                if (!File.Exists(userRDManFile)) continue;

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(userRDManFile);

                // grab the recent RDG files
                var filesToOpen = xmlDoc.GetElementsByTagName("FilesToOpen");
                var items = filesToOpen[0].ChildNodes;
                var node = items[0];

                var lastAccessed = File.GetLastAccessTime(userRDManFile);
                var lastModified = File.GetLastWriteTime(userRDManFile);

                sb.AppendLine($"    RDCManFile   : {userRDManFile}");
                sb.AppendLine($"    Accessed     : {lastAccessed}");
                sb.AppendLine($"    Modified     : {lastModified}");

                foreach (XmlNode rdgFile in items)
                {
                    found = true;
                    sb.AppendLine($"      .RDG File  : {rdgFile.InnerText}");
                }

                sb.AppendLine();
            }

            if (found)
            {
                sb.AppendLine("  [*] Use the Mimikatz \"dpapi::rdg\" module with appropriate /masterkey to decrypt any .rdg files");
                sb.AppendLine("  [*] You can extract many DPAPI masterkeys from memory with the Mimikatz \"sekurlsa::dpapi\" module");
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for RDCMan Settings Files (Current User)");

            var found = false;
            var userName = Environment.GetEnvironmentVariable("USERNAME");
            var userRDManFile = String.Format("{0}\\AppData\\Local\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings", Environment.GetEnvironmentVariable("USERPROFILE"));

            if (File.Exists(userRDManFile))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(userRDManFile);

                // grab the recent RDG files
                var filesToOpen = xmlDoc.GetElementsByTagName("FilesToOpen");
                var items = filesToOpen[0].ChildNodes;
                var node = items[0];

                var lastAccessed = File.GetLastAccessTime(userRDManFile);
                var lastModified = File.GetLastWriteTime(userRDManFile);

                sb.AppendLine($"    RDCManFile   : {userRDManFile}");
                sb.AppendLine($"    Accessed     : {lastAccessed}");
                sb.AppendLine($"    Modified     : {lastModified}");

                foreach (XmlNode rdgFile in items)
                {
                    found = true;
                    sb.AppendLine($"      .RDG File  : {rdgFile.InnerText}");
                }

                sb.AppendLine();
            }

            if (found)
            {
                sb.AppendLine("  [*] Use the Mimikatz \"dpapi::rdg\" module with appropriate /masterkey to decrypt any .rdg files");
                sb.AppendLine("  [*] You can extract many DPAPI masterkeys from memory with the Mimikatz \"sekurlsa::dpapi\" module");
            }
        }
    }
}
