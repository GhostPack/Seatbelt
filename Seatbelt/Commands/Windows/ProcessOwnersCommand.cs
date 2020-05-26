#nullable disable
using System;
using System.Collections.Generic;
using System.Management;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class ProcessesOwnerCommand : CommandBase
    {
        public override string Command => "ProcessOwners";
        public override string Description => "Running non-session 0 process list with owners. For remote use.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ProcessesOwnerCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var wmiData = ThisRunTime.GetManagementObjectSearcher(@"Root\CIMV2", "SELECT * FROM Win32_Process WHERE SessionID != 0");
            var retObjectCollection = wmiData.Get();

            foreach (ManagementObject Process in retObjectCollection)
            {
                var OwnerInfo = new string[2];

                try
                {
                    Process.InvokeMethod("GetOwner", (object[])OwnerInfo);
                }
                catch { }
                var owner = "";

                if (OwnerInfo[0] != null)
                {
                    owner = String.Format("{0}\\{1}", OwnerInfo[1], OwnerInfo[0]);

                    yield return new ProcessesOwnerDTO()
                    {
                        ProcessName = Process["Name"],
                        ProcessID = Process["ProcessId"],
                        Owner = owner
                    };
                }
            }
        }

    }

    internal class ProcessesOwnerDTO : CommandDTOBase
    {
        public object ProcessName { get; set; }

        public object ProcessID { get; set; }

        public object Owner { get; set; }

    }

    [CommandOutputType(typeof(ProcessesOwnerDTO))]
    internal class ProcessOwnerFormatter : TextFormatterBase
    {
        public ProcessOwnerFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ProcessesOwnerDTO)result;
            WriteLine(" {0,-50} {1,-10} {2}", dto.ProcessName, dto.ProcessID, dto.Owner);
        }
    }
}
#nullable enable