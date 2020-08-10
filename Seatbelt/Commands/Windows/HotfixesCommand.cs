using System;
using System.Collections.Generic;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands.Windows
{
    internal class HotfixCommand : CommandBase
    {
        public override string Command => "Hotfixes";
        public override string Description => "Installed hotfixes (via WMI)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public HotfixCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // Lists installed hotfixes via WMI (the Win32_QuickFixEngineering class)
            // This is similar to PowerShell's Get-Hotfix
            //  Note:   This class returns only the updates supplied by Component Based Servicing (CBS).
            //          Updates supplied by Microsoft Windows Installer (MSI) or the Windows update site (https://update.microsoft.com) are not returned.
            // Translation: this only shows (usually) _Windows_ updates, not all _Microsoft_ updates. For that use the "MicrosoftUpdates" command.

            WriteHost("Enumerating Windows Hotfixes. For *all* Microsoft updates, use the 'MicrosoftUpdates' command.\r\n");

            var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_QuickFixEngineering");
            var data = wmiData.Get();
            foreach (var hotfix in data)
            {
                DateTime? InstalledOn;
                try
                {
                    InstalledOn = Convert.ToDateTime(hotfix["InstalledOn"].ToString()).ToUniversalTime();
                }
                catch
                {
                    InstalledOn = null;
                }
                yield return new HotfixDTO(
                    hotfix["HotFixID"].ToString(),
                    InstalledOn,
                    hotfix["Description"].ToString(),
                    hotfix["InstalledBy"].ToString()
                );
            }
        }
    }

    internal class HotfixDTO : CommandDTOBase
    {
        public HotfixDTO(string hotFixID, DateTime? installedOnUTC, string description, string installedBy)
        {
            HotFixID = hotFixID;
            InstalledOnUTC = installedOnUTC;
            Description = description;
            InstalledBy = installedBy;
        }
        public string HotFixID { get; set; }
        public DateTime? InstalledOnUTC { get; set; }
        public string Description { get; set; }
        public string InstalledBy { get; set; }
    }

    [CommandOutputType(typeof(HotfixDTO))]
    internal class HotfixTextFormatter : TextFormatterBase
    {
        public HotfixTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (HotfixDTO)result;

            WriteLine($"  {dto.HotFixID,-10} {dto.InstalledOnUTC?.ToLocalTime(),-22} {dto.Description,-30} {dto.InstalledBy}");
        }
    }
}
