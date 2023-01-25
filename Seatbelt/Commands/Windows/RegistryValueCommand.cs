using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Seatbelt.Commands.Windows
{
    // TODO: Handle x64 vs x86 hives
    internal class RegistryValueCommand : CommandBase
    {
        public override string Command => "reg";
        public override string Description => @"Registry key values (HKLM\Software by default) argument == [Path] [intDepth] [Regex] [boolIgnoreErrors]";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false; // TODO remote, but will take some work

        public RegistryValueCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var hive = RegistryHive.LocalMachine;
            var keyPath = "Software";
            var depth = 0;
            var regex = new Regex(".");
            var ignoreErrors = true;
            var computer = "";

            if (args.Length == 0)
            {
                depth = 1;
                regex = new Regex("default");
            }
            if (args.Length >= 1)
            {
                var separatorPos = args[0].IndexOf("\\");
                if (separatorPos == -1)                   // e.g. HKLM
                {
                    hive = RegistryUtil.GetHive(args[0]);
                    keyPath = "";
                }
                else if (separatorPos == args[0].Length)   // e.g. HKLM\
                {
                    var hiveStr = args[0].Substring(0, separatorPos);
                    hive = RegistryUtil.GetHive(hiveStr);
                    keyPath = "";
                }
                else                                       // e.g. HKLM\Software
                {
                    var hiveStr = args[0].Substring(0, separatorPos);
                    hive = RegistryUtil.GetHive(hiveStr);
                    keyPath = args[0].Substring(separatorPos + 1);
                }


            }
            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out depth))
                {
                    WriteError("Could not parse depth argument");
                }
            }
            if (args.Length >= 3) { regex = new Regex(args[2], RegexOptions.IgnoreCase); }
            if (args.Length >= 4) { ignoreErrors = bool.Parse(args[3]); }
            if (args.Length >= 5) { computer = args[4]; }


            foreach (var output in EnumerateRootKey(computer, hive, keyPath, regex, depth, ignoreErrors))
            {
                yield return output;
            }
        }

        private IEnumerable<RegistryValueDTO> EnumerateRootKey(string computer, RegistryHive hive, string keyPath, Regex regex, int depth, bool ignoreErrors)
        {
            using var rootHive = RegistryKey.OpenRemoteBaseKey(hive, computer);
            using var key = rootHive.OpenSubKey(keyPath);

            foreach (var output in EnumerateRegistryKey(key, regex, depth, ignoreErrors))
            {
                yield return output;
            }
        }

        private IEnumerable<RegistryValueDTO> EnumerateRegistryKey(RegistryKey key, Regex regex, int depth, bool ignoreErrors)
        {
            if (key == null)
            {
                throw new Exception("NullRegistryHive");
            }

            if (depth < 0)
            {
                yield break;
            }

            var outputKeyPath = key.ToString()
                .Replace("HKEY_LOCAL_MACHINE", "HKLM")
                .Replace("HKEY_CURRENT_USER", "HKCU")
                .Replace("HKEY_CLASSES_ROOT", "HKCR")
                .Replace("HKEY_USERS", "HKU");

            var sddl = "";
            if (!Runtime.FilterResults)
            {
                try
                {
                    var accessControl = key.GetAccessControl();
                    sddl = accessControl.GetSecurityDescriptorSddlForm(System.Security.AccessControl.AccessControlSections.Access | System.Security.AccessControl.AccessControlSections.Owner);
                }
                catch { }
            }

            // 1) Handle key values
            // Get the default value since GetValueNames doesn't always return it
            var defaultValue = key.GetValue("");
            if (regex.IsMatch("default") || (regex.IsMatch(outputKeyPath) || (defaultValue != null && regex.IsMatch($"{defaultValue}"))))
            {
                yield return new RegistryValueDTO(
                    outputKeyPath,
                    "(default)",
                    defaultValue,
                    RegistryValueKind.String,
                    sddl
                );
            }

            foreach (var valueName in key.GetValueNames())
            {
                if (valueName == null || valueName == "")
                    continue;

                var valueKind = key.GetValueKind(valueName);
                var value = key.GetValue(valueName);

                // Skip the default value and non-matching valueNames
                if (regex.IsMatch(valueName) || regex.IsMatch($"{value}"))
                {
                    yield return new RegistryValueDTO(
                        outputKeyPath,
                        valueName,
                        value,
                        valueKind,
                        sddl
                    );
                }
            }

            // 2) Handle subkeys
            foreach (var subkeyName in key.GetSubKeyNames())
            {
                RegistryKey? subkey = null;
                try
                {
                    subkey = key.OpenSubKey(subkeyName);
                }
                catch (Exception e)
                {
                    if (!ignoreErrors)
                    {
                        throw new Exception($"Error accessing {(key + "\\" + subkeyName)}: " + e);
                    }
                }

                if (subkey == null)
                    continue;

                foreach (var result in EnumerateRegistryKey(subkey, regex, (depth - 1), ignoreErrors))
                {
                    yield return result;
                }
            }
        }
    }

    public class RegistryValueDTO : CommandDTOBase
    {
        public RegistryValueDTO(string key, string valueName, object value, object valueKind, string sddl)
        {
            Key = key;
            ValueName = valueName;
            Value = value;
            ValueKind = valueKind;
            SDDL = sddl;
        }
        public string Key { get; }
        public string ValueName { get; }
        public object Value { get; }
        public object ValueKind { get; }
        public object SDDL { get; }
    }

    [CommandOutputType(typeof(RegistryValueDTO))]
    internal class RegistryValueTextFormatter : TextFormatterBase
    {
        public RegistryValueTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            if (result == null)
            {
                return;
            }

            var dto = (RegistryValueDTO)result;

            if ((int)dto.ValueKind == (int)RegistryValueKind.MultiString)
            {
                var values = (string[])dto.Value;

                if (String.IsNullOrEmpty((string)dto.SDDL))
                {
                    WriteLine($"{dto.Key} ! {dto.ValueName} :\n{String.Join("\n", values)}");
                }
                else
                {
                    WriteLine($"{dto.Key} ! {dto.ValueName} :\n{String.Join("\n", values)}\n  {dto.SDDL}");
                }
            }
            else
            {
                if (String.IsNullOrEmpty((string)dto.SDDL))
                {
                    WriteLine($"{dto.Key} ! {dto.ValueName} : {dto.Value}");
                }
                else
                {
                    WriteLine($"{dto.Key} ! {dto.ValueName} : {dto.Value}\n  {dto.SDDL}");
                }
            }
        }
    }
}
