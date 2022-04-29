#if DEBUG
using Microsoft.Win32;
using System.Collections.Generic;


namespace Seatbelt.Commands.Windows
{
    internal class SecureBootCommand : CommandBase
    {
        public override string Command => "SecureBoot";
        public override string Description => "Secure Boot configuration";
        public override CommandGroup[] Group => new[] {CommandGroup.System, CommandGroup.Remote};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public SecureBootCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var uefiSecureBootState = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State", @"UEFISecureBootEnabled");
            if (uefiSecureBootState == null)
                yield break; // Exit the function and don't return anything

            var policyPublisher = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State", @"PolicyPublisher");
            var policyVersion = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State", @"PolicyVersion");

            yield return new SecureBootDTO(
                    uefiSecureBootState == 1 ? true : false,
                    policyPublisher,
                    policyVersion
            );
        }

        internal class SecureBootDTO : CommandDTOBase
        {
            public SecureBootDTO(bool enabled, string? publisher, string? version)
            {
                Enabled = enabled;
                Publisher = publisher;
                Version = version;
            }
            public bool Enabled { get; }
            public string? Publisher { get; }
            public string? Version { get; }
        }
    }
}
#endif