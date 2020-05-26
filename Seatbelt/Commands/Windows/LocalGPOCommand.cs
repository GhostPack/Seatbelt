using Microsoft.Win32;
using Seatbelt.Util;
using System.Collections.Generic;


namespace Seatbelt.Commands.Windows
{
    enum GPOLink
    {
        NO_LINK_INFORMATION = 0,
        LOCAL_MACHINE = 1,
        SITE = 2,
        DOMAIN = 3,
        ORGANIZATIONAL_UNIT = 4
    }

    // ref for "flags" - https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-gpol/b0e5c9e8-e858-4a7a-a94a-4a3d0a9d87a2
    enum GPOOptions
    {
        ALL_SECTIONS_ENABLED = 0,
        USER_SECTION_DISABLED = 1,
        COMPUTER_SECTION_DISABLE = 2,
        ALL_SECTIONS_DISABLED = 3
    }

    internal class LocalGPOCommand : CommandBase
    {
        public override string Command => "LocalGPOs";
        public override string Description => "Local Group Policy settings applied to the machine/local users";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO remote


        public LocalGPOCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // reference - https://specopssoft.com/blog/things-work-group-policy-caching/

            // local machine GPOs
            var basePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\DataStore\Machine\0";
            var machineIDs = RegistryUtil.GetSubkeyNames(RegistryHive.LocalMachine, basePath) ?? new string[] {};
            foreach(var ID in machineIDs)
            {
                var settings = RegistryUtil.GetValues(RegistryHive.LocalMachine, $"{basePath}\\{ID}");

                yield return new LocalGPODTO(
                    settings["GPOName"],
                    "machine",
                    settings["DisplayName"],
                    settings["Link"],
                    settings["FileSysPath"],
                    (GPOOptions)settings["Options"],
                    (GPOLink)settings["GPOLink"],
                    settings["Extensions"]
                );
            }

            // local user GPOs
            var userGpOs = new Dictionary<string, Dictionary<string, object>>();

            var sids = Registry.Users.GetSubKeyNames();
            foreach (var sid in sids)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                var extensions = RegistryUtil.GetSubkeyNames(RegistryHive.Users, $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History");
                if ((extensions == null) || (extensions.Length == 0))
                {
                    continue;
                }

                foreach (var extension in extensions)
                {
                    var path =
                        $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History\\{extension}";
                    var UserIDs = RegistryUtil.GetSubkeyNames(RegistryHive.Users, path) ?? new string[] { };
                    foreach (var ID in UserIDs)
                    {
                        var settings = RegistryUtil.GetValues(RegistryHive.Users, $"{path}\\{ID}");

                        if (userGpOs.ContainsKey($"{settings["GPOName"]}"))
                        {
                            continue;
                        }

                        userGpOs.Add($"{settings["GPOName"]}", settings);
                    }
                }
            }

            foreach (var UserGPO in userGpOs)
            {
                yield return new LocalGPODTO(
                    UserGPO.Value["GPOName"],
                    "user",
                    UserGPO.Value["DisplayName"],
                    UserGPO.Value["Link"],
                    UserGPO.Value["FileSysPath"],
                    (GPOOptions)UserGPO.Value["Options"],
                    (GPOLink)UserGPO.Value["GPOLink"],
                    UserGPO.Value["Extensions"]
                );
            }
        }

        internal class LocalGPODTO : CommandDTOBase
        {
            public LocalGPODTO(object gpoName, object gpoType, object displayName, object link, object fileSysPath, GPOOptions options, GPOLink gpoLink, object extensions)
            {
                GPOName = gpoName;
                GPOType = gpoType;
                DisplayName = displayName;
                Link = link;
                FileSysPath = fileSysPath;
                Options = options;
                GPOLink = gpoLink;
                Extensions = extensions;    
            }
            public object GPOName { get; }
            public object GPOType { get; }
            public object DisplayName { get; }
            public object Link { get; set; }
            public object FileSysPath { get; }
            public GPOOptions Options { get; }
            public GPOLink GPOLink { get; }
            public object Extensions { get; }
        }
    }
}
