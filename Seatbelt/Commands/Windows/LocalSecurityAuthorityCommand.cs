using Microsoft.Win32;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class LocalSecurityAuthorityCommand : CommandBase
    {
        public override string Command => "LSASettings";
        public override string Description => "LSA settings (including auth packages)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public LocalSecurityAuthorityCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var settings = ThisRunTime.GetValues(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Lsa");

            if ((settings != null) && (settings.Count != 0))
            {
                foreach (var kvp in settings)
                {
                    if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.String"))
                    {
                        var result = string.Join(",", (string[])kvp.Value);

                        yield return new LocalSecurityAuthorityDTO(kvp.Key, result);
                    }
                    else if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.Byte"))
                    {
                        var result = System.BitConverter.ToString((byte[])kvp.Value);
                        yield return new LocalSecurityAuthorityDTO(kvp.Key, result);
                    }
                    else
                    {
                        yield return new LocalSecurityAuthorityDTO(kvp.Key, kvp.Value.ToString());
                    }
                }
            }
        }

        internal class LocalSecurityAuthorityDTO : CommandDTOBase
        {
            public LocalSecurityAuthorityDTO(string key, string value)
            {
                Key = key;
                Value = value;
            }
            public string Key { get; }
            public string Value { get; }
        }

        [CommandOutputType(typeof(LocalSecurityAuthorityDTO))]
        internal class LocalSecurityAuthorityFormatter : TextFormatterBase
        {
            public LocalSecurityAuthorityFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (LocalSecurityAuthorityDTO)result;

                WriteLine("  {0,-30} : {1}", dto.Key, dto.Value);

                if (Regex.IsMatch(dto.Key, "Security Packages") && Regex.IsMatch(dto.Value, @".*wdigest.*"))
                {
                    WriteLine("    [*] WDigest is enabled - plaintext password extraction is possible!");
                }

                if (dto.Key.Equals("RunAsPPL", System.StringComparison.InvariantCultureIgnoreCase) && dto.Value == "1")
                {
                    WriteLine("    [*] LSASS Protected Mode is enabled! You will not be able to access lsass.exe's memory easily.");
                }

                if (dto.Key.Equals("DisableRestrictedAdmin", System.StringComparison.InvariantCultureIgnoreCase) && dto.Value == "0")
                {
                    WriteLine("    [*] RDP Restricted Admin Mode is enabled! You can use pass-the-hash to access RDP on this system.");
                }

                if (dto.Key.Equals("TokenLeakDetectDelaySecs", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    WriteLine($"    [*] TokenLeakDetectDelaySecs is set to '{dto.Value}' - logon sessions will be cleared after this number of seconds!");
                }
            }
        }
    }
}
