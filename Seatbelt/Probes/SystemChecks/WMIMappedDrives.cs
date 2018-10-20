using System;
using System.Management;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class WMIMappedDrives : IProbe
    {

        public static string ProbeName => "WMIMappedDrives";


        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("Mapped Drives (via WMI)");

            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_networkconnection");
                var data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                   sb.AppendLine($"  LocalName        : {result["LocalName"]}");
                   sb.AppendLine($"  RemoteName       : {result["RemoteName"]}");
                   sb.AppendLine($"  RemotePath       : {result["RemotePath"]}");
                   sb.AppendLine($"  Status           : {result["Status"]}");
                   sb.AppendLine($"  ConnectionState  : {result["ConnectionState"]}");
                   sb.AppendLine($"  Persistent       : {result["Persistent"]}");
                   sb.AppendLine($"  UserName         : {result["UserName"]}");
                   sb.AppendLine($"  Description      : {result["Description"]}\r\n");
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
