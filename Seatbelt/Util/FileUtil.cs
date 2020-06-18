using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Seatbelt.Util
{
    class FileUtil
    {
        public static bool IsDotNetAssembly(string path)
        {
            try
            {
                var myAssemblyName = AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch (BadImageFormatException exception)
            {
                if (Regex.IsMatch(exception.Message, ".*This assembly is built by a runtime newer than the currently loaded runtime and cannot be loaded.*", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }

    public static class IniFileHelper
    {
        // https://code.msdn.microsoft.com/windowsdesktop/Reading-and-Writing-Values-85084b6a
        // MIT license

        public static int capacity = 512;


        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder value, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string? section, string? key, string defaultValue, [In, Out] char[] value, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileSection(string section, IntPtr keyValue, int size, string filePath);

        public static string[] ReadSections(string filePath)
        {
            // first line will not recognize if ini file is saved in UTF-8 with BOM
            while (true)
            {
                var chars = new char[capacity];
                var size = GetPrivateProfileString(null, null, "", chars, capacity, filePath);

                if (size == 0)
                    return new string[] { };

                if (size < capacity - 2)
                {
                    var result = new string(chars, 0, size);
                    var sections = result.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    return sections;
                }

                capacity *= 2;
            }
        }


        public static string[] ReadKeyValuePairs(string section, string filePath)
        {
            while (true)
            {
                var returnedString = Marshal.AllocCoTaskMem(capacity * sizeof(char));
                var size = GetPrivateProfileSection(section, returnedString, capacity, filePath);

                if (size == 0)
                {
                    Marshal.FreeCoTaskMem(returnedString);
                    return new string[] {};
                }

                if (size < capacity - 2)
                {
                    var result = Marshal.PtrToStringAuto(returnedString, size - 1);
                    Marshal.FreeCoTaskMem(returnedString);
                    var keyValuePairs = result.Split('\0');
                    return keyValuePairs;
                }

                Marshal.FreeCoTaskMem(returnedString);
                capacity *= 2;
            }
        }
    }

}
