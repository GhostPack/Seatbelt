using System;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class SysmonConfig :IProbe
    {
        public static string ProbeName => "SysmonConfig";

        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("Sysmon Configuration");


            var hashing = Helpers.GetRegValue("HKLM", "SYSTEM\\CurrentControlSet\\Services\\SysmonDrv\\Parameters", "HashingAlgorithm");
            if (!String.IsNullOrEmpty(hashing))
            {
                sb.AppendLine($"  Hashing algorithm: {hashing}");
            }

            var options = Helpers.GetRegValue("HKLM", "SYSTEM\\CurrentControlSet\\Services\\SysmonDrv\\Parameters", "Options");
            if (!String.IsNullOrEmpty(options))
            {
                sb.AppendLine($"  Options: {options}");
            }

            var sysmonRules = Helpers.GetRegValueBytes("HKLM", "SYSTEM\\CurrentControlSet\\Services\\SysmonDrv\\Parameters", "Rules");
            if (sysmonRules != null)
            {
                sb.AppendLine("  Sysmon rules: " + Convert.ToBase64String(sysmonRules));
            }

            return sb.ToString();
        }
    }
}
