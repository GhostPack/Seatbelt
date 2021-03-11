using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Seatbelt.Commands.Windows;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
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

            var paramsKey = @"SYSTEM\CurrentControlSet\Services\SysmonDrv\Parameters";

            var regHashAlg = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, paramsKey, "HashingAlgorithm");
            var regOptions = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, paramsKey, "Options");
            var regSysmonRules = ThisRunTime.GetBinaryValue(RegistryHive.LocalMachine, paramsKey, "Rules");
            var installed = false;
            var hashingAlgorithm = (SysmonHashAlgorithm)0;
            var sysmonOptions = (SysmonOptions)0;
            string? b64SysmonRules = null;

            if ((regHashAlg != null) || (regOptions != null) || (regSysmonRules != null))
            {
                installed = true;
            }

            if (regHashAlg != null && regHashAlg != 0)
            {
                regHashAlg = regHashAlg & 15; // we only care about the last 4 bits
                hashingAlgorithm = (SysmonHashAlgorithm)regHashAlg;
            }

            if (regOptions != null)
            {
                sysmonOptions = (SysmonOptions)regOptions;
            }

            if (regSysmonRules != null)
            {
                b64SysmonRules = Convert.ToBase64String(regSysmonRules);
            }

            yield return new SysmonDTO(
                installed,
                hashingAlgorithm,
                sysmonOptions,
                b64SysmonRules
            );
        }

        internal class SysmonDTO : CommandDTOBase
        {
            public SysmonDTO(bool installed, SysmonHashAlgorithm hashingAlgorithm, SysmonOptions options, string? rules)
            {
                Installed = installed;
                HashingAlgorithm = hashingAlgorithm;
                Options = options;
                Rules = rules;  
            }
            public bool Installed { get; }

            public SysmonHashAlgorithm HashingAlgorithm { get; }

            public SysmonOptions Options { get; }

            public string? Rules { get; }
        }

        [CommandOutputType(typeof(SysmonDTO))]
        internal class SysmonTextFormatter : TextFormatterBase
        {
            public SysmonTextFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (SysmonDTO)result;
                
                WriteLine($"Installed:        {dto.Installed}");
                WriteLine($"HashingAlgorithm: {dto.HashingAlgorithm}");
                WriteLine($"Options:          {dto.Options}");
                WriteLine($"Rules:");

                foreach (var line in Split(dto.Rules, 100))
                {
                    WriteLine($"    {line}");
                }

            }

            private IEnumerable<string> Split(string? text, int lineLength)
            {
                if(text == null) yield break;

                var i = 0;
                for (; i < text.Length; i += lineLength)
                {
                    if (i + lineLength > text.Length)
                    {
                        break;
                    }

                    yield return text.Substring(i, lineLength);
                }

                yield return text.Substring(i);
            }
        }
    }
}