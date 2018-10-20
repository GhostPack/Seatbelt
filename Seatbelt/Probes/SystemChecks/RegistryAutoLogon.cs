using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class RegistryAutoLogon : IProbe
    {

        public static string ProbeName => "RegistryAutoLogon";


        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("Registry Auto-logon Settings");

            var defaultDomainName = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultDomainName");
            if (defaultDomainName != "")
            {
                sb.AppendLine($"  {"DefaultDomainName",-23} : {defaultDomainName}");
            }

            var defaultUserName = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultUserName");
            if (defaultUserName != "")
            {
                sb.AppendLine($"  {"DefaultUserName",-23} : {defaultUserName}");
            }

            var defaultPassword = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultPassword");
            if (defaultPassword != "")
            {
                sb.AppendLine($"  {"DefaultPassword",-23} : {defaultPassword}");
            }

            var altDefaultDomainName = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultDomainName");
            if (altDefaultDomainName != "")
            {
                sb.AppendLine($"  {"AltDefaultDomainName",-23} : {altDefaultDomainName}");
            }

            var altDefaultUserName = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultUserName");
            if (altDefaultDomainName != "")
            {
                sb.AppendLine($"  {"AltDefaultUserName",-23} : {altDefaultUserName}");
            }

            var altDefaultPassword = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultPassword");
            if (altDefaultDomainName != "")
            {
                sb.AppendLine($"  {"AltDefaultPassword",-23} : {altDefaultPassword}");
            }

            return sb.ToString();
        }
    }
}
