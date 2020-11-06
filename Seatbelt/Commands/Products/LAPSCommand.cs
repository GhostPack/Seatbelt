using System.Collections.Generic;
using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands
{
    internal class LAPSCommand : CommandBase
    {
        public override string Command => "LAPS";
        public override string Description => "LAPS settings, if installed";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public LAPSCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var AdmPwdEnabled = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft Services\\AdmPwd", "AdmPwdEnabled");

            if (AdmPwdEnabled != null && !AdmPwdEnabled.Equals(""))
            {
                var LAPSAdminAccountName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft Services\\AdmPwd", "AdminAccountName");

                var LAPSPasswordComplexity = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordComplexity");

                var LAPSPasswordLength = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordLength");

                var LAPSPwdExpirationProtectionEnabled = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft Services\\AdmPwd", "PwdExpirationProtectionEnabled");

                yield return new LapsDTO(
                    (AdmPwdEnabled == "1"),
                    LAPSAdminAccountName,
                    LAPSPasswordComplexity,
                    LAPSPasswordLength,
                    LAPSPwdExpirationProtectionEnabled
                );
            }
            else
            {
                yield return new LapsDTO(
                    admPwdEnabled: false,
                    lapsAdminAccountName: null,
                    lapsPasswordComplexity: null,
                    lapsPasswordLength: null,
                    lapsPwdExpirationProtectionEnabled: null
                );
            }
        }

        class LapsDTO : CommandDTOBase
        {
            public LapsDTO(bool admPwdEnabled, string? lapsAdminAccountName, string? lapsPasswordComplexity, string? lapsPasswordLength, string? lapsPwdExpirationProtectionEnabled)
            {
                AdmPwdEnabled = admPwdEnabled;
                LAPSAdminAccountName = lapsAdminAccountName;
                LAPSPasswordComplexity = lapsPasswordComplexity;
                LAPSPasswordLength = lapsPasswordLength;
                LapsPwdExpirationProtectionEnabled = lapsPwdExpirationProtectionEnabled;  
            }
            public bool AdmPwdEnabled { get; }

            public string? LAPSAdminAccountName { get; }

            public string? LAPSPasswordComplexity { get; }

            public string? LAPSPasswordLength { get; }

            public string? LapsPwdExpirationProtectionEnabled { get; }
        }

        [CommandOutputType(typeof(LapsDTO))]
        internal class LAPSFormatter : TextFormatterBase
        {
            public LAPSFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (LapsDTO)result;

                if(dto.AdmPwdEnabled.Equals("false"))
                {
                    WriteLine("  [*] LAPS not installed");
                }
                else
                {
                    WriteLine("  {0,-37} : {1}", "LAPS Enabled", dto.AdmPwdEnabled);
                    WriteLine("  {0,-37} : {1}", "LAPS Admin Account Name", dto.LAPSAdminAccountName);
                    WriteLine("  {0,-37} : {1}", "LAPS Password Complexity", dto.LAPSPasswordComplexity);
                    WriteLine("  {0,-37} : {1}", "LAPS Password Length", dto.LAPSPasswordLength);
                    WriteLine("  {0,-37} : {1}", "LAPS Expiration Protection Enabled", dto.LapsPwdExpirationProtectionEnabled);
                }
            }
        }
    }
}
