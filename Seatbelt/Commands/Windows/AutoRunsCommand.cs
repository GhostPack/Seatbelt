#nullable disable
using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class AutoRunsCommand : CommandBase
    {
        public override string Command => "AutoRuns";
        public override string Description => "Auto run executables/scripts/programs";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public AutoRunsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            //WriteHost("Registry Autoruns");

            string[] autorunLocations = new string[] {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunService",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceService",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunService",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"
            };

            foreach (string autorunLocation in autorunLocations)
            {
                var settings = ThisRunTime.GetValues(RegistryHive.LocalMachine, autorunLocation);

                if ((settings != null) && (settings.Count != 0))
                {
                    AutoRunDTO entry = new AutoRunDTO();

                    entry.Key = System.String.Format("HKLM:\\{0}", autorunLocation);
                    entry.Entries = new List<string>();

                    foreach (KeyValuePair<string, object> kvp in settings)
                    {
                        entry.Entries.Add(kvp.Value.ToString());
                    }

                    yield return entry;
                }
            }
        }

        internal class AutoRunDTO : CommandDTOBase
        {
            public string Key { get; set; }
            public List<string> Entries { get; set; }
        }

        [CommandOutputType(typeof(AutoRunDTO))]
        internal class AutoRunTextFormatter : TextFormatterBase
        {
            public AutoRunTextFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (AutoRunDTO)result;

                WriteLine("\n  {0} :", dto.Key);
                foreach (string entry in dto.Entries)
                {
                    WriteLine("    {0}", entry);
                }
            }
        }
    }
}
#nullable enable