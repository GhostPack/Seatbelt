using System;
using System.Management;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class AntiVirusWMI :IProbe
    {
        public static string ProbeName => "AntiVirusWMI";
        
        public string List()
        {
            var sb = new StringBuilder();

            // lists installed VA products via WMI (the AntiVirusProduct class)

            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                var data = wmiData.Get();

                sb.AppendProbeHeaderLine("Registered Antivirus (via WMI)");

                foreach (ManagementObject virusChecker in data)
                {
                    sb.AppendLine($"  Engine        : {virusChecker["displayName"]}");
                    sb.AppendLine($"  ProductEXE    : {virusChecker["pathToSignedProductExe"]}");
                    sb.AppendLine($"  ReportingEXE  : {virusChecker["pathToSignedReportingExe"]}");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

    }
}
