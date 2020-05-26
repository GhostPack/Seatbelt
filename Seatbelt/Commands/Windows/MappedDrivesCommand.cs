#nullable disable
using System.Collections.Generic;
using System.Management;


namespace Seatbelt.Commands.Windows
{
    internal class MappedDrivesCommand : CommandBase
    {
        public override string Command => "MappedDrives";
        public override string Description => "Users' mapped drives (via WMI)";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public MappedDrivesCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_networkconnection");
            var data = wmiData.Get();

            WriteHost("Mapped Drives (via WMI)\n");

            foreach (ManagementObject result in data)
            {
                yield return new MappedDrivesDTO()
                {
                    LocalName = result["LocalName"].ToString(),
                    RemoteName = result["RemoteName"].ToString(),
                    RemotePath = result["RemotePath"].ToString(),
                    Status = result["Status"].ToString(),
                    ConnectionState = result["ConnectionState"].ToString(),
                    Persistent = result["Persistent"].ToString(),
                    UserName = result["UserName"].ToString(),
                    Description = result["Description"].ToString()
                };
            }
        }
    }

    internal class MappedDrivesDTO : CommandDTOBase
    {
        public string LocalName { get; set; }
        public string RemoteName { get; set; }
        public string RemotePath { get; set; }
        public string Status { get; set; }
        public string ConnectionState { get; set; }
        public string Persistent { get; set; }
        public string UserName { get; set; }
        public string Description { get; set; }
    }
}
#nullable enable