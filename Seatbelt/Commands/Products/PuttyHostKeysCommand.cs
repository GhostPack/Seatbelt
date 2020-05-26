using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands
{
    internal class PuttyHostKeysCommand : CommandBase
    {
        public override string Command => "PuttyHostKeys";
        public override string Description => "Saved Putty SSH host keys";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public PuttyHostKeysCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var SIDs = ThisRunTime.GetUserSIDs();

            foreach (var sid in SIDs)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                var hostKeys = ThisRunTime.GetValues(RegistryHive.Users, $"{sid}\\Software\\SimonTatham\\PuTTY\\SshHostKeys\\");
                if (hostKeys == null || hostKeys.Count == 0)
                {
                    continue;
                }

                var keys = new List<string>();

                foreach (var kvp in hostKeys)
                {
                    keys.Add($"{kvp.Key}");
                }

                yield return new PuttyHostKeysDTO(
                    sid,
                    keys
                );
            }
        }

        internal class PuttyHostKeysDTO : CommandDTOBase
        {
            public PuttyHostKeysDTO(string sid, List<string> hostKeys)
            {
                Sid = sid;
                HostKeys = hostKeys;
            }
            public string Sid { get; }
            public List<string> HostKeys { get; }
        }

        [CommandOutputType(typeof(PuttyHostKeysDTO))]
        internal class PuttyHostKeysFormatter : TextFormatterBase
        {
            public PuttyHostKeysFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (PuttyHostKeysDTO)result;

                WriteLine("  {0} :", dto.Sid);

                foreach (var hostKey in dto.HostKeys)
                {
                    WriteLine($"    {hostKey}");
                }
                WriteLine();
            }
        }
    }
}
