#nullable disable
using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;
using System.Collections.Generic;
using System.Linq;

namespace Seatbelt.Commands.Windows
{
    internal class InternetSettingsCommand : CommandBase
    {
        public override string Command => "InternetSettings";
        public override string Description => "Internet settings including proxy configs and zones configuration";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false; // TODO remote

        public InternetSettingsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var result = new InternetSettingsDTO();

            // lists user/system internet settings, including default proxy info
            var keyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            var proxySettings = RegistryUtil.GetValues(RegistryHive.CurrentUser, keyPath);
            foreach (var kvp in proxySettings)
            {
                result.GeneralSettings.Add(new InternetSettingsKey(
                    "HKCU",
                    keyPath,
                    kvp.Key,
                    kvp.Value.ToString(),
                    null));
            }

            keyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            var proxySettings2 = RegistryUtil.GetValues(RegistryHive.LocalMachine, keyPath);
            foreach (var kvp in proxySettings2)
            {
                result.GeneralSettings.Add(new InternetSettingsKey(
                    "HKLM",
                    keyPath,
                    kvp.Key,
                    kvp.Value.ToString(),
                    null));
            }


            // List user/system internet settings for zonemapkey (local, trusted, etc.) :
            // 1 = Intranet zone – sites on your local network.
            // 2 = Trusted Sites zone – sites that have been added to your trusted sites.
            // 3 = Internet zone – sites that are on the Internet.
            // 4 = Restricted Sites zone – sites that have been specifically added to your restricted sites.


            IDictionary<string, string> zoneMapKeys = new Dictionary<string, string>()
                                            {
                                                {"0", "My Computer" },
                                                {"1", "Local Intranet Zone"},
                                                {"2", "Trusted Sites Zone"},
                                                {"3", "Internet Zone"},
                                                {"4", "Restricted Sites Zone"}
                                            };

            keyPath = @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMapKey";
            var zoneMapKey = RegistryUtil.GetValues(RegistryHive.LocalMachine, keyPath);
            foreach (var kvp in zoneMapKey.AsEnumerable())
            {
                result.ZoneMaps.Add(new InternetSettingsKey(
                    "HKLM",
                    keyPath,
                    kvp.Key,
                    kvp.Value.ToString(),
                    zoneMapKeys.AsEnumerable().Single(l => l.Key == kvp.Value.ToString()).Value
                ));
            }

            var zoneMapKey2 = RegistryUtil.GetValues(RegistryHive.CurrentUser, keyPath);
            foreach (var kvp in zoneMapKey2.AsQueryable())
            {
                result.ZoneMaps.Add(new InternetSettingsKey(
                    "HKCU",
                    keyPath,
                    kvp.Key,
                    kvp.Value.ToString(),
                    zoneMapKeys.AsEnumerable().Single(l => l.Key == kvp.Value.ToString()).Value
                ));
            }

            // List Zones settings with automatic logons

            /**
             * HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\{0..4}\1A00
             * Logon setting (1A00) may have any one of the following values (hexadecimal):
             * Value    Setting
             *  ---------------------------------------------------------------
             * 0x00000000 Automatically logon with current username and password
             * 0x00010000 Prompt for user name and password
             * 0x00020000 Automatic logon only in the Intranet zone
             * 0x00030000 Anonymous logon
            **/

            IDictionary<uint, string> zoneAuthSettings = new Dictionary<uint, string>()
                                            {
                                                {0x00000000, "Automatically logon with current username and password"},
                                                {0x00010000, "Prompt for user name and password"},
                                                {0x00020000, "Automatic logon only in the Intranet zone"},
                                                {0x00030000, "Anonymous logon"}
                                            };

            for (int i = 0; i <= 4; i++)
            {
                keyPath = @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\" + i;
                var authSetting = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, keyPath, "1A00");
                if (authSetting != null)
                {
                    var zone = zoneMapKeys.AsEnumerable().Single(l => l.Key == i.ToString()).Value;
                    var authSettingStr = zoneAuthSettings.AsEnumerable().Single(l => l.Key == authSetting).Value;

                    result.ZoneAuthSettings.Add(new InternetSettingsKey(
                        "HKLM",
                        keyPath,
                        "1A00",
                        authSetting.ToString(),
                        $"{zone} : {authSettingStr}"
                    ));
                }
            }

            yield return result;
        }

        internal class InternetSettingsKey
        {
            public InternetSettingsKey(string hive, string path, string valueName, string value, string? interpretation)
            {
                Hive = hive;
                Path = path;
                ValueName = valueName;
                Value = value;
                Interpretation = interpretation;
            }
            public string Hive { get; }
            public string Path { get; }
            public string ValueName { get; }
            public string Value { get; }
            public string? Interpretation { get; }
        }

        internal class InternetSettingsDTO : CommandDTOBase
        {
            public List<InternetSettingsKey> GeneralSettings { get; set; } = new List<InternetSettingsKey>();
            public List<InternetSettingsKey> ZoneMaps { get; set; } = new List<InternetSettingsKey>();
            public List<InternetSettingsKey> ZoneAuthSettings { get; set; } = new List<InternetSettingsKey>();
        }

        [CommandOutputType(typeof(InternetSettingsDTO))]
        internal class InternetSettingsFormatter : TextFormatterBase
        {
            public InternetSettingsFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (InternetSettingsDTO)result;

                WriteLine("General Settings");
                WriteLine("  {0}    {1,30} : {2}\n", "Hive", "Key", "Value");
                foreach (var i in dto.GeneralSettings)
                {
                    WriteLine("  {0}    {1,30} : {2}", "HKCU", i.ValueName, i.Value);
                }

                WriteLine("\nURLs by Zone");

                if(dto.ZoneMaps.Count == 0)
                    WriteLine("  No URLs configured");

                foreach (var i in dto.ZoneMaps)
                {
                    WriteLine("  {0} {1,-30} : {2}", i.Hive, i.ValueName, i.Interpretation);
                }

                WriteLine("\nZone Auth Settings");
                foreach (var i in dto.ZoneAuthSettings)
                {
                    WriteLine($"  {i.Interpretation}");
                }

            }
        }
    }
}
#nullable enable