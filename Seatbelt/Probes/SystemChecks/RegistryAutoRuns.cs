using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class RegistryAutoRuns :IProbe
    {

        public static string ProbeName => "RegistryAutoRuns";


        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("Registry Autoruns");

            var autorunLocations = new string[] {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunService",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceService",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunService",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"
            };

            foreach (var autorunLocation in autorunLocations)
            {
                var settings = Helpers.GetRegValues("HKLM", autorunLocation);
                if ((settings == null) || (settings.Count == 0)) continue;

                sb.AppendLine($"  HKLM:\\{autorunLocation} :");
                foreach (var kvp in settings)
                {
                    sb.AppendLine($"    {kvp.Value}");
                }

                sb.AppendLine();

            }

            return sb.ToString();
        }
    }
}
