using System.Linq;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class SystemEnvVariables : IProbe
    {
        public static string ProbeName => "SystemEnvVariables";


        public string List()
        {
            var sb = new StringBuilder();

            // dumps out current system environment variables
            sb.AppendProbeHeaderLine("System Environment Variables");
            var settings = Helpers.GetRegValues("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment");

            if (settings != null && settings.Any())
            {
                foreach (var kvp in settings)
                {
                    sb.AppendLine($"  {kvp.Key,-35} : {kvp.Value}");
                }
            }

            return sb.ToString();
        }
    }
}
