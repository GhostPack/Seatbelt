#nullable disable
using System.Collections.Generic;
using System.Management;
using Seatbelt.Util;


namespace Seatbelt.Commands.Windows
{
    internal class PrintersCommand : CommandBase
    {
        public override string Command => "Printers";
        public override string Description => "Installed Printers (via WMI)";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false; // could if it wasn't for the SDDL

        public PrintersCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists installed printers via WMI (the Win32_Printer class)
            var printerQuery = new ManagementObjectSearcher("SELECT * from Win32_Printer");
            foreach (var printer in printerQuery.Get())
            {
                var isDefault = (bool)printer.GetPropertyValue("Default");
                var isNetworkPrinter = (bool)printer.GetPropertyValue("Network");
                string printerSDDL = null;
                var printerName = $"{printer.GetPropertyValue("Name")}";

                try
                {
                    var info = SecurityUtil.GetSecurityInfos(printerName, Interop.Advapi32.SE_OBJECT_TYPE.SE_PRINTER);
                    printerSDDL = info.SDDL;
                }
                catch
                {
                    // eat it
                }

                yield return new InstalledPrintersDTO(
                    printerName,
                    $"{printer.GetPropertyValue("Status")}",
                    printerSDDL,
                    isDefault,
                    isNetworkPrinter
                );
            }
        }
    }

    internal class InstalledPrintersDTO : CommandDTOBase
    {
        public InstalledPrintersDTO(string name, string status, string sddl, bool isDefault, bool isNetworkPrinter)
        {
            Name = name;
            Status = status;
            Sddl = sddl;
            IsDefault = isDefault;
            IsNetworkPrinter = isNetworkPrinter;
        }
        public string Name { get; }
        public string Status { get; }
        public string Sddl { get; }
        public bool IsDefault { get; }
        public bool IsNetworkPrinter { get; }
    }
}
#nullable enable