using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

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

        private IEnumerable<string> GetCLRVersions()
        {
            var versions = new List<string>();

            var dirs = ThisRunTime.GetDirectories("\\Windows\\Microsoft.Net\\Framework\\");
            foreach (var dir in dirs)
            {
                if (System.IO.File.Exists($"{dir}\\System.dll"))
                {
                    // yes, I know I'm passing a directory and not a file. I know this is a hack :)
                    versions.Add(System.IO.Path.GetFileName(dir.TrimEnd(System.IO.Path.DirectorySeparatorChar)).TrimStart('v'));
                }
            }

            return versions;
        }

        public string GetOSVersion()
        {
            var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\cimv2", "SELECT Version FROM Win32_OperatingSystem");
            
            try
            {
                foreach (var os in wmiData.Get())
                {
                    return os["Version"].ToString();
                }
            }
            catch { }

            return "";
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var installedDotNetVersions = new List<string>();
            var installedCLRVersions = new List<string>();
            installedCLRVersions.AddRange(GetCLRVersions());

#nullable disable
            var dotNet35Version = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5", "Version");
            if (!string.IsNullOrEmpty(dotNet35Version))
            {
                installedDotNetVersions.Add(dotNet35Version);
            }

            var dotNet4Version = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Version");
            if (!string.IsNullOrEmpty(dotNet4Version))
            {
                installedDotNetVersions.Add(dotNet4Version);
            }

            int osVersionMajor = int.Parse(GetOSVersion().Split('.')[0]);
#nullable restore
            yield return new DotNetDTO(
                installedCLRVersions.ToArray(),
                installedDotNetVersions.ToArray(),
                osVersionMajor >= 10
                );
        }
    }

    class DotNetDTO : CommandDTOBase
    {
        public DotNetDTO(string[] installedCLRVersions, string[] installedDotNetVersions, bool osSupportsAmsi)
        {
            InstalledCLRVersions = installedCLRVersions;
            InstalledDotNetVersions = installedDotNetVersions;
            OsSupportsAmsi = osSupportsAmsi;
        }
        public string[] InstalledCLRVersions { get; }
        public string[] InstalledDotNetVersions { get; }
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
            var lowestVersion = dto.InstalledDotNetVersions.Min(v => (new Version(v)));
            var highestVersion = dto.InstalledDotNetVersions.Max(v => (new Version(v)));
            bool dotNetSupportsAMSI = ((highestVersion.Major >= 4) && (highestVersion.Minor >= 8));

            WriteLine("  Installed CLR Versions");
            foreach (var v in dto.InstalledCLRVersions)
            {
                WriteLine("      " + v);
            }

            WriteLine("\n  Installed .NET Versions");
            foreach (var v in dto.InstalledDotNetVersions)
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
