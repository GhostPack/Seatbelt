using System;
using System.Collections.Generic;
using System.Linq;
using Seatbelt.Interop;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands.Windows
{
    internal class OptionalFeaturesCommand : CommandBase
    {
        public override string Command => "OptionalFeatures";
        public override string Description => "List Optional Features/Roles (via WMI)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public OptionalFeaturesCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var results = new List<OptionalFeaturesCommandDTO>();
            var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "SELECT Name,Caption,InstallState FROM Win32_OptionalFeature");
            var featureList = wmiData.Get();

            WriteHost("{0,-8} {1,-50} {2}", "State", "Name", "Caption");

            foreach (var feature in featureList)
            {
                var state = (OptionalFeatureState)Enum.Parse(typeof(OptionalFeatureState), feature["InstallState"].ToString());

                if(Runtime.FilterResults && state != OptionalFeatureState.Enabled)
                    continue;
                
                results.Add(new OptionalFeaturesCommandDTO(
                    feature["Name"].ToString(),
                    feature["Caption"].ToString(),
                    state));
            }

            foreach (var result in results.OrderBy(r => r.Name))
            {
                yield return result;
            }
        }
    }

    internal class OptionalFeaturesCommandDTO : CommandDTOBase
    {
        public OptionalFeaturesCommandDTO(string name, string caption, OptionalFeatureState state)
        {
            Name = name;
            Caption = caption;
            State = state;
        }
        public string Name { get; }
        public string Caption { get; }
        public OptionalFeatureState State { get; }
    }

    internal enum OptionalFeatureState
    {
        Enabled = 1,
        Disabled = 2,
        Absent = 3,
        Unknown = 4
    }

    [CommandOutputType(typeof(OptionalFeaturesCommandDTO))]
    internal class OptionalFeatureTextFormatter : TextFormatterBase
    {
        public OptionalFeatureTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (OptionalFeaturesCommandDTO)result;
            WriteLine("{0,-8} {1,-50} {2}", dto.State, dto.Name, dto.Caption);
        }
    }
}
