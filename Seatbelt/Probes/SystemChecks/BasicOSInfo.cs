using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class BasicOSInfo : IProbe
    {
        public static string ProbeName => "BasicOSInfo";

        public string List()
        {
            var sb = new StringBuilder();

            // returns basic OS/host information, including:
            //      Windows version information
            //      integrity/admin levels
            //      processor count/architecture
            //      basic user and domain information
            //      whether the system is a VM
            //      etc.

            var productName = Helpers.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "ProductName");
            var editionId = Helpers.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "EditionID");
            var releaseId = Helpers.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "ReleaseId");
            var buildBranch = Helpers.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "BuildBranch");
            var currentMajorVersionNumber = Helpers.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentMajorVersionNumber");
            var currentVersion = Helpers.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentVersion");

            var isHighIntegrity = Helpers.IsHighIntegrity();
            var isLocalAdmin = Helpers.IsLocalAdmin();

            var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            var processorCount = Environment.ProcessorCount.ToString();
            var isVM = Helpers.IsVirtualMachine();

            var now = DateTime.UtcNow;
            var boot = now - TimeSpan.FromMilliseconds(Environment.TickCount);
            var bootTime = boot + TimeSpan.FromMilliseconds(Environment.TickCount);

            var strHostName = Dns.GetHostName();
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var dnsDomain = properties.DomainName;

            sb.AppendProbeHeaderLine("Basic OS Information");

            sb.AppendLine($"  {"Hostname",-30}:  {strHostName}");
            sb.AppendLine($"  {"Domain Name",-30}:  {dnsDomain}");
            sb.AppendLine($"  {"Username",-30}:  {WindowsIdentity.GetCurrent().Name}");
            sb.AppendLine($"  {"ProductName",-30}:  {productName}");
            sb.AppendLine($"  {"EditionID",-30}:  {editionId}");
            sb.AppendLine($"  {"ReleaseId",-30}:  {releaseId}");
            sb.AppendLine($"  {"BuildBranch",-30}:  {buildBranch}");
            sb.AppendLine($"  {"CurrentMajorVersionNumber",-30}:  {currentMajorVersionNumber}");
            sb.AppendLine($"  {"CurrentVersion",-30}:  {currentVersion}");
            sb.AppendLine($"  {"Architecture",-30}:  {arch}");
            sb.AppendLine($"  {"ProcessorCount",-30}:  {processorCount}");
            sb.AppendLine($"  {"IsVirtualMachine",-30}:  {isVM}");
            sb.AppendLine($"  {"BootTime (approx)",-30}:  {bootTime}");
            sb.AppendLine($"  {"HighIntegrity",-30}:  {isHighIntegrity}");
            sb.AppendLine($"  {"IsLocalAdmin",-30}:  {isLocalAdmin}");

            if (!isHighIntegrity && isLocalAdmin)
            {
                sb.AppendLine("    [*] In medium integrity but user is a local administrator- UAC can be bypassed.");
            }

            return sb.ToString();
        }
        
    }
}
