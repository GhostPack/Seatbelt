using System.Collections.Generic;
using Microsoft.Win32;

namespace Seatbelt.Commands
{
    internal class SccmClientCommand : CommandBase
    {
        public override string Command => "SCCM";
        public override string Description => "System Center Configuration Manager (SCCM) settings, if applicable";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public SccmClientCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            yield return new SccmClientDTO(
                ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\CCMSetup", "LastValidMP"),
                ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SMS\Mobile Client", "AssignedSiteCode"),
                ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SMS\Mobile Client", "ProductVersion"),
                ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SMS\Mobile Client", "LastSuccessfulInstallParams")  // Sometimes contains the fallback server's hostname
            );
        }
    }

    internal class SccmClientDTO : CommandDTOBase
    {
        public SccmClientDTO(string? server, string? siteCode, string? productVersion, string? lastSuccessfulInstallParams)
        {
            Server = server;
            SiteCode = siteCode;
            ProductVersion = productVersion;
            LastSuccessfulInstallParams = lastSuccessfulInstallParams;
        }
        public string? Server { get; }
        public string? SiteCode { get; }
        public string? ProductVersion { get; }
        public string? LastSuccessfulInstallParams { get; }
    }
}