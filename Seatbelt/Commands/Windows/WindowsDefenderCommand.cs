using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class WindowsDefenderCommand : CommandBase
    {
        public override string Command => "WindowsDefender";
        public override string Description => "Windows Defender settings (including exclusion locations)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public WindowsDefenderCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var pathExclusionData = ThisRunTime.GetValues(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths");
            List<string> pathExclusions = new List<string>();
            foreach (var kvp in pathExclusionData)
            {
                pathExclusions.Add(kvp.Key);
            }

            var processExclusionData = ThisRunTime.GetValues(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Processes");
            List<string> processExclusions = new List<string>();
            foreach (var kvp in processExclusionData)
            {
                processExclusions.Add(kvp.Key);
            }

            var extensionExclusionData = ThisRunTime.GetValues(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Extensions");
            List<string> extensionExclusions = new List<string>();
            foreach (var kvp in extensionExclusionData)
            {
                extensionExclusions.Add(kvp.Key);
            }

            yield return new WindowsDefenderDTO(
                pathExclusions,
                processExclusions,
                extensionExclusions
            );
        }

        internal class WindowsDefenderDTO : CommandDTOBase
        {
            public WindowsDefenderDTO(List<string> pathExclusions, List<string> processExclusions, List<string> extensionExclusions)
            {
                PathExclusions = pathExclusions;
                ProcessExclusions = processExclusions;
                ExtensionExclusions = extensionExclusions;
            }
            public List<string> PathExclusions { get; }
            public List<string> ProcessExclusions { get; }
            public List<string> ExtensionExclusions { get; }
        }

        [CommandOutputType(typeof(WindowsDefenderDTO))]
        internal class WindowsDefenderFormatter : TextFormatterBase
        {
            public WindowsDefenderFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (WindowsDefenderDTO)result;

                List<string> pathExclusions = dto.PathExclusions;
                List<string> processExclusions = dto.ProcessExclusions;
                List<string> extensionExclusions = dto.ExtensionExclusions;

                if (pathExclusions.Count != 0)
                {
                    WriteLine("\r\n  PathExclusions      : ");
                    foreach (var path in pathExclusions)
                    {
                        WriteLine($"                          {path}");
                    }
                }

                if (processExclusions.Count != 0)
                {
                    WriteLine("\r\n  ProcessExclusions   : ");
                    foreach (var process in processExclusions)
                    {
                        WriteLine($"                          {process}");
                    }
                }

                if (extensionExclusions.Count != 0)
                {
                    WriteLine("\r\n  ExtensionExclusions : ");
                    foreach (var extension in extensionExclusions)
                    {
                        WriteLine($"                          {extension}");
                    }
                }
            }
        }
    }
}
