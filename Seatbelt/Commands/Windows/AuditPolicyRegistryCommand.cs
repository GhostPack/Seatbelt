using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    // TODO: If elevated, pull with Windows Audit Policy/Advanced Audit policy
    internal class AuditPolicyRegistryCommand : CommandBase
    {
        public override string Command => "AuditPolicyRegistry";
        public override string Description => "Audit settings via the registry";
        public override CommandGroup[] Group => new[] {CommandGroup.System, CommandGroup.Remote};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public AuditPolicyRegistryCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)  
        {
            // TODO: Expand the audit policy enumeration
            var settings = ThisRunTime.GetValues(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit");

            if (settings == null) 
                yield break;

            foreach (var kvp in settings)
            {
                if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType()?.ToString() == "System.String"))
                {
                    var result = string.Join(",", (string[])kvp.Value);
                    yield return new AuditPolicyDTO(
                        kvp.Key,
                        result
                    );
                }
                else
                {
                    yield return new AuditPolicyDTO(
                        kvp.Key,
                        $"{kvp.Value}"
                    );
                }
            }
        }

        internal class AuditPolicyDTO : CommandDTOBase
        {
            public AuditPolicyDTO(string key, string value)
            {
                Key = key;
                Value = value;  
            }
            public string Key { get; }
            public string Value { get; }
        }


        [CommandOutputType(typeof(AuditPolicyDTO))]
        internal class AuditPolicyTextFormatter : TextFormatterBase
        {
            public AuditPolicyTextFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (AuditPolicyDTO)result;

                WriteLine("  {0,-30} : {1}", dto.Key, dto.Value);
            }
        }
    }
}
