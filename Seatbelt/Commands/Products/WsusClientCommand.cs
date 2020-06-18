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
            yield return new WsusClientDTO(
                (ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "UseWUServer") == 1),
                ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUServer"),
                ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "UpdateServiceUrlAlternate"),
                ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUStatusServer")
            );
        }
    }

    internal class WsusClientDTO : CommandDTOBase
    {
        public WsusClientDTO(bool? useWuServer, string? server, string? alternateServer, string? statisticsServer)
        {
            UseWUServer = useWuServer;
            Server = server;
            AlternateServer = alternateServer;
            StatisticsServer = statisticsServer;    
        }
        public bool? UseWUServer { get; set; }
        public string? Server { get; set; }
        public string? AlternateServer { get; set; }
        public string? StatisticsServer { get; set; }
    }
}
