using System;
using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    internal class Wtsapi32
    {
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] string pServerName);

        [DllImport("wtsapi32.dll")]
        public static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSEnumerateSessionsEx(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] ref int pLevel,
            [MarshalAs(UnmanagedType.U4)] int Filter,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref int pCount);

        [DllImport("wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("Wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            uint sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out uint pBytesReturned
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO
        {
            public int SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO_1
        {
            public uint ExecEnvId;

            public WTS_CONNECTSTATE_CLASS State;

            public uint SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pSessionName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pHostName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pUserName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pDomainName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pFarmName;
        }


        public enum ADDRESS_FAMILY
        {
            AF_UNSPEC,
            AF_INET = 2,
            AF_IPX = 6,
            AF_NETBIOS = 17,
            AF_INET6 = 23,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_ADDRESS
        {
            public ADDRESS_FAMILY AddressFamily;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        public enum WTS_INFO_CLASS
        {
            WTSInitialProgram = 0,
            WTSApplicationName = 1,
            WTSWorkingDirectory = 2,
            WTSOEMId = 3,
            WTSSessionId = 4,
            WTSUserName = 5,
            WTSWinStationName = 6,
            WTSDomainName = 7,
            WTSConnectState = 8,
            WTSClientBuildNumber = 9,
            WTSClientName = 10,
            WTSClientDirectory = 11,
            WTSClientProductId = 12,
            WTSClientHardwareId = 13,
            WTSClientAddress = 14,
            WTSClientDisplay = 15,
            WTSClientProtocolType = 16,
            WTSIdleTime = 17,
            WTSLogonTime = 18,
            WTSIncomingBytes = 19,
            WTSOutgoingBytes = 20,
            WTSIncomingFrames = 21,
            WTSOutgoingFrames = 22,
            WTSClientInfo = 23,
            WTSSessionInfo = 24,
            WTSSessionInfoEx = 25,
            WTSConfigInfo = 26,
            WTSValidationInfo = 27,
            WTSSessionAddressV4 = 28,
            WTSIsRemoteSession = 29
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_DISPLAY
        {
            public int HorizontalResolution;
            public int VerticalResolution;
            public int ColorDepth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct WTSINFO
        {
            public WTS_CONNECTSTATE_CLASS State;
            public int SessionId;
            public int IncomingBytes;
            public int OutgoingBytes;
            public int IncomingFrames;
            public int OutgoingFrames;
            public int IncomingCompressedBytes;
            public int OutgoingCompressedBytes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string WinStationName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
            public string Domain;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
            public string UserName;
            [MarshalAs(UnmanagedType.I8)]
            public long ConnectTime;
            [MarshalAs(UnmanagedType.I8)]
            public long DisconnectTime;
            [MarshalAs(UnmanagedType.I8)]
            public long LastInputTime;
            [MarshalAs(UnmanagedType.I8)]
            public long LogonTime;
            [MarshalAs(UnmanagedType.I8)]
            public long CurrentTime;
        }
    }
}

