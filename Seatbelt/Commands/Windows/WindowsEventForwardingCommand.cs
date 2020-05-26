using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class WindowsEventForwardingCommand : CommandBase
    {
        public override string Command => "WindowsEventForwarding";
        public override string Description => "Windows Event Forwarding (WEF) settings via the registry";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public WindowsEventForwardingCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {

            var settings = ThisRunTime.GetValues(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\EventLog\\EventForwarding\\SubscriptionManager");

            if (settings != null)
            {
                foreach (var kvp in settings)
                {
                    if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.String"))
                    {
                        var result = string.Join(",", (string[])kvp.Value);
                        WriteHost("  {0,-30} : {1}", kvp.Key, result);
                        yield return new WindowsEventForwardingDTO(kvp.Key, result);
                    }
                    else
                    {
                        WriteHost("  {0,-30} : {1}", kvp.Key, kvp.Value);
                        yield return new WindowsEventForwardingDTO(kvp.Key, kvp.Value.ToString());
                    }
                }
            }
        }

        internal class WindowsEventForwardingDTO : CommandDTOBase
        {
            public WindowsEventForwardingDTO(string key, string value)
            {
                Key = key;
                Value = value;
            }
            public string Key { get; }
            public string Value { get; }
        }

        [CommandOutputType(typeof(WindowsEventForwardingDTO))]
        internal class WindowsEventForwardingFormatter : TextFormatterBase
        {
            public WindowsEventForwardingFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (WindowsEventForwardingDTO)result;

                WriteLine("  {0,-30} : {1}", dto.Key, dto.Value);
            }
        }
    }
}
