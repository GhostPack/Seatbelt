using System;
using System.Runtime.InteropServices;

namespace Seatbelt.WindowsInterop
{
    public static class NetworkAPI
    {
        // from boboes' code at https://stackoverflow.com/questions/33935825/pinvoke-netlocalgroupgetmembers-runs-into-fatalexecutionengineerror/33939889#33939889

        [DllImport("Netapi32.dll")]
        public static extern uint NetLocalGroupGetMembers([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string localgroupname, int level, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries, out IntPtr resumehandle);

        [DllImport("Netapi32.dll")]
        public static extern int NetApiBufferFree(IntPtr Buffer);

        // LOCALGROUP_MEMBERS_INFO_2 - Structure for holding members details
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LOCALGROUP_MEMBERS_INFO_2
        {
            public IntPtr lgrmi2_sid;
            public int lgrmi2_sidusage;
            public string lgrmi2_domainandname;
        }

        // documented in MSDN
        public const uint ERROR_ACCESS_DENIED = 0x0000005;
        public const uint ERROR_MORE_DATA = 0x00000EA;
        public const uint ERROR_NO_SUCH_ALIAS = 0x0000560;
        public const uint NERR_InvalidComputer = 0x000092F;

        // found by testing
        public const uint NERR_GroupNotFound = 0x00008AC;
        public const uint SERVER_UNAVAILABLE = 0x0006BA;
    }
}