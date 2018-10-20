using System;
using System.Management;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class AppLockerSettings : IProbe
    {
        public static string ProbeName => "AppLockerSettings";

        // @_RastaMouse
        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("AppLocker Settings");

            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT Name, State FROM win32_service WHERE Name = 'AppIDSvc'");
                var data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    sb.AppendLine($"  [*] {result["Name"]} is {result["State"]}").AppendLine();
                }

                var keys = Helpers.GetRegSubkeys("HKLM", "Software\\Policies\\Microsoft\\Windows\\SrpV2");

                if (keys.Length != 0)
                {
                    foreach (string key in keys)
                    {
                        var enforcementMode = Helpers.GetRegValue("HKLM", "Software\\Policies\\Microsoft\\Windows\\SrpV2\\" + key, "EnforcementMode");
                        if (enforcementMode != "")
                        {
                            // This is really sucky
                            if (enforcementMode == "0") { enforcementMode = "Audit Mode"; }
                            if (enforcementMode == "1") { enforcementMode = "Enforce Mode"; }

                            sb.AppendLine();
                            sb.AppendLine($"    [*] {key} is in {enforcementMode}");

                            string[] ids = Helpers.GetRegSubkeys("HKLM", "Software\\Policies\\Microsoft\\Windows\\SrpV2\\" + key);
                            foreach (string id in ids)
                            {
                                string rule = Helpers.GetRegValue("HKLM", "Software\\Policies\\Microsoft\\Windows\\SrpV2\\" + key + "\\" + id, "Value");
                                sb.AppendLine($"      [*] {rule}");
                            }

                            if (ids.Length == 0)
                            {
                                sb.AppendLine("      [*] No rules");
                            }

                        }
                        else
                        {
                            sb.AppendLine($"    [*] {key} not configured");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("  [*] AppLocker not configured");
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
