#nullable disable
using Microsoft.Win32;
using Seatbelt.Util;
using System.Collections.Generic;
using System.Management;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

namespace Seatbelt.Commands.Windows
{
    internal class AppLockerCommand : CommandBase
    {
        public override string Command => "AppLocker";
        public override string Description => "AppLocker settings, if installed";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO: impement remote

        public AppLockerCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // ref - @_RastaMouse https://rastamouse.me/2018/09/enumerating-applocker-config/
            var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT Name, State FROM win32_service WHERE Name = 'AppIDSvc'");
            var data = wmiData.Get();
            string appIdSvcState = "Service not found";

            var rules = new List<string>();

            foreach (var o in data)
            {
                var result = (ManagementObject)o;
                appIdSvcState = result["State"].ToString();
            }

            var keys = RegistryUtil.GetSubkeyNames(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\SrpV2");

            if (keys != null && keys.Length != 0)
            {
                foreach (var key in keys)
                {
                    var keyName = key;
                    var enforcementMode = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, $"Software\\Policies\\Microsoft\\Windows\\SrpV2\\{key}", "EnforcementMode");
                    var enforcementModeStr = enforcementMode switch
                    {
                        null => "not configured",
                        0 => "Audit Mode",
                        1 => "Enforce Mode",
                        _ => $"Unknown value {enforcementMode}"
                    };

                    var ids = RegistryUtil.GetSubkeyNames(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\SrpV2\\" + key);

                    foreach (var id in ids)
                    {
                        var rule = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, $"Software\\Policies\\Microsoft\\Windows\\SrpV2\\{key}\\{id}", "Value");
                        rules.Add(rule);
                    }

                    yield return new AppLockerDTO(
                        configured: true,
                        appIdSvcState,
                        keyName,
                        enforcementModeStr,
                        rules
                        );
                }
            }
            else
            {
                yield return new AppLockerDTO(
                    configured: false,
                    appIdSvcState,
                    keyName: null,
                    enforcementMode: null,
                    rules: null
                );
            }
        }

        internal class AppLockerDTO : CommandDTOBase
        {
            public AppLockerDTO(bool configured, string appIdSvcState, string? keyName, string? enforcementMode, List<string>? rules)
            {
                Configured = configured;
                AppIdSvcState = appIdSvcState;
                KeyName = keyName;
                EnforcementMode = enforcementMode;
                Rules = rules;
            }
            public bool Configured { get; }

            public string AppIdSvcState { get; }

            public string? KeyName { get; }

            public string? EnforcementMode { get; }

            public List<string>? Rules { get; }
        }

        [CommandOutputType(typeof(AppLockerDTO))]
        internal class AppLockerTextFormatter : TextFormatterBase
        {
            public AppLockerTextFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (AppLockerDTO)result;

                WriteLine("  [*] AppIDSvc service is {0}\n", dto.AppIdSvcState);
                if (dto.AppIdSvcState != "Running")
                    WriteLine("    [*] Applocker is not running because the AppIDSvc is not running\n");

                if (!dto.Configured)
                {
                    WriteLine("  [*] AppLocker not configured");
                }
                else if (dto.EnforcementMode.Equals("not configured"))
                {
                    WriteLine("    [*] {0} not configured", dto.KeyName);
                }
                else
                {
                    WriteLine("\n    [*] {0} is in {1}", dto.KeyName, dto.EnforcementMode);

                    if (dto.Rules.Count == 0)
                    {
                        WriteLine("      [*] No rules");
                    }
                    else
                    {
                        foreach (var rule in dto.Rules)
                        {
                            WriteLine("      [*] {0}", rule);
                        }
                    }
                }
            }
        }
    }
}
#nullable enable