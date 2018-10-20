using System;
using System.Management;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class NetworkShares :IProbe
    {

        public static string ProbeName => "NetworkShares";

        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("Network Shares (via WMI)");

            // lists current network shares for this system via WMI

            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_Share");
                var data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    sb.AppendLine($"  Name             : {result["Name"]}");
                    sb.AppendLine($"  Path             : {result["Path"]}");
                    sb.AppendLine($"  Description      : {result["Description"]}");
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
