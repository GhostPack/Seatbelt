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
        public override string Description => "Current environment variables";
        public override CommandGroup[] Group => new[] {CommandGroup.System, CommandGroup.Remote};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public EnvironmentVariableCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var envVariables = new List<EnvironmentVariableDTO>();

            try
            {
                var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "Select UserName,Name,VariableValue from win32_environment");
                var data = wmiData.Get();

                foreach (var envVariable in data)
                {
                    envVariables.Add(new EnvironmentVariableDTO(
                        envVariable["UserName"],
                        envVariable["Name"],
                        envVariable["VariableValue"]
                    ));
                }
            }
            catch { }

            foreach (var envVariable in envVariables)
            {
                yield return envVariable;
            }

        }

        internal class EnvironmentVariableDTO : CommandDTOBase
        {
            public EnvironmentVariableDTO(object userName, object name, object value)
            {
                UserName = userName.ToString();
                Name = name.ToString();
                Value = value.ToString();
            }
            public string UserName { get; set; }
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

                WriteLine("  {0,-35}{1,-35}{2}", dto.UserName, dto.Name, dto.Value);
            }
        }
    }
}
#nullable enable