using System.Collections.Generic;
using Microsoft.Win32;

namespace Seatbelt.Commands.Windows
{
    internal class WindowsAutoLogonCommand : CommandBase
    {
        public override string Command => "WindowsAutoLogon";
        public override string Description => "Registry autologon information";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public Runtime ThisRunTime;
        public override bool SupportRemote => true;

        public WindowsAutoLogonCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        } 

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var winlogonPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";
            var strDefaultDomainName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, winlogonPath, "DefaultDomainName");

            var strDefaultUserName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, winlogonPath, "DefaultUserName");

            var strDefaultPassword = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, winlogonPath, "DefaultPassword");

            var strAltDefaultDomainName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, winlogonPath, "AltDefaultDomainName");

            var strAltDefaultUserName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, winlogonPath, "AltDefaultUserName");

            var strAltDefaultPassword = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, winlogonPath, "AltDefaultPassword");

            yield return new WindowsAutoLogonDTO(
                strDefaultDomainName,
                strDefaultUserName,
                strDefaultPassword,
                strAltDefaultDomainName,
                strAltDefaultUserName,
                strAltDefaultPassword
            );
        }

        internal class WindowsAutoLogonDTO : CommandDTOBase
        {
            public WindowsAutoLogonDTO(string? defaultDomainName, string? defaultUserName, string? defaultPassword, string? altDefaultDomainName, string? altDefaultUserName, string? altDefaultPassword)
            {
                DefaultDomainName = defaultDomainName;
                DefaultUserName = defaultUserName;
                DefaultPassword = defaultPassword;
                AltDefaultDomainName = altDefaultDomainName;
                AltDefaultUserName = altDefaultUserName;
                AltDefaultPassword = altDefaultPassword;    
            }
            public string? DefaultDomainName { get; }
            public string? DefaultUserName { get; }
            public string? DefaultPassword { get; }
            public string? AltDefaultDomainName { get; }
            public string? AltDefaultUserName { get; }
            public string? AltDefaultPassword { get; }
        }
    }
}