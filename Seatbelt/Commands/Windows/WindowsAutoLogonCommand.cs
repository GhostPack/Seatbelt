#nullable disable
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
            var strDefaultDomainName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultDomainName");

            var strDefaultUserName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultUserName");

            var strDefaultPassword = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultPassword");

            var strAltDefaultDomainName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultDomainName");

            var strAltDefaultUserName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultUserName");

            var strAltDefaultPassword = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultPassword");

            yield return new WindowsAutoLogonDTO()
            {
                DefaultDomainName = strDefaultDomainName,
                DefaultUserName = strDefaultUserName,
                DefaultPassword = strDefaultPassword,
                AltDefaultDomainName = strAltDefaultDomainName,
                AltDefaultUserName = strAltDefaultUserName,
                AltDefaultPassword = strAltDefaultPassword
            };
        }

        internal class WindowsAutoLogonDTO : CommandDTOBase
        {
            public string DefaultDomainName { get; set; }
            public string DefaultUserName { get; set; }
            public string DefaultPassword { get; set; }
            public string AltDefaultDomainName { get; set; }
            public string AltDefaultUserName { get; set; }
            public string AltDefaultPassword { get; set; }
        }
    }
}
#nullable enable