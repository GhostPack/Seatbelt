using System;
using System.Collections.Generic;
using Seatbelt.Interop;

namespace Seatbelt.Commands.Windows
{
    internal class AntiVirusCommand : CommandBase
    {
        public override string Command => "AntiVirus";
        public override string Description => "Registered antivirus (via WMI)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public AntiVirusCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (String.IsNullOrEmpty(ThisRunTime.ComputerName) && Shlwapi.IsWindowsServer())
            {
                WriteHost("Cannot enumerate antivirus. root\\SecurityCenter2 WMI namespace is not available on Windows Servers");
                yield break;
            }

            // lists installed VA products via WMI (the AntiVirusProduct class)
            var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
            var data = wmiData.Get();
            foreach (var virusChecker in data)
            {
                yield return new AntiVirusDTO(
                    virusChecker["displayName"],
                    virusChecker["pathToSignedProductExe"],
                    virusChecker["pathToSignedReportingExe"]
                );
            }
        }
    }

    internal class AntiVirusDTO : CommandDTOBase
    {
        public AntiVirusDTO(object engine, object productExe, object reportingExe)
        {
            Engine = engine;
            ProductEXE = productExe;
            ReportingEXE = reportingExe;
        }
        public object Engine { get; }
        public object ProductEXE { get; }
        public object ReportingEXE { get; }
    }
}
