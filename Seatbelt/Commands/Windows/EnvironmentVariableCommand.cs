#nullable disable
using Microsoft.Win32;
using Seatbelt.Util;
using System;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class EnvironmentVariableCommand : CommandBase
    {
        public override string Command => "EnvironmentVariables";
        public override string Description => "Current user environment variables";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false;

        class PathLocation
        {
            public string Type { get; set; }
            public RegistryHive Hive { get; set; }
            public string Path { get; set; }
        }

        public EnvironmentVariableCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var currentProcessVars = new SortedDictionary<string, string>();

            try
            {
                // dumps out current user environment variables
                foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    var name = (string)env.Key;
                    var value = (string)env.Value;
                    currentProcessVars[name] = value;
                }
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }

            foreach (var variable in currentProcessVars)
            {
                yield return new EnvironmentVariableDTO()
                {
                    Type = "CurrentProcess",
                    Name = variable.Key,
                    Value = variable.Value
                };
            }

            PathLocation[] locations =
            {
                new PathLocation() {
                    Type = "System",
                    Hive = RegistryHive.LocalMachine,
                    Path = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment"
                },
                new PathLocation() {
                    Type = "User",
                    Hive = RegistryHive.CurrentUser,
                    Path = "Environment"
                },
                new PathLocation() {
                    Type = "Volatile",
                    Hive = RegistryHive.CurrentUser,
                    Path = "Volatile Environment"
                },
                new PathLocation() {
                    Type = "Volatile1",
                    Hive = RegistryHive.CurrentUser,
                    Path = "Volatile Environment\\1"
                }
            };

            foreach (var location in locations)
            {
                var SystemVariables = new SortedDictionary<string, string>();
                var settings = RegistryUtil.GetValues(location.Hive, location.Path);

                foreach (var kvp in settings)
                {
                    SystemVariables[kvp.Key] = kvp.Value.ToString();
                }

                foreach (var variable in SystemVariables)
                {
                    yield return new EnvironmentVariableDTO()
                    {
                        Type = location.Type,
                        Name = variable.Key,
                        Value = variable.Value
                    };
                }
            }
        }

        internal class EnvironmentVariableDTO : CommandDTOBase
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }


        [CommandOutputType(typeof(EnvironmentVariableDTO))]
        internal class EnvironmentVariableFormatter : TextFormatterBase
        {
            public EnvironmentVariableFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (EnvironmentVariableDTO)result;

                WriteLine("  {0,-15}{1,-35}{2}", dto.Type, dto.Name, dto.Value);
            }
        }
    }
}
#nullable enable