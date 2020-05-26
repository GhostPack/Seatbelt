#nullable disable
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
            yield return new SccmClientDTO()
            {
                Server = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\CCMSetup", "LastValidMP"),
                SiteCode = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SMS\Mobile Client", "AssignedSiteCode"),
                ProductVersion = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SMS\Mobile Client", "ProductVersion"),
                LastSuccessfulInstallParams = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SMS\Mobile Client", "LastSuccessfulInstallParams"),  // Sometimes contains the fallback server's hostname
            };
        }
    }

    internal class SccmClientDTO : CommandDTOBase
    {
        public string Server { get; set; }
        public string SiteCode { get; set; }
        public string ProductVersion { get; set; }
        public string LastSuccessfulInstallParams { get; set; }
    }
}
#nullable enable