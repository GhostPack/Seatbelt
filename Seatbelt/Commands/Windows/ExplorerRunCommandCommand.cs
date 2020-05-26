#nullable disable
using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

namespace Seatbelt.Commands.Windows
{
    class ExplorerRunCommand
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    internal class ExplorerRunCommandCommand : CommandBase
    {
        public override string Command => "ExplorerRunCommands";
        public override string Description => "Recent Explorer \"run\" commands";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ExplorerRunCommandCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists recently run commands via the RunMRU registry key

            var SIDs = ThisRunTime.GetUserSIDs();

            foreach (var sid in SIDs)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                var recentCommands = ThisRunTime.GetValues(RegistryHive.Users, $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU");
                if ((recentCommands == null) || (recentCommands.Count == 0))
                {
                    continue;
                }

                var commands = new List<ExplorerRunCommand>();

                foreach (var kvp in recentCommands)
                {
                    var command = new ExplorerRunCommand();
                    command.Key = kvp.Key;
                    command.Value = $"{kvp.Value}";
                    commands.Add(command);
                }

                yield return new ExplorerRunCommandDTO(
                    sid,
                    commands
                );
            }
        }

        internal class ExplorerRunCommandDTO : CommandDTOBase
        {
            public ExplorerRunCommandDTO(string sid, List<ExplorerRunCommand> commands)
            {
                Sid = sid;
                Commands = commands;
            }
            public string Sid { get; set; }
            public List<ExplorerRunCommand> Commands { get; set; }
        }

        [CommandOutputType(typeof(ExplorerRunCommandDTO))]
        internal class ExplorerRunCommandFormatter : TextFormatterBase
        {
            public ExplorerRunCommandFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (ExplorerRunCommandDTO)result;

                WriteLine("\n  {0} :", dto.Sid);

                foreach (var runCommand in dto.Commands)
                {
                    WriteLine("    {0,-10} :  {1}", runCommand.Key, runCommand.Value);
                }
            }
        }
    }
}
#nullable enable