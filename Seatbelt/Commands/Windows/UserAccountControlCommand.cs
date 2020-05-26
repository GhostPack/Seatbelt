#nullable disable
using System.Collections.Generic;
using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    internal class UserAccountControlCommand : CommandBase
    {
        public override string Command => "UAC";
        public override string Description => "UAC system policies via the registry";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public UserAccountControlCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // dump out various UAC system policies

            var consentPromptBehaviorAdmin = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin");

            var enableLua = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA");

            var localAccountTokenFilterPolicy = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "LocalAccountTokenFilterPolicy");

            var filterAdministratorToken = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "FilterAdministratorToken");

            yield return new UserAccountControlDTO()
            {
                ConsentPromptBehaviorAdmin = consentPromptBehaviorAdmin.ToString(),
                EnableLua = enableLua.ToString(),
                FilterAdministratorToken = filterAdministratorToken.ToString(),
                LocalAccountTokenFilterPolicy = localAccountTokenFilterPolicy.ToString()
            };
        }

        class UserAccountControlDTO : CommandDTOBase
        {
            public string ConsentPromptBehaviorAdmin { get; set; }

            public string EnableLua { get; set; }

            public string FilterAdministratorToken { get; set; }

            public string LocalAccountTokenFilterPolicy { get; set; }
        }

        [CommandOutputType(typeof(UserAccountControlDTO))]
        internal class UserAccountControlFormatter : TextFormatterBase
        {
            public UserAccountControlFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (UserAccountControlDTO)result;

                switch (dto.ConsentPromptBehaviorAdmin)
                {
                    case "0":
                        WriteLine("  {0,-30} : {1} - No prompting", "ConsentPromptBehaviorAdmin", dto.ConsentPromptBehaviorAdmin);
                        break;
                    case "1":
                        WriteLine("  {0,-30} : {1} - PromptOnSecureDesktop", "ConsentPromptBehaviorAdmin", dto.ConsentPromptBehaviorAdmin);
                        break;
                    case "2":
                        WriteLine("  {0,-30} : {1} - PromptPermitDenyOnSecureDesktop", "ConsentPromptBehaviorAdmin", dto.ConsentPromptBehaviorAdmin);
                        break;
                    case "3":
                        WriteLine("  {0,-30} : {1} - PromptForCredsNotOnSecureDesktop", "ConsentPromptBehaviorAdmin", dto.ConsentPromptBehaviorAdmin);
                        break;
                    case "4":
                        WriteLine("  {0,-30} : {1} - PromptForPermitDenyNotOnSecureDesktop", "ConsentPromptBehaviorAdmin", dto.ConsentPromptBehaviorAdmin);
                        break;
                    case "5":
                        WriteLine("  {0,-30} : {1} - PromptForNonWindowsBinaries", "ConsentPromptBehaviorAdmin", dto.ConsentPromptBehaviorAdmin);
                        break;
                    default:
                        WriteLine("  {0,-30} : PromptForNonWindowsBinaries", "ConsentPromptBehaviorAdmin");
                        break;
                }


                WriteLine("  {0,-30} : {1}", "EnableLUA (Is UAC enabled?)", dto.EnableLua);

                if (!dto.EnableLua.Equals("1"))
                {
                    WriteLine("    [*] EnableLUA != 1, UAC policies disabled.\n    [*] Any local account can be used for lateral movement.");
                }


                WriteLine("  {0,-30} : {1}", "LocalAccountTokenFilterPolicy", dto.LocalAccountTokenFilterPolicy);
                if (dto.EnableLua.Equals("1") && dto.LocalAccountTokenFilterPolicy.Equals("1"))
                {
                    WriteLine("    [*] LocalAccountTokenFilterPolicy set to 1.\n    [*] Any local account can be used for lateral movement.");
                }


                WriteLine("  {0,-30} : {1}", "FilterAdministratorToken", dto.FilterAdministratorToken);

                if (dto.EnableLua.Equals("1") && !dto.LocalAccountTokenFilterPolicy.Equals("1") && !dto.FilterAdministratorToken.Equals("1"))
                {
                    WriteLine("    [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken != 1.\n    [*] Only the RID-500 local admin account can be used for lateral movement.");
                }


                if (dto.EnableLua.Equals("1") && !dto.LocalAccountTokenFilterPolicy.Equals("1") && dto.FilterAdministratorToken.Equals("1"))
                {
                    WriteLine("    [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken == 1.\n    [*] No local accounts can be used for lateral movement.");
                }
            }
        }
    }
}
#nullable enable