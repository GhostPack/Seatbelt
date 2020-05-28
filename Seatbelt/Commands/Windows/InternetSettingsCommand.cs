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
            WriteHost(" Hive                               Key : Value\n");
            // lists user/system internet settings, including default proxy info
            var proxySettings = RegistryUtil.GetValues(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");

            if ((proxySettings != null) && (proxySettings.Count != 0))
            {
                foreach (var kvp in proxySettings)
                {
                    yield return new InternetSettingsDTO()
                    {
                        Hive = "HKCU",
                        Key = kvp.Key,
                        Value = kvp.Value.ToString()
                    };
                }
            }

            WriteHost();

            var proxySettings2 = RegistryUtil.GetValues(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");

            if ((proxySettings2 != null) && (proxySettings2.Count != 0))
            {
                foreach (var kvp in proxySettings2)
                {
                    yield return new InternetSettingsDTO()
                    {
                        Hive = "HKLM",
                        Key = kvp.Key,
                        Value = kvp.Value.ToString()
                    };
                }
            }

            WriteHost("");

            // List user/system internet settings for zonemapkey (local, trusted, etc.) :
            // 1 = Intranet zone – sites on your local network.
            // 2 = Trusted Sites zone – sites that have been added to your trusted sites.
            // 3 = Internet zone – sites that are on the Internet.
            // 4 = Restricted Sites zone – sites that have been specifically added to your restricted sites.

            WriteHost(" Hive                               Key : Value\n");

            IDictionary<string, string> zoneMapKeys = new Dictionary<string, string>()
                                            {
                                                {"0", "My Computer" },
                                                {"1", "Local Intranet Zone"},
                                                {"2", "Trusted sites Zone"},
                                                {"3", "Internet Zone"},
                                                {"4", "Restricted Sites Zone"}
                                            };

            var zoneMapKey = RegistryUtil.GetValues(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMapKey");
            if ((zoneMapKey != null) && (zoneMapKey.Count != 0))
            {
                foreach (var kvp in zoneMapKey)
                {
                    yield return new InternetSettingsDTO()
                    {
                        Hive = "HKLM",
                        Key = kvp.Key,
                        Value = zoneMapKeys.AsEnumerable().Single(l => l.Key == kvp.Value.ToString()).Value
                    };
                }
            }

            WriteHost("");

            var zoneMapKey2 = RegistryUtil.GetValues(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMapKey");

            if ((zoneMapKey2 != null) && (zoneMapKey2.Count != 0))
            {
                foreach (var kvp in zoneMapKey2)
                {
                    yield return new InternetSettingsDTO()
                    {
                        Hive = "HKCU",
                        Key = kvp.Key,
                        Value = zoneMapKeys.AsEnumerable().Single(l => l.Key == kvp.Value.ToString()).Value
                    };
                }
            }

            WriteHost("");

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

            WriteHost("Zone settings");
            IDictionary<uint, string> zoneAuthSettings = new Dictionary<uint, string>()
                                            {
                                                {0x00000000, "Automatically logon with current username and password"},
                                                {0x00010000, "Prompt for user name and password"},
                                                {0x00020000, "Automatic logon only in the Intranet zone"},
                                                {0x00030000, "Anonymous logon"}
                                            };

            for (int i = 0; i <= 4; i++)
            {
                var zoneSettings = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\" + i.ToString(), "1A00");
                if (zoneSettings != null)
                {
                    WriteHost(zoneMapKeys.AsEnumerable().Single(l => l.Key == i.ToString()).Value + "\tSettings: " + zoneAuthSettings.AsEnumerable().Single(l => l.Key == zoneSettings).Value);
                }
            }
        }

        internal class InternetSettingsDTO : CommandDTOBase
        {
            public string Hive { get; set; }

            public string Key { get; set; }

            public string Value { get; set; }
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

                WriteLine(" {0}    {1,30} : {2}", dto.Hive, dto.Key, dto.Value);
            }
        }
    }
}
#nullable enable