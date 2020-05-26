#nullable disable
using Seatbelt.Output.Formatters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using Microsoft.Win32;
using Seatbelt.Util;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    internal class OSInfoCommand : CommandBase
    {
        public override string Command => "OSInfo";
        public override string Description => "Basic OS info (i.e. architecture, OS version, etc.)";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false;

        public OSInfoCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var ProductName = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "ProductName");
            var EditionID = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "EditionID");
            var ReleaseId = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "ReleaseId");
            var BuildBranch = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "BuildBranch");
            var CurrentMajorVersionNumber = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentMajorVersionNumber");
            var CurrentVersion = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentVersion");

            var BuildNumber = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuildNumber");
            var UBR = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "UBR");
            if (!string.IsNullOrEmpty(UBR))  // UBR is not on Win < 10
            {
                BuildNumber += ("." + UBR);
            }

            var isHighIntegrity = SecurityUtil.IsHighIntegrity();
            var isLocalAdmin = SecurityUtil.IsLocalAdmin();

            var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            var ProcessorCount = Environment.ProcessorCount.ToString();
            var isVM = IsVirtualMachine();

            var now = DateTime.UtcNow;
            var boot = now - TimeSpan.FromMilliseconds(Environment.TickCount);
            var BootTime = boot + TimeSpan.FromMilliseconds(Environment.TickCount);

            var strHostName = Dns.GetHostName();
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var dnsDomain = properties.DomainName;

            var timeZone = TimeZone.CurrentTimeZone;
            var cultureInfo = CultureInfo.InstalledUICulture;

            var machineGuid = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Cryptography", "MachineGuid");

            yield return new OSInfoDTO()
            {
                Hostname = strHostName,
                Domain = dnsDomain,
                Username = WindowsIdentity.GetCurrent().Name,
                ProductName = ProductName,
                EditionId = EditionID,
                ReleaseId = ReleaseId,
                Build = BuildNumber,
                BuildBranch = BuildBranch,
                CurrentMajorVersionNumber = CurrentMajorVersionNumber,
                CurrentVersion = CurrentVersion,
                Architecture = arch,
                ProcessorCount = ProcessorCount,
                IsVirtualMachine = isVM,
                BootTime = BootTime,
                IsHighIntegrity = isHighIntegrity,
                IsLocalAdmin = isLocalAdmin,
                Time = DateTime.Now,
                TimeZone = timeZone.StandardName,
                TimeZoneUtcOffset = timeZone.GetUtcOffset(DateTime.Now).ToString(),
                Locale = cultureInfo.ToString(),
                MachineGuid = machineGuid
            };
        }

        private bool IsVirtualMachine()
        {
            // returns true if the system is likely a virtual machine
            // Adapted from RobSiklos' code from https://stackoverflow.com/questions/498371/how-to-detect-if-my-application-is-running-in-a-virtual-machine/11145280#11145280

            using (var searcher = new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
            {
                using (var items = searcher.Get())
                {
                    foreach (var item in items)
                    {
                        var manufacturer = item["Manufacturer"].ToString().ToLower();
                        if ((manufacturer == "microsoft corporation" && item["Model"].ToString().ToUpperInvariant().Contains("VIRTUAL"))
                            || manufacturer.Contains("vmware")
                            || manufacturer.Contains("xen")
                            || item["Model"].ToString() == "VirtualBox")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    internal class OSInfoDTO : CommandDTOBase
    {
        public string Hostname { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string? ProductName { get; set; }
        public string? EditionId { get; set; }
        public string? ReleaseId { get; set; }
        public string Build { get; set; }
        public string? BuildBranch { get; set; }
        public string? CurrentMajorVersionNumber { get; set; }
        public string? CurrentVersion { get; set; }
        public string Architecture { get; set; }
        public string ProcessorCount { get; set; }
        public bool IsVirtualMachine { get; set; }
        public DateTime BootTime { get; set; }
        public bool IsHighIntegrity { get; set; }
        public bool IsLocalAdmin { get; set; }
        public DateTime Time { get; set; }
        public string TimeZone { get; set; }
        public string TimeZoneUtcOffset { get; set; }
        public string Locale { get; set; }
        public string MachineGuid { get; set; }
    }

    [CommandOutputType(typeof(OSInfoDTO))]
    internal class OsInfoTextFormatter : TextFormatterBase
    {
        public OsInfoTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (OSInfoDTO)result;
            WriteLine("  {0,-30}:  {1}", "Hostname", dto.Hostname);
            WriteLine("  {0,-30}:  {1}", "Domain Name", dto.Domain);
            WriteLine("  {0,-30}:  {1}", "Username", dto.Username);
            WriteLine("  {0,-30}:  {1}", "ProductName", dto.ProductName);
            WriteLine("  {0,-30}:  {1}", "EditionID", dto.EditionId);
            WriteLine("  {0,-30}:  {1}", "ReleaseId", dto.ReleaseId);
            WriteLine("  {0,-30}:  {1}", "Build", dto.Build);
            WriteLine("  {0,-30}:  {1}", "BuildBranch", dto.BuildBranch);
            WriteLine("  {0,-30}:  {1}", "CurrentMajorVersionNumber", dto.CurrentMajorVersionNumber);
            WriteLine("  {0,-30}:  {1}", "CurrentVersion", dto.CurrentVersion);
            WriteLine("  {0,-30}:  {1}", "Architecture", dto.Architecture);
            WriteLine("  {0,-30}:  {1}", "ProcessorCount", dto.ProcessorCount);
            WriteLine("  {0,-30}:  {1}", "IsVirtualMachine", dto.IsVirtualMachine);
            var uptime = TimeSpan.FromTicks(dto.Time.Ticks - dto.BootTime.Ticks);
            var bootTimeStr = $"{uptime.Days:00}:{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";
            WriteLine("  {0,-30}:  {1} ({2})", "BootTime (approx)", dto.BootTime, bootTimeStr);
            WriteLine("  {0,-30}:  {1}", "HighIntegrity", dto.IsHighIntegrity);
            WriteLine("  {0,-30}:  {1}", "IsLocalAdmin", dto.IsLocalAdmin);

            if (!dto.IsHighIntegrity && dto.IsLocalAdmin)
            {
                WriteLine("    [*] In medium integrity but user is a local administrator - UAC can be bypassed.");
            }

            WriteLine("  {0,-30}:  {1}", "Time", dto.Time);
            WriteLine("  {0,-30}:  {1}", "TimeZone", dto.TimeZone);
            WriteLine("  {0,-30}:  {1}", "TimeZoneOffset", dto.TimeZoneUtcOffset);
            WriteLine("  {0,-30}:  {1}", "Locale", dto.Locale);
            WriteLine("  {0,-30}:  {1}", "MachineGuid", dto.MachineGuid);
        }
    }
}
#nullable enable