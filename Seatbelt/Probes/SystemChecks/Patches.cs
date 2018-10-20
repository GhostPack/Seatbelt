using System;
using System.Management;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class Patches :IProbe
    {

        public static string ProbeName => "Patches";


        public string List()
        {
            var sb = new StringBuilder(2048);


            // lists current patches via WMI (win32_quickfixengineering)
            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_quickfixengineering");
                var data = wmiData.Get();

                sb.AppendProbeHeaderLine("Installed Patches (via WMI)");

                sb.AppendLine("  HotFixID   InstalledOn    Description");

                foreach (ManagementObject result in data)
                {
                    sb.AppendLine($"  {result["HotFixID"],-11}{result["InstalledOn"],-15}{result["Description"]}");
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
