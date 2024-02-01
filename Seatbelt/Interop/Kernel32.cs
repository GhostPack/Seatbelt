using System;
using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    internal class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalAlloc(uint uFlags, uint uBytes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA
           lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
        ProcessAccess processAccess,
        bool bInheritHandle,
        int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
         [MarshalAs(UnmanagedType.LPTStr)] string filename,
         [MarshalAs(UnmanagedType.U4)] System.IO.FileAccess access,
         [MarshalAs(UnmanagedType.U4)] System.IO.FileShare share,
         IntPtr securityAttributes,
         [MarshalAs(UnmanagedType.U4)] System.IO.FileMode creationDisposition,
         [MarshalAs(UnmanagedType.U4)] System.IO.FileAttributes flagsAndAttributes,
         IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetNamedPipeServerProcessId(IntPtr hPipe, out int ProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetNamedPipeServerSessionId(IntPtr hPipe, out int ProcessId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();


        [Flags]
        public enum ProcessAccess
        {
            AllAccess = 0x001FFFFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }
    }
}
