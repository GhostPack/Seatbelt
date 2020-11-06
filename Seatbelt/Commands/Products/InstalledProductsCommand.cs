using Microsoft.Win32;
using System;
using System.Collections.Generic;


namespace Seatbelt.Commands.Windows
{
    internal class InstalledProductsCommand : CommandBase
    {
        public override string Command => "InstalledProducts";
        public override string Description => "Installed products via the registry";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public InstalledProductsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            string[] productKeys = { @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" };

            foreach (var productKey in productKeys)
            {
                var architecture = "x86";
                if (productKey.Equals(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    architecture = "x64";
                }

                var subkeyNames = ThisRunTime.GetSubkeyNames(RegistryHive.LocalMachine, productKey) ?? new string[] { };
                foreach(var subkeyName in subkeyNames)
                {
                    var DisplayName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"{productKey}\\{subkeyName}", "DisplayName");
                    var DisplayVersion = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"{productKey}\\{subkeyName}", "DisplayVersion");
                    var Publisher = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"{productKey}\\{subkeyName}", "Publisher");
                    var InstallDateStr = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"{productKey}\\{subkeyName}", "InstallDateStr");
                    var InstallDate = new DateTime();

                    if (InstallDateStr != null && !String.IsNullOrEmpty(InstallDateStr))
                    {
                        try
                        {
                            var year = InstallDateStr.Substring(0, 4);
                            var month = InstallDateStr.Substring(4, 2);
                            var day = InstallDateStr.Substring(6, 2);
                            var date = $"{year}-{month}-{day}";
                            InstallDate = DateTime.Parse(date);
                        }
                        catch { }
                    }

                    if (DisplayName != null && !String.IsNullOrEmpty(DisplayName))
                    {
                        yield return new InstalledProductsDTO(
                            DisplayName,
                            DisplayVersion,
                            Publisher,
                            InstallDate,
                            architecture
                        );
                    }
                }
            }
        }

        internal class InstalledProductsDTO : CommandDTOBase
        {
            public InstalledProductsDTO(string displayName, string? displayVersion, string? publisher, DateTime installDate, string architecture)
            {
                DisplayName = displayName;
                DisplayVersion = displayVersion;
                Publisher = publisher;
                InstallDate = installDate;
                Architecture = architecture;
            }
            public string DisplayName { get; }

            public string? DisplayVersion { get; }

            public string? Publisher { get; }

            public DateTime InstallDate { get; }

            public string Architecture { get; }
        }
    }
}
