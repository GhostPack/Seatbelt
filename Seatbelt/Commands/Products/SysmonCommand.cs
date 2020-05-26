#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Seatbelt.Util;


namespace Seatbelt.Commands
{
    // TODO: Grab the version of Sysmon from its binary
    internal class SysmonCommand : CommandBase
    {
        public override string Command => "Sysmon";
        public override string Description => "Sysmon configuration from the registry";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        // hashing algorithm reference from @mattifestation's SysmonRuleParser.ps1
        //  ref - https://github.com/mattifestation/PSSysmonTools/blob/master/PSSysmonTools/Code/SysmonRuleParser.ps1#L589-L595
        [Flags]
        public enum SysmonHashAlgorithm
        {
            NotDefined = 0,
            SHA1 = 1,
            MD5 = 2,
            SHA256 = 4,
            IMPHASH = 8
        }

        [Flags]
        public enum SysmonOptions
        {
            NotDefined = 0,
            NetworkConnection = 1,
            ImageLoading = 2
        }

        public SysmonCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {

            if (!SecurityUtil.IsHighIntegrity() && !ThisRunTime.ISRemote())
            {
                WriteError("Unable to collect. Must be an administrator.");
                yield break;
            }

            var hashAlg = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\SysmonDrv\Parameters", "HashingAlgorithm");
            var options = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\SysmonDrv\Parameters", "Options");
            var sysmonRules = ThisRunTime.GetBinaryValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\SysmonDrv\Parameters", "Rules");
            var installed = false;
            var HashingAlgorithm = (SysmonHashAlgorithm)0;
            var Options = (SysmonOptions)0;
            var b64SysmonRules = "";

            if ((hashAlg != null) || (options != null) || (sysmonRules != null))
            {
                installed = true;
            }

            if (hashAlg != null && hashAlg != 0)
            {
                hashAlg = hashAlg & 15; // we only care about the last 4 bits
                HashingAlgorithm = (SysmonHashAlgorithm)hashAlg;
            }

            if (options != null)
            {
                Options = (SysmonOptions)options;
            }

            if (sysmonRules != null)
            {
                b64SysmonRules = Convert.ToBase64String(sysmonRules);
            }

            yield return new SysmonDTO()
            {
                Installed = installed,
                HashingAlgorithm = HashingAlgorithm,
                Options = Options,
                Rules = b64SysmonRules
            };
        }

        internal class SysmonDTO : CommandDTOBase
        {
            public bool Installed { get; set; }

            public SysmonHashAlgorithm HashingAlgorithm { get; set; }

            public SysmonOptions Options { get; set; }

            public string Rules { get; set; }
        }
    }
}
#nullable enable