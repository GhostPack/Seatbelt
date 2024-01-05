using Seatbelt.Output.Formatters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;
using Seatbelt.Util;
using Seatbelt.Output.TextWriters;
using System.Management;

namespace Seatbelt.Commands.Windows
{
    internal class OSInfoCommand : CommandBase
    {
        public override string Command => "OSInfo";
        public override string Description => "Basic OS info (i.e. architecture, OS version, etc.)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public OSInfoCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var ProductName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "ProductName");
            var EditionID = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "EditionID");
            var ReleaseId = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "ReleaseId");
            var BuildBranch = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "BuildBranch");
            var CurrentMajorVersionNumber = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentMajorVersionNumber");
            var CurrentVersion = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentVersion");

            var BuildNumber = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuildNumber");
            var UBR = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion", "UBR");
            if (!string.IsNullOrEmpty(UBR))  // UBR is not on Win < 10
            {
                BuildNumber += ("." + UBR);
            }

            if (ThisRunTime.ISRemote())
            {
                var isHighIntegrity = true;
                var isLocalAdmin = true;

                var arch = ThisRunTime.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                var ProcessorCount = ThisRunTime.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
                var isVM = IsVirtualMachine();

                var bootTimeUtc = new DateTime();

                var strHostName = ThisRunTime.ComputerName;

                var domain = "";
                var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "Select Domain from Win32_ComputerSystem");
                var data = wmiData.Get();
                foreach (var o in data)
                {
                    var result = (ManagementObject)o;
                    domain = result["Domain"].ToString();
                }

                var machineGuid = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Cryptography", "MachineGuid");
                var temp = new string[0];

                yield return new OSInfoDTO(
                    strHostName,
                    domain,
                    "",
                    ProductName,
                    EditionID,
                    ReleaseId,
                    BuildNumber,
                    BuildBranch,
                    CurrentMajorVersionNumber.ToString(),
                    CurrentVersion,
                    arch,
                    ProcessorCount,
                    isVM,
                    bootTimeUtc,
                    isHighIntegrity,
                    isLocalAdmin,
                    DateTime.UtcNow,
                    null,
                    null,
                    null,
                    null,
                    temp,
                    machineGuid
                );
            }
            else
            {
                var isHighIntegrity = SecurityUtil.IsHighIntegrity();
                var isLocalAdmin = SecurityUtil.IsLocalAdmin();

                var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                var ProcessorCount = Environment.ProcessorCount.ToString();
                var isVM = IsVirtualMachine();

                var now = DateTime.UtcNow;
                var bootTimeUtc = now - TimeSpan.FromMilliseconds(Environment.TickCount);

                var strHostName = Dns.GetHostName();
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var dnsDomain = properties.DomainName;

                var timeZone = TimeZone.CurrentTimeZone;
                var cultureInfo = CultureInfo.InstalledUICulture;
                var inputLanguage = InputLanguage.CurrentInputLanguage.LayoutName;

                var installedInputLanguages = new List<string>();
                foreach (InputLanguage l in InputLanguage.InstalledInputLanguages)
                    installedInputLanguages.Add(l.LayoutName);

                var machineGuid = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Cryptography", "MachineGuid");


                yield return new OSInfoDTO(
                    strHostName,
                    dnsDomain,
                    WindowsIdentity.GetCurrent().Name,
                    ProductName,
                    EditionID,
                    ReleaseId,
                    BuildNumber,
                    BuildBranch,
                    CurrentMajorVersionNumber.ToString(),
                    CurrentVersion,
                    arch,
                    ProcessorCount,
                    isVM,
                    bootTimeUtc,
                    isHighIntegrity,
                    isLocalAdmin,
                    DateTime.UtcNow,
                    timeZone.StandardName,
                    timeZone.GetUtcOffset(DateTime.Now).ToString(),
                    cultureInfo.ToString(),
                    inputLanguage,
                    installedInputLanguages.ToArray(),
                    machineGuid
                );
            }
        }

        private bool IsVirtualMachine()
        {
            // returns true if the system is likely a virtual machine
            // Adapted from RobSiklos' code from https://stackoverflow.com/questions/498371/how-to-detect-if-my-application-is-running-in-a-virtual-machine/11145280#11145280

            using (var searcher = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "Select * from Win32_ComputerSystem"))
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
        public OSInfoDTO(string hostname, string domain, string username, string? productName, string? editionId, string? releaseId, string? build, string? buildBranch, string? currentMajorVersionNumber, string? currentVersion, string architecture, string processorCount, bool isVirtualMachine, DateTime bootTimeUtc, bool isHighIntegrity, bool isLocalAdmin, DateTime currentTimeUtc, string? timeZone, string? timeZoneUtcOffset, string? locale, string? inputLanguage, string[]? installedInputLanguages, string? machineGuid)
        {
            Hostname = hostname;
            Domain = domain;
            Username = username;
            ProductName = productName;
            EditionId = editionId;
            ReleaseId = releaseId;
            Build = build;
            BuildBranch = buildBranch;
            CurrentMajorVersionNumber = currentMajorVersionNumber;
            CurrentVersion = currentVersion;
            Architecture = architecture;
            ProcessorCount = processorCount;
            IsVirtualMachine = isVirtualMachine;
            BootTimeUtc = bootTimeUtc;
            IsHighIntegrity = isHighIntegrity;
            IsLocalAdmin = isLocalAdmin;
            CurrentTimeUtc = currentTimeUtc;
            TimeZone = timeZone;
            TimeZoneUtcOffset = timeZoneUtcOffset;
            Locale = locale;
            InputLanguage = inputLanguage;
            InstalledInputLanguages = installedInputLanguages;
            MachineGuid = machineGuid;
        }

        public string Hostname { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string? ProductName { get; set; }
        public string? EditionId { get; set; }
        public string? ReleaseId { get; set; }
        public string? Build { get; set; }
        public string? BuildBranch { get; set; }
        public string? CurrentMajorVersionNumber { get; set; }
        public string? CurrentVersion { get; set; }
        public string Architecture { get; set; }
        public string ProcessorCount { get; set; }
        public bool IsVirtualMachine { get; set; }
        public DateTime BootTimeUtc { get; set; }
        public bool IsHighIntegrity { get; set; }
        public bool IsLocalAdmin { get; set; }
        public DateTime CurrentTimeUtc { get; set; }
        public string? TimeZone { get; set; }
        public string? TimeZoneUtcOffset { get; set; }
        public string? Locale { get; set; }
        public string? InputLanguage;
        public string[]? InstalledInputLanguages;
        public string? MachineGuid { get; set; }
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
            
            var uptime = TimeSpan.FromTicks(dto.CurrentTimeUtc.Ticks - dto.BootTimeUtc.Ticks);
            var bootTimeStr = $"{uptime.Days:00}:{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";
            WriteLine("  {0,-30}:  {1} (Total uptime: {2})", "BootTimeUtc (approx)", dto.BootTimeUtc, bootTimeStr);
            WriteLine("  {0,-30}:  {1}", "HighIntegrity", dto.IsHighIntegrity);
            WriteLine("  {0,-30}:  {1}", "IsLocalAdmin", dto.IsLocalAdmin);

            if (!dto.IsHighIntegrity && dto.IsLocalAdmin)
            {
                WriteLine("    [*] In medium integrity but user is a local administrator - UAC can be bypassed.");
            }

            WriteLine($"  {"CurrentTimeUtc",-30}:  {dto.CurrentTimeUtc} (Local time: {dto.CurrentTimeUtc.ToLocalTime()})");
            WriteLine("  {0,-30}:  {1}", "TimeZone", dto.TimeZone);
            WriteLine("  {0,-30}:  {1}", "TimeZoneOffset", dto.TimeZoneUtcOffset);
            WriteLine("  {0,-30}:  {1}", "InputLanguage", dto.InputLanguage);
            WriteLine("  {0,-30}:  {1}", "InstalledInputLanguages", string.Join(", ", dto.InstalledInputLanguages));
            WriteLine("  {0,-30}:  {1}", "MachineGuid", dto.MachineGuid);
        }
    }
}
