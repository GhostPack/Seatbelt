using System.Collections.Generic;
using System.Management;


namespace Seatbelt.Commands.Windows
{
    internal class NetworkSharesCommand : CommandBase
    {
        public override string Command => "NetworkShares";
        public override string Description => "Network shares exposed by the machine (via WMI)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public NetworkSharesCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists current network shares for this system via WMI
            var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_Share");
            using var data = wmiData.Get();

            foreach (ManagementObject result in data)
            {
                yield return new NetworkShareDTO(
                    result["Name"],
                    result["Path"],
                    result["Description"]
                );
            }
        }
    }

    internal class NetworkShareDTO : CommandDTOBase
    {
        public NetworkShareDTO(object name, object path, object description)
        {
            Name = name;
            Path = path;
            Description = description;
        }
        public object Name { get; }
        public object Path { get; }
        public object Description { get; }
    }
}
