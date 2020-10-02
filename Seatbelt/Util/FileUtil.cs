using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Seatbelt.Util
{
    class FileUtil
    {
        public static bool IsDotNetAssembly(string fileName)
        {
            try
            {
                // from https://stackoverflow.com/a/15608028
                using (Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if (fileStream.Length < 64)
                    {
                        return false;
                    }

                    //PE Header starts @ 0x3C (60). Its a 4 byte header.
                    fileStream.Position = 0x3C;
                    uint peHeaderPointer = binaryReader.ReadUInt32();
                    if (peHeaderPointer == 0)
                    {
                        peHeaderPointer = 0x80;
                    }

                    // Ensure there is at least enough room for the following structures:
                    //     24 byte PE Signature & Header
                    //     28 byte Standard Fields         (24 bytes for PE32+)
                    //     68 byte NT Fields               (88 bytes for PE32+)
                    // >= 128 byte Data Dictionary Table
                    if (peHeaderPointer > fileStream.Length - 256)
                    {
                        return false;
                    }

                    // Check the PE signature.  Should equal 'PE\0\0'.
                    fileStream.Position = peHeaderPointer;
                    uint peHeaderSignature = binaryReader.ReadUInt32();
                    if (peHeaderSignature != 0x00004550)
                    {
                        return false;
                    }

                    // skip over the PEHeader fields
                    fileStream.Position += 20;

                    const ushort PE32 = 0x10b;
                    const ushort PE32Plus = 0x20b;

                    // Read PE magic number from Standard Fields to determine format.
                    var peFormat = binaryReader.ReadUInt16();
                    if (peFormat != PE32 && peFormat != PE32Plus)
                    {
                        return false;
                    }

                    // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
                    // When this is non-zero then the file contains CLI data otherwise not.
                    ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
                    fileStream.Position = dataDictionaryStart;

                    uint cliHeaderRva = binaryReader.ReadUInt32();
                    if (cliHeaderRva == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch {
                return false;
            }
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
