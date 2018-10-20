using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public class UACSystemPolicies : IProbe
    {

        public static string ProbeName => "UACSystemPolicies";


        public string List()
        {
            var sb = new StringBuilder();

            // dump out various UAC system policies
            sb.AppendProbeHeaderLine("UAC System Policies");

            var consentPromptBehaviorAdmin = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin");
            switch (consentPromptBehaviorAdmin)
            {
                case "0":
                    sb.AppendLine($"  {"ConsentPromptBehaviorAdmin",-30} : {consentPromptBehaviorAdmin} - No prompting");
                    break;
                case "1":
                    sb.AppendLine($"  {"ConsentPromptBehaviorAdmin",-30} : {consentPromptBehaviorAdmin} - PromptOnSecureDesktop");
                    break;
                case "2":
                    sb.AppendLine($"  {"ConsentPromptBehaviorAdmin",-30} : {consentPromptBehaviorAdmin} - PromptPermitDenyOnSecureDesktop");
                    break;
                case "3":
                    sb.AppendLine($"  {"ConsentPromptBehaviorAdmin",-30} : {consentPromptBehaviorAdmin} - PromptForCredsNotOnSecureDesktop");
                    break;
                case "4":
                    sb.AppendLine($"  {"ConsentPromptBehaviorAdmin",-30} : {consentPromptBehaviorAdmin} - PromptForPermitDenyNotOnSecureDesktop");
                    break;
                case "5":
                    sb.AppendLine($"  {"ConsentPromptBehaviorAdmin",-30} : {consentPromptBehaviorAdmin} - PromptForNonWindowsBinaries");
                    break;
                default:
                    sb.AppendLine($"  {"ConsentPromptBehaviorAdmin",-30} : PromptForNonWindowsBinaries");
                    break;
            }

            var enableLua = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "EnableLUA");
            sb.AppendLine($"  {"EnableLUA",-30} : {enableLua}");

            if ((enableLua == "") || (enableLua == "0"))
            {
                sb.AppendLine("    [*] EnableLUA != 1, UAC policies disabled.");
                sb.AppendLine("    [*] Any local account can be used for lateral movement.");
            }

            var localAccountTokenFilterPolicy = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "LocalAccountTokenFilterPolicy");
            sb.AppendLine($"  {"LocalAccountTokenFilterPolicy",-30} : {localAccountTokenFilterPolicy}");
            if ((enableLua == "1") && (localAccountTokenFilterPolicy == "1"))
            {
                sb.AppendLine("    [*] LocalAccountTokenFilterPolicy set to 1.");
                sb.AppendLine("    [*] Any local account can be used for lateral movement.");
            }

            var filterAdministratorToken = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "FilterAdministratorToken");
            sb.AppendLine($"  {"FilterAdministratorToken",-30} : {filterAdministratorToken}");

            if ((enableLua == "1") && (localAccountTokenFilterPolicy != "1") && (filterAdministratorToken != "1"))
            {
                sb.AppendLine("    [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken != 1.");
                sb.AppendLine("    [*] Only the RID-500 local admin account can be used for lateral movement.");
            }

            if ((enableLua == "1") && (localAccountTokenFilterPolicy != "1") && (filterAdministratorToken == "1"))
            {
                sb.AppendLine("    [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken == 1.");
                sb.AppendLine("    [*] No local accounts can be used for lateral movement.");
            }


            return sb.ToString();
        }
    }
}
