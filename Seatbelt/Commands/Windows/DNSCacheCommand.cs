using System;
using System.Collections.Generic;
using System.Management;


namespace Seatbelt.Commands.Windows
{
    internal class DNSCacheCommand : CommandBase
    {
        public override string Command => "DNSCache";
        public override string Description => "DNS cache entries (via WMI)";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public DNSCacheCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            ManagementObjectCollection? data = null;

            // lists the local DNS cache via WMI (MSFT_DNSClientCache class)
            try
            {
                var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\standardcimv2", "SELECT * FROM MSFT_DNSClientCache");
                data = wmiData.Get();
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
            {
                WriteError(string.Format("  [X] 'MSFT_DNSClientCache' WMI class unavailable (minimum supported versions of Windows: 8/2012)", ex.Message));
            }
            catch (Exception ex)
            {
                WriteError(ex.ToString());
            }

            if (data == null)
            {
                yield break;
            }

            foreach (var o in data)
            {
                var result = (ManagementObject) o;
                yield return new DNSCacheDTO(
                    result["Entry"],
                    result["Name"],
                    result["Data"]
                );
            }

            data.Dispose();
        }
    }

    internal class DNSCacheDTO : CommandDTOBase
    {
        public DNSCacheDTO(object entry, object name, object data)
        {
            Entry = entry;
            Name = name;
            Data = data;
        }
        public object Entry { get; set; }
        public object Name { get; set; }
        public object Data { get; set; }
    }
}
