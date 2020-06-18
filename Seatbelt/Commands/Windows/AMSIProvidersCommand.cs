#nullable disable
using Microsoft.Win32;
using System.Collections.Generic;

namespace Seatbelt.Commands.Windows
{
    internal class AMSIProviderCommand : CommandBase
    {
        public override string Command => "AMSIProviders";
        public override string Description => "Providers registered for AMSI";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public AMSIProviderCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var providers = ThisRunTime.GetSubkeyNames(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\AMSI\Providers");
            foreach (var provider in providers)
            {
                var ProviderPath = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"SOFTWARE\\Classes\\CLSID\\{provider}\\InprocServer32", "");

                yield return new AMSIProviderDTO(
                    provider,
                    ProviderPath
                );
            }
        }

        internal class AMSIProviderDTO : CommandDTOBase
        {
            public AMSIProviderDTO(string guid, string? providerPath)
            {
                GUID = guid;
                ProviderPath = providerPath;    
            }
            public string GUID { get; set; }
            public string? ProviderPath { get; set; }
        }
    }
}
#nullable enable