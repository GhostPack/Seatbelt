#nullable disable
using System.Collections.Generic;
using Microsoft.Win32;

namespace Seatbelt.Commands
{
    internal class WsusClientCommand : CommandBase
    {
        public override string Command => "WSUS";
        public override string Description => "Windows Server Update Services (WSUS) settings, if applicable";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public WsusClientCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            yield return new WsusClientDTO()
            {
                UseWUServer = (ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "UseWUServer") == 1),
                Server = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUServer"),
                AlternateServer = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "UpdateServiceUrlAlternate"),
                StatisticsServer = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUStatusServer"),
            };
        }
    }

    internal class WsusClientDTO : CommandDTOBase
    {
        public bool UseWUServer { get; set; }
        public string Server { get; set; }
        public string AlternateServer { get; set; }
        public string StatisticsServer { get; set; }
    }
}
#nullable enable