using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Seatbelt.Commands.Windows
{
    internal class DotNetCommand : CommandBase
    {
        public override string Command => "DotNet";
        public override string Description => "DotNet versions";
        public override CommandGroup[] Group => new[] {CommandGroup.System, CommandGroup.Remote};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public DotNetCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var installedVersions = new List<string>();
#nullable disable
            var dotNet35Version = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5", "Version");
            if (!string.IsNullOrEmpty(dotNet35Version))
            {
                installedVersions.Add(dotNet35Version);
            }

            var dotNet4Version = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Version");
            if (!string.IsNullOrEmpty(dotNet4Version))
            {
                installedVersions.Add(dotNet4Version);
            }
#nullable restore
            yield return new DotNetDTO(
                installedVersions.ToArray(),
                Environment.OSVersion.Version.Major >= 10
                );
        }
    }

    class DotNetDTO : CommandDTOBase
    {
        public DotNetDTO(string[] installedVersions, bool osSupportsAmsi)
        {
            InstalledVersions = installedVersions;
            OsSupportsAmsi = osSupportsAmsi;
        }
        public string[] InstalledVersions { get; }
        public bool OsSupportsAmsi { get; }
    }

    [CommandOutputType(typeof(DotNetDTO))]
    internal class DotNetTextFormatter : TextFormatterBase
    {
        public DotNetTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (DotNetDTO)result;
            var lowestVersion = dto.InstalledVersions.Min(v => (new Version(v)));
            var highestVersion = dto.InstalledVersions.Max(v => (new Version(v)));
            bool dotNetSupportsAMSI = ((highestVersion.Major >= 4) && (highestVersion.Minor >= 8));

            WriteLine("  Installed .NET Versions");
            foreach (var v in dto.InstalledVersions)
            {
                WriteLine("      " + v);
            }

            WriteLine("\n  Anti-Malware Scan Interface (AMSI)");
            WriteLine($"      OS supports AMSI           : {dto.OsSupportsAmsi}");
            WriteLine($"     .NET version support AMSI   : {dotNetSupportsAMSI}");

            if((highestVersion.Major == 4) && (highestVersion.Minor >= 8))
            {
                WriteLine($"        [!] The highest .NET version is enrolled in AMSI!");
            }

            if (
                dto.OsSupportsAmsi &&
                dotNetSupportsAMSI &&
                ((
                    (lowestVersion.Major == 3)
                ) ||
                ((lowestVersion.Major == 4) && (lowestVersion.Minor < 8)))
            )
            {
                WriteLine($"        [*] You can invoke .NET version {lowestVersion.Major}.{lowestVersion.Minor} to bypass AMSI.");
            }
        }
    }
}
