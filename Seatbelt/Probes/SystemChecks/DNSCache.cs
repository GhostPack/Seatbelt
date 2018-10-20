using System;
using System.Management;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class DNSCache :IProbe
    {
        public static string ProbeName => "DNSCache";

        public  string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("DNS Cache (via WMI)");

            // lists the local DNS cache via WMI (MSFT_DNSClientCache class)
            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\standardcimv2", "SELECT * FROM MSFT_DNSClientCache");
                var data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    sb.AppendLine($"  Entry         : {result["Entry"]}");
                    sb.AppendLine($"  Name          : {result["Name"]}");
                    sb.AppendLine($"  Data          : {result["Data"]}").AppendLine();
                }
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
            {
                sb.AppendLine("  [X] 'MSFT_DNSClientCache' WMI class unavailable (minimum supported versions of Windows: 8/2012) " + ex.Message);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

    }
}
