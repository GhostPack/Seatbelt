using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using Seatbelt.WindowsInterop;
using static Seatbelt.WindowsInterop.NativeMethods;

namespace Seatbelt
{
    public static class Helpers
    {

        // helpers (registry, UNC paths, etc.)

        public static IntPtr OpenServer(string name)
            => WTSOpenServer(name);

        public static void CloseServer(IntPtr serverHandle)
            => WTSCloseServer(serverHandle);

        public static string TranslateSid(string sid)
        {
            // adapted from http://www.pinvoke.net/default.aspx/advapi32.LookupAccountSid
            var accountSid = new SecurityIdentifier(sid);
            var accountSidByes = new byte[accountSid.BinaryLength];
            accountSid.GetBinaryForm(accountSidByes, 0);

            var name = new StringBuilder();
            var cchName = (uint)name.Capacity;
            var referencedDomainName = new StringBuilder();
            var cchReferencedDomainName = (uint)referencedDomainName.Capacity;
            SID_NAME_USE sidUse;

            var err = 0;
            if (!LookupAccountSid(null, accountSidByes, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
            {
                err = Marshal.GetLastWin32Error();
                if (err == ERROR_INSUFFICIENT_BUFFER)
                {
                    name.EnsureCapacity((int)cchName);
                    referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
                    err = 0;
                    if (!LookupAccountSid(null, accountSidByes, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
                        err = Marshal.GetLastWin32Error();
                }
            }
            if (err == 0)
                return String.Format("{0}\\{1}", referencedDomainName.ToString(), name.ToString());
            else
                return "";
        }

        public static string GetRegValue(string hive, string path, string value)
        {
            // returns a single registry value under the specified path in the specified hive (HKLM/HKCU)
            var regKeyValue = "";
            if (hive == "HKCU")
            {
                var regKey = Registry.CurrentUser.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else if (hive == "HKU")
            {
                var regKey = Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
            else
            {
                var regKey = Registry.LocalMachine.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = String.Format("{0}", regKey.GetValue(value));
                }
                return regKeyValue;
            }
        }

        public static byte[] GetRegValueBytes(string hive, string path, string value)
        {
            // returns a byte array of single registry value under the specified path in the specified hive (HKLM/HKCU)
            byte[] regKeyValue = null;
            if (hive == "HKCU")
            {
                var regKey = Registry.CurrentUser.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
            else if (hive == "HKU")
            {
                var regKey = Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
            else
            {
                var regKey = Registry.LocalMachine.OpenSubKey(path);
                if (regKey != null)
                {
                    regKeyValue = (byte[])regKey.GetValue(value);
                }
                return regKeyValue;
            }
        }

        public static Dictionary<string, object> GetRegValues(string hive, string path)
        {
            // returns all registry values under the specified path in the specified hive (HKLM/HKCU)
            Dictionary<string, object> keyValuePairs = null;
            try
            {
                if (hive == "HKCU")
                {
                    using (var regKeyValues = Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                else if (hive == "HKU")
                {
                    using (var regKeyValues = Registry.Users.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                else
                {
                    using (var regKeyValues = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (regKeyValues != null)
                        {
                            var valueNames = regKeyValues.GetValueNames();
                            keyValuePairs = valueNames.ToDictionary(name => name, regKeyValues.GetValue);
                        }
                    }
                }
                return keyValuePairs;
            }
            catch
            {
                return null;
            }
        }

        public static string[] GetRegSubkeys(string hive, string path)
        {
            // returns an array of the subkeys names under the specified path in the specified hive (HKLM/HKCU/HKU)
            try
            {
                RegistryKey myKey = null;
                if (hive == "HKLM")
                {
                    myKey = Registry.LocalMachine.OpenSubKey(path);
                }
                else if (hive == "HKU")
                {
                    myKey = Registry.Users.OpenSubKey(path);
                }
                else
                {
                    myKey = Registry.CurrentUser.OpenSubKey(path);
                }


                if (myKey == null)
                    return new string[0];
                else
                    return myKey.GetSubKeyNames();
            }
            catch
            {
                return new string[0];
            }
        }

        public static string GetUNCPath(string originalPath)
        {
            // uses WNetGetConnection to map a drive letter to a possible UNC mount path
            // Pulled from @ambyte's gist at https://gist.github.com/ambyte/01664dc7ee576f69042c

            var sb = new StringBuilder(512);
            var size = sb.Capacity;

            // look for the {LETTER}: combination ...
            if (originalPath.Length > 2 && originalPath[1] == ':')
            {
                // don't use char.IsLetter here - as that can be misleading
                // the only valid drive letters are a-z && A-Z.
                var c = originalPath[0];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                {
                    var error = WNetGetConnection(originalPath.Substring(0, 2),
                        sb, ref size);
                    if (error == 0)
                    {
                        var dir = new DirectoryInfo(originalPath);

                        var path = Path.GetFullPath(originalPath)
                            .Substring(Path.GetPathRoot(originalPath).Length);
                        return Path.Combine(sb.ToString().TrimEnd(), path);
                    }
                }
            }

            return originalPath;
        }

        public static bool IsHighIntegrity()
        {
            // returns true if the current process is running with adminstrative privs in a high integrity context
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string[] GetLocalGroupMembers(string groupName, StringBuilder sb)
        {
            // returns the "DOMAIN\user" members for a specified local group name
            // adapted from boboes' code at https://stackoverflow.com/questions/33935825/pinvoke-netlocalgroupgetmembers-runs-into-fatalexecutionengineerror/33939889#33939889

            string computerName = null; // null for the local machine

            int EntriesRead;
            int TotalEntries;
            IntPtr Resume;
            IntPtr bufPtr;

            var retVal = NetworkAPI.NetLocalGroupGetMembers(computerName, groupName, 2, out bufPtr, -1, out EntriesRead, out TotalEntries, out Resume);

            if (retVal != 0)
            {
                if (retVal == NetworkAPI.ERROR_ACCESS_DENIED) { sb.AppendLine("Access denied"); return null; }
                if (retVal == NetworkAPI.ERROR_MORE_DATA) { sb.AppendLine("ERROR_MORE_DATA"); return null; }
                if (retVal == NetworkAPI.ERROR_NO_SUCH_ALIAS) { sb.AppendLine("Group not found"); return null; }
                if (retVal == NetworkAPI.NERR_InvalidComputer) { sb.AppendLine("Invalid computer name"); return null; }
                if (retVal == NetworkAPI.NERR_GroupNotFound) { sb.AppendLine("Group not found"); return null; }
                if (retVal == NetworkAPI.SERVER_UNAVAILABLE) { sb.AppendLine("Server unavailable"); return null; }
                sb.AppendLine("Unexpected NET_API_STATUS: " + retVal.ToString());
                return null;
            }

            if (EntriesRead > 0)
            {
                var names = new string[EntriesRead];
                var Members = new NetworkAPI.LOCALGROUP_MEMBERS_INFO_2[EntriesRead];
                var iter = bufPtr;

                for (var i = 0; i < EntriesRead; i++)
                {
                    Members[i] = (NetworkAPI.LOCALGROUP_MEMBERS_INFO_2)Marshal.PtrToStructure(iter, typeof(NetworkAPI.LOCALGROUP_MEMBERS_INFO_2));

                    //x64 safe
                    iter = new IntPtr(iter.ToInt64() + Marshal.SizeOf(typeof(NetworkAPI.LOCALGROUP_MEMBERS_INFO_2)));

                    names[i] = Members[i].lgrmi2_domainandname;
                }
                NetworkAPI.NetApiBufferFree(bufPtr);

                return names;
            }
            else
            {
                return null;
            }
        }

        public static string[] GetTokenGroupSIDs()
        {
            // Returns all SIDs that the current user is a part of, whether they are disabled or not.
            // slightly adapted from https://stackoverflow.com/questions/2146153/how-to-get-the-logon-sid-in-c-sharp/2146418#2146418

            var tokenInfLength = 0;

            // first call gets length of TokenInformation
            var result = GetTokenInformation(WindowsIdentity.GetCurrent().Token, TokenInformation.TokenGroups, IntPtr.Zero, tokenInfLength, out tokenInfLength);
            var tokenInformation = Marshal.AllocHGlobal(tokenInfLength);
            result = GetTokenInformation(WindowsIdentity.GetCurrent().Token, TokenInformation.TokenGroups, tokenInformation, tokenInfLength, out tokenInfLength);

            if (!result)
            {
                Marshal.FreeHGlobal(tokenInformation);
                return null;
            }

            var groups = (TOKEN_GROUPS)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_GROUPS));
            var userSIDS = new string[groups.GroupCount];
            var sidAndAttrSize = Marshal.SizeOf(new SID_AND_ATTRIBUTES());
            for (var i = 0; i < groups.GroupCount; i++)
            {
                var sidAndAttributes = (SID_AND_ATTRIBUTES)Marshal.PtrToStructure(
                    new IntPtr(tokenInformation.ToInt64() + i * sidAndAttrSize + IntPtr.Size), typeof(SID_AND_ATTRIBUTES));

                var pstr = IntPtr.Zero;
                ConvertSidToStringSid(sidAndAttributes.Sid, out pstr);
                userSIDS[i] = Marshal.PtrToStringAuto(pstr);
                LocalFree(pstr);
            }

            Marshal.FreeHGlobal(tokenInformation);
            return userSIDS;
        }

        public static bool GetSystem()
        {
            // helper to elevate to SYSTEM for Kerberos ticket enumeration via token impersonation

            if (IsHighIntegrity())
            {
                var hToken = IntPtr.Zero;

                // Open winlogon's token with TOKEN_DUPLICATE accesss so ca can make a copy of the token with DuplicateToken
                var processes = Process.GetProcessesByName("winlogon");
                var handle = processes[0].Handle;

                // TOKEN_DUPLICATE = 0x0002
                var success = OpenProcessToken(handle, 0x0002, out hToken);
                if (!success)
                {
                    //Console.WriteLine("OpenProcessToken failed!");
                    return false;
                }

                // make a copy of the NT AUTHORITY\SYSTEM token from winlogon
                // 2 == SecurityImpersonation
                var hDupToken = IntPtr.Zero;
                success = DuplicateToken(hToken, 2, ref hDupToken);
                if (!success)
                {
                    //Console.WriteLine("DuplicateToken failed!");
                    return false;
                }

                success = ImpersonateLoggedOnUser(hDupToken);
                if (!success)
                {
                    //Console.WriteLine("ImpersonateLoggedOnUser failed!");
                    return false;
                }

                // clean up the handles we created
                CloseHandle(hToken);
                CloseHandle(hDupToken);

                var name = WindowsIdentity.GetCurrent().Name;
                if (name != "NT AUTHORITY\\SYSTEM")
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static IntPtr LsaRegisterLogonProcessHelper()
        {
            // helper that establishes a connection to the LSA server and verifies that the caller is a logon application
            //  used for Kerberos ticket enumeration

            var logonProcessName = "User32LogonProcesss";
            LSA_STRING_IN LSAString;
            var lsaHandle = IntPtr.Zero;
            UInt64 securityMode = 0;

            LSAString.Length = (ushort)logonProcessName.Length;
            LSAString.MaximumLength = (ushort)(logonProcessName.Length + 1);
            LSAString.Buffer = logonProcessName;

            var ret = LsaRegisterLogonProcess(LSAString, out lsaHandle, out securityMode);

            return lsaHandle;
        }

        public static bool IsLocalAdmin()
        {
            // checks if the "S-1-5-32-544" in the current token groups set, meaning the user is a local administrator
            var SIDs = GetTokenGroupSIDs();

            foreach (var SID in SIDs)
            {
                if (SID == "S-1-5-32-544")
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsVirtualMachine()
        {
            // returns true if the system is likely a virtual machine
            // Adapted from RobSiklos' code from https://stackoverflow.com/questions/498371/how-to-detect-if-my-application-is-running-in-a-virtual-machine/11145280#11145280

            using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
            {
                using (var items = searcher.Get())
                {
                    foreach (var item in items)
                    {
                        var manufacturer = item["Manufacturer"].ToString().ToLower();
                        if ((manufacturer == "microsoft corporation" && item["Model"].ToString().ToUpperInvariant().Contains("VIRTUAL"))
                            || manufacturer.Contains("vmware")
                            || item["Model"].ToString() == "VirtualBox")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool CheckAccess(string path, FileSystemRights accessRight)
        {
            // checks if the current user has the specified AccessRight to the specified file or folder
            // adapted from https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder/21996345#21996345

            if (String.IsNullOrEmpty(path)) return false;

            try
            {
                var rules = Directory.GetAccessControl(path).GetAccessRules(true, true, typeof(SecurityIdentifier));
                var identity = WindowsIdentity.GetCurrent();

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (identity.Groups.Contains(rule.IdentityReference))
                    {
                        if ((accessRight & rule.FileSystemRights) == accessRight)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                                return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        public static bool CheckModifiableAccess(string path)
        {
            // checks if the current user has rights to modify the given file/directory
            // adapted from https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder/21996345#21996345

            if (String.IsNullOrEmpty(path)) return false;
            // TODO: check if file exists, check file's parent folder

            FileSystemRights[] ModifyRights =
            {
                FileSystemRights.ChangePermissions,
                FileSystemRights.FullControl,
                FileSystemRights.Modify,
                FileSystemRights.TakeOwnership,
                FileSystemRights.Write,
                FileSystemRights.WriteData,
                FileSystemRights.CreateDirectories,
                FileSystemRights.CreateFiles
            };

            var paths = new ArrayList();
            paths.Add(path);

            try
            {
                var attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    var parentFolder = System.IO.Path.GetDirectoryName(path);
                    paths.Add(parentFolder);
                }
            }
            catch
            {
                return false;
            }


            try
            {
                foreach (string candidatePath in paths)
                {
                    var rules = Directory.GetAccessControl(candidatePath).GetAccessRules(true, true, typeof(SecurityIdentifier));
                    var identity = WindowsIdentity.GetCurrent();

                    foreach (FileSystemAccessRule rule in rules)
                    {
                        if (identity.Groups.Contains(rule.IdentityReference))
                        {
                            foreach (var AccessRight in ModifyRights)
                            {
                                if ((AccessRight & rule.FileSystemRights) == AccessRight)
                                {
                                    if (rule.AccessControlType == AccessControlType.Allow)
                                        return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static List<string> FindFiles(string path, string patterns)
        {
            // finds files matching one or more patterns under a given path, recursive
            // adapted from http://csharphelper.com/blog/2015/06/find-files-that-match-multiple-patterns-in-c/
            //      pattern: "*pass*;*.png;"

            var files = new List<string>();
            try
            {

                var filesUnfiltered = GetFiles(path).ToList();


                // search every pattern in this directory's files
                foreach (var pattern in patterns.Split(';'))
                {
                    files.AddRange(filesUnfiltered.Where( f => f.Contains(pattern.Trim('*'))));

                    //files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                }

                //// go recurse in all sub-directories
                //foreach (var directory in Directory.GetDirectories(path))
                //    files.AddRange(FindFiles(directory, patterns));
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }

            return files;
        }

        // FROM: https://stackoverflow.com/a/929418
        private static IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (var subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception)
                {
                    // Eat it
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception)
                {
                    // Eat it
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }
        
        public static IEnumerable<string> Split(string text, int partLength, StringBuilder sb)
        {
            if (text == null) { sb.AppendLine("[ERROR] Split() - singleLineString"); }
            if (partLength < 1) { sb.AppendLine("[ERROR] Split() - 'columns' must be greater than 0."); }

            var partCount = Math.Ceiling((double)text.Length / partLength);
            if (partCount < 2)
            {
                yield return text;
            }

            for (var i = 0; i < partCount; i++)
            {
                var index = i * partLength;
                var lengthLeft = Math.Min(partLength, text.Length - index);
                var line = text.Substring(index, lengthLeft);
                yield return line;
            }
        }
    }
}
