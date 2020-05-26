using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Seatbelt.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.AccessControl;

namespace Seatbelt.Util
{
    public enum RegistryHiveType
    {
        X86,
        X64
    }

    [Flags]
    public enum RegistryAccessMask
    {
        QueryValue = 0x0001,
        SetValue = 0x0002,
        CreateSubKey = 0x0004,
        EnumerateSubKeys = 0x0008,
        Notify = 0x0010,
        CreateLink = 0x0020,
        WoW6432 = 0x0200,
        Wow6464 = 0x0100,
        Write = 0x20006,
        Read = 0x20019,
        Execute = 0x20019,
        AllAccess = 0xF003F
    }

    public class RegistryKeyValue
    {
        public RegistryKeyValue(string path, RegistryValueKind kind, object value)
        {
            Path = path;
            Kind = kind;
            Value = value;
        }
        public string Path { get; }
        public RegistryValueKind Kind { get; }
        public object Value { get; }
    }

    public static class RegistryUtil
    {
        // For 3.5 compat. Taken from https://stackoverflow.com/questions/26217199/what-are-some-alternatives-to-registrykey-openbasekey-in-net-3-5
        public static RegistryKey? OpenBaseKey(RegistryHive registryHive, RegistryHiveType registryType)
        {
            var _hiveKeys = new Dictionary<RegistryHive, UIntPtr>
            {
                { RegistryHive.ClassesRoot, new UIntPtr(0x80000000u) },
                { RegistryHive.CurrentConfig, new UIntPtr(0x80000005u) },
                { RegistryHive.CurrentUser, new UIntPtr(0x80000001u) },
                { RegistryHive.DynData, new UIntPtr(0x80000006u) },
                { RegistryHive.LocalMachine, new UIntPtr(0x80000002u) },
                { RegistryHive.PerformanceData, new UIntPtr(0x80000004u) },
                { RegistryHive.Users, new UIntPtr(0x80000003u) }
            };

            var _accessMasks = new Dictionary<RegistryHiveType, RegistryAccessMask>
            {
                { RegistryHiveType.X64, RegistryAccessMask.Wow6464 },
                { RegistryHiveType.X86, RegistryAccessMask.WoW6432 }
            };

            if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major <= 5)
                throw new PlatformNotSupportedException(
                    "The platform or operating system must be Windows XP or later.");

            var hiveKey = _hiveKeys[registryHive];
            var flags = RegistryAccessMask.QueryValue | RegistryAccessMask.EnumerateSubKeys | _accessMasks[registryType];

            var result = Advapi32.RegOpenKeyEx(hiveKey, string.Empty, 0, (uint)flags, out var keyHandlePointer);
            if (result == 0)
            {
                var safeRegistryHandleType = typeof(SafeHandleZeroOrMinusOneIsInvalid).Assembly.GetType("Microsoft.Win32.SafeHandles.SafeRegistryHandle");
                var safeRegistryHandleConstructor = safeRegistryHandleType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IntPtr), typeof(bool) }, null); // .NET < 4
                if (safeRegistryHandleConstructor == null)
                {
                    safeRegistryHandleConstructor = safeRegistryHandleType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(IntPtr), typeof(bool) },
                        null); // .NET >= 4
                }

                var keyHandle = safeRegistryHandleConstructor.Invoke(new object[] { keyHandlePointer, true });
                var net3Constructor = typeof(RegistryKey).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { safeRegistryHandleType, typeof(bool) }, null);
                var net4Constructor = typeof(RegistryKey).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IntPtr), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }, null);
                object key;

                if (net4Constructor != null)
                {
                    key = net4Constructor.Invoke(new object[] { keyHandlePointer, true, false, false, hiveKey == _hiveKeys[RegistryHive.PerformanceData] });
                }
                else if (net3Constructor != null)
                {
                    key = net3Constructor.Invoke(new object[] { keyHandle, true });
                }
                else
                {
                    var keyFromHandleMethod = typeof(RegistryKey).GetMethod("FromHandle", BindingFlags.Static | BindingFlags.Public, null, new[] { safeRegistryHandleType }, null);
                    key = keyFromHandleMethod.Invoke(null, new object[] { keyHandle });
                }

                var field = typeof(RegistryKey).GetField("keyName", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(key, string.Empty);
                }

                return (RegistryKey)key;
            }

            if (result == 2)  // NOT_FOUND
            {
                return null;
            }

            throw new Win32Exception(result);
        }

        private static RegistryKeyValue? GetValue(RegistryHive hive, string path, string value, RegistryHiveType view = RegistryHiveType.X64)
        {
            var regKey = OpenBaseKey(hive, view)?.OpenSubKey(path, RegistryKeyPermissionCheck.Default, RegistryRights.QueryValues);
            var regKeyValue = regKey?.GetValue(value);
            
            if (regKey == null || regKeyValue == null)
                return null;

            var kind = regKey.GetValueKind(value);

            return new RegistryKeyValue(
                path,
                kind,
                regKeyValue
            );
        }

        public static string? GetStringValue(RegistryHive hive, string path, string value, RegistryHiveType view = RegistryHiveType.X64)
        {
            var regValue = GetValue(hive, path, value, view);

            return regValue?.Value.ToString();
        }

        public static string? GetStringValue(RegistryHive hive, string path, string value, ManagementClass wmiRegProv)
        {
            // WMI flavor
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("GetStringValue");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                inParams["sValueName"] = value;
                var outParams = wmiRegProv.InvokeMethod("GetStringValue", inParams, null);
                return outParams.GetPropertyValue("sValue") == null ? null : (string)outParams.GetPropertyValue("sValue");
            }
            catch
            {
                return "";
            }
        }

        public static string[] GetMultiStringValue(RegistryHive hive, string path, string value, ManagementClass wmiRegProv)
        {
            // WMI flavor
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("GetMultiStringValue");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                inParams["sValueName"] = value;
                var outParams = wmiRegProv.InvokeMethod("GetMultiStringValue", inParams, null);
                return outParams.GetPropertyValue("sValue") == null ? new string[] { } : (string[])outParams.GetPropertyValue("sValue");
            }
            catch
            {
                return new string[] { };
            }
        }

        public static string? GetExpandedStringValue(RegistryHive hive, string path, string value, ManagementClass wmiRegProv)
        {
            // WMI flavor
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("GetExpandedStringValue");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                inParams["sValueName"] = value;
                var outParams = wmiRegProv.InvokeMethod("GetExpandedStringValue", inParams, null);
                return outParams.GetPropertyValue("sValue") == null ? null : (string)outParams.GetPropertyValue("sValue");
            }
            catch
            {
                return "";
            }
        }

        public static uint? GetDwordValue(RegistryHive hive, string path, string value, RegistryHiveType view = RegistryHiveType.X64)
        {
            var regValue = GetValue(hive, path, value, view);

            if (regValue == null)
                return null;

            if (uint.TryParse($"{regValue.Value}", out var output))
            {
                return output;
            }

            try
            {
                // for big values
                return unchecked((uint)((int)regValue.Value));
            }
            catch
            {
                return null;
            }
        }

        public static uint? GetDwordValue(RegistryHive hive, string path, string value, ManagementClass wmiRegProv)
        {
            // WMI flavor
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("GetDWORDValue");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                inParams["sValueName"] = value;
                var outParams = wmiRegProv.InvokeMethod("GetDWORDValue", inParams, null);
                return outParams.GetPropertyValue("uValue") == null ? null : (uint?)outParams.GetPropertyValue("uValue");
            }
            catch
            {
                return null;
            }
        }

        public static long? GetQwordValue(RegistryHive hive, string path, string value, ManagementClass wmiRegProv)
        {
            // WMI flavor
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("GetQWORDValue");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                inParams["sValueName"] = value;
                var outParams = wmiRegProv.InvokeMethod("GetQWORDValue", inParams, null);
                return outParams.GetPropertyValue("uValue") == null ? null : (long?)outParams.GetPropertyValue("uValue");
            }
            catch
            {
                return null;
            }
        }


        public static byte[]? GetBinaryValue(RegistryHive hive, string path, string value, RegistryHiveType view = RegistryHiveType.X64)
        {
            var regValue = GetValue(hive, path, value, view);

            return (byte[]?)regValue?.Value;
        }

        public static byte[]? GetBinaryValue(RegistryHive hive, string path, string value, ManagementClass wmiRegProv)
        {
            // WMI flavor
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("GetBinaryValue");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                inParams["sValueName"] = value;
                var outParams = wmiRegProv.InvokeMethod("GetBinaryValue", inParams, null);
                return outParams.GetPropertyValue("uValue") == null ? null : (byte[])outParams.GetPropertyValue("uValue");
            }
            catch
            {
                return null;
            }
        }


        public static Dictionary<string, object> GetValues(RegistryHive hive, string path, string computer = "")
        {
            // returns all registry values under the specified path in the specified hive (HKLM/HKCU)
            RegistryKey? rootHive = null;
            RegistryKey? key = null;

            try
            {
                rootHive = RegistryKey.OpenRemoteBaseKey(hive, computer);
                key = rootHive.OpenSubKey(path, false) ?? throw new Exception("Key doesn't exist");

                var valueNames = key.GetValueNames();
                var keyValuePairs = valueNames.ToDictionary(name => name, key.GetValue);
                return keyValuePairs;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
            finally
            {
                key?.Close();
                rootHive?.Close();
            }
        }

        public static Dictionary<string, object> GetValues(RegistryHive hive, string path, ManagementClass wmiRegProv)
        {
            // returns all registry values under the specified path in the specified hive (HKLM/HKCU)
            // WMI flavor

            var results = new Dictionary<string, object>();
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("EnumValues");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                var outParams = wmiRegProv.InvokeMethod("EnumValues", inParams, null);

                var valueNames = (string[])outParams.GetPropertyValue("sNames");
                var valueTypes = (int[])outParams.GetPropertyValue("Types");

                for (var i = 0; i < valueNames.Length; ++i)
                {
                    object? value = null;
                    switch (valueTypes[i])
                    {
                        case 1:
                            {
                                // String
                                value = GetStringValue(hive, $"{path}", $"{valueNames[i]}", wmiRegProv);
                                break;
                            }
                        case 2:
                            {
                                // ExpandedStringValue
                                value = GetExpandedStringValue(hive, $"{path}", $"{valueNames[i]}", wmiRegProv);
                                break;
                            }
                        case 3:
                            {
                                // Binary
                                value = GetBinaryValue(hive, $"{path}", $"{valueNames[i]}", wmiRegProv);
                                break;
                            }
                        case 4:
                            {
                                // DWORD
                                value = GetDwordValue(hive, $"{path}", $"{valueNames[i]}", wmiRegProv);
                                break;
                            }
                        case 7:
                            {
                                // MultiString
                                value = GetMultiStringValue(hive, $"{path}", $"{valueNames[i]}", wmiRegProv);
                                break;
                            }
                        case 11:
                            {
                                // QWORD
                                value = GetQwordValue(hive, $"{path}", $"{valueNames[i]}", wmiRegProv);
                                break;
                            }
                        default:
                            throw new Exception($"Unhandled WMI registry value type: {valueTypes[i]}");
                    }

                    if (value != null)
                        results.Add($"{valueNames[i]}", value);
                }
            }
            catch
            {
                // ignored
            }

            return results;
        }

        public static string[]? GetSubkeyNames(RegistryHive hive, string path, string computer = "")
        {
            // returns an array of the subkeys names under the specified path in the specified hive (HKLM/HKCU/HKU)
            RegistryKey? rootHive = null;
            RegistryKey? key = null;
            try
            {
                rootHive = RegistryKey.OpenRemoteBaseKey(hive, computer);
                key = rootHive.OpenSubKey(path, false);
                return key?.GetSubKeyNames();
            }
            catch
            {
                return null;
            }
            finally
            {
                key?.Close();
                rootHive?.Close();
            }
        }

        public static string[]? GetSubkeyNames(RegistryHive hive, string path, ManagementClass wmiRegProv)
        {
            // returns an array of the subkeys names under the specified path in the specified hive (HKLM/HKCU/HKU)
            // WMI flavor
            try
            {
                var inParams = wmiRegProv.GetMethodParameters("EnumKey");
                inParams["hDefKey"] = (UInt32)hive;
                inParams["sSubKeyName"] = path;
                var outParams = wmiRegProv.InvokeMethod("EnumKey", inParams, null);
                return outParams.GetPropertyValue("sNames") == null ? new string[] { } : (string[])outParams.GetPropertyValue("sNames");
            }
            catch
            {
                return null;
            }
        }

        public static string[] GetUserSIDs(ManagementClass wmiRegProv)
        {
            return GetSubkeyNames(RegistryHive.Users, "", wmiRegProv) ?? new string[]{};
        }

        public static string[] GetUserSIDs()
        {
            return Registry.Users.GetSubKeyNames() ?? new string[] {};
        }

        public static RegistryHive GetHive(string name)
        {
            switch (name.ToUpper())
            {
                case "HKCR":
                case "HKEY_CLASSES_ROOT":
                    return RegistryHive.ClassesRoot;

                case "HKEY_CURRENT_CONFIG":
                    return RegistryHive.CurrentConfig;

                case "HKCU":
                case "HKEY_CURRENT_USER":
                    return RegistryHive.CurrentUser;

                case "HKLM":
                case "HKEY_LOCAL_MACHINE":
                    return RegistryHive.LocalMachine;

                case "HKEY_PERFORMANCE_DATA":
                    return RegistryHive.PerformanceData;

                case "HKU":
                case "HKEY_USERS":
                    return RegistryHive.Users;

                default:
                    throw new Exception("UnknownRegistryHive");
            }
        }
    }
}
