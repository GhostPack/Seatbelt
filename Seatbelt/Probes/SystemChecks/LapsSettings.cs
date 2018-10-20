using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class LapsSettings :IProbe
    {
        public static string ProbeName => "LapsSettings";

        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("LAPS Settings");

            var admPwdEnabled = Helpers.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "AdmPwdEnabled");

            if (admPwdEnabled != "")
            {
                sb.AppendLine($"  {"LAPS Enabled",-37} : {admPwdEnabled}");

                var lapsAdminAccountName = Helpers.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "AdminAccountName");
                sb.AppendLine($"  {"LAPS Admin Account Name",-37} : {lapsAdminAccountName}");

                var lapsPasswordComplexity = Helpers.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordComplexity");
                sb.AppendLine($"  {"LAPS Password Complexity",-37} : {lapsPasswordComplexity}");

                var lapsPasswordLength = Helpers.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordLength");
                sb.AppendLine($"  {"LAPS Password Length",-37} : {lapsPasswordLength}");

                var lapsPwdExpirationProtectionEnabled = Helpers.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PwdExpirationProtectionEnabled");
                sb.AppendLine($"  {"LAPS Expiration Protection Enabled",-37} : {lapsPwdExpirationProtectionEnabled}");
            }
            else
            {
                sb.AppendLine("  [*] LAPS not installed");
            }


            return sb.ToString();
        }

    }
}
