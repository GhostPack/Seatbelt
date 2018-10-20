using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Seatbelt.WindowsInterop
{
    public static class NativeMethods
    {
        public const int MAXLEN_PHYSADDR = 8;
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;

        // PInvoke structures/contants
        public const uint SE_GROUP_LOGON_ID = 0xC0000000; // from winnt.h
        public const int TokenGroups = 2; // from TokenInformation


        // PInvoke signature definitions
        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WNetGetConnection(
            [MarshalAs(UnmanagedType.LPTStr)] string localName,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName,
            ref int length);

        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool GetTokenInformation(
            IntPtr tokenHandle,
            TokenInformation tokenInformation,
            IntPtr ptrTokenInformation,
            int tokenInformationLength,
            out int returnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeName(
            string lpSystemName,
            IntPtr lpLuid,
            System.Text.StringBuilder lpName,
            ref int cchName);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        internal static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

        [DllImport("wtsapi32.dll")]
        internal static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern Int32 WTSEnumerateSessions(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] Int32 reserved,
            [MarshalAs(UnmanagedType.U4)] Int32 version,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern Int32 WTSEnumerateSessionsEx(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pLevel,
            [MarshalAs(UnmanagedType.U4)] Int32 Filter,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("Wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            uint sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out uint pBytesReturned
        );

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref uint dwOutBufLen,
            bool sort,
            int ipVersion,
            TCP_TABLE_CLASS tblClass,
            int reserved);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint I_QueryTagInformation(
            IntPtr Unknown,
            ScServiceTagQueryType Type,
            ref SC_SERVICE_TAG_QUERY Query
            );

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedUdpTable(
            IntPtr pUdpTable,
            ref uint dwOutBufLen,
            bool sort,
            int ipVersion,
            UDP_TABLE_CLASS tblClass,
            int reserved);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern int LsaConnectUntrusted([Out] out IntPtr LsaHandle);

        [DllImport("secur32.dll", SetLastError = true)]
        public static extern int LsaRegisterLogonProcess(LSA_STRING_IN LogonProcessName, out IntPtr LsaHandle, out ulong SecurityMode);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern int LsaDeregisterLogonProcess([In] IntPtr LsaHandle);

        [DllImport("secur32.dll", SetLastError = false)]
        public static extern int LsaLookupAuthenticationPackage([In] IntPtr LsaHandle, [In] ref LSA_STRING_IN PackageName, [Out] out int AuthenticationPackage);

        [DllImport("secur32.dll", SetLastError = false)]
        public static extern int LsaCallAuthenticationPackage(IntPtr LsaHandle, int AuthenticationPackage, ref KERB_QUERY_TKT_CACHE_REQUEST ProtocolSubmitBuffer, int SubmitBufferLength, out IntPtr ProtocolReturnBuffer, out int ReturnBufferLength, out int ProtocolStatus);

        [DllImport("secur32.dll", EntryPoint = "LsaCallAuthenticationPackage", SetLastError = false)]
        public static extern int LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(IntPtr LsaHandle, int AuthenticationPackage, ref KERB_RETRIEVE_TKT_REQUEST ProtocolSubmitBuffer, int SubmitBufferLength, out IntPtr ProtocolReturnBuffer, out int ReturnBufferLength, out int ProtocolStatus);

        [DllImport("secur32.dll", EntryPoint = "LsaCallAuthenticationPackage", SetLastError = false)]
        public static extern int LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT_UNI(IntPtr LsaHandle, int AuthenticationPackage, ref KERB_RETRIEVE_TKT_REQUEST_UNI ProtocolSubmitBuffer, int SubmitBufferLength, out IntPtr ProtocolReturnBuffer, out int ReturnBufferLength, out int ProtocolStatus);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern uint LsaFreeReturnBuffer(IntPtr buffer);

        [DllImport("Secur32.dll", SetLastError = false)]
        internal static extern uint LsaEnumerateLogonSessions(out UInt64 LogonSessionCount, out IntPtr LogonSessionList);

        [DllImport("Secur32.dll", SetLastError = false)]
        internal static extern uint LsaGetLogonSessionData(IntPtr luid, out IntPtr ppLogonSessionData);

        // for GetSystem()
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll")]
        public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern IntPtr LocalAlloc(uint uFlags, uint uBytes);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int GetIpNetTable(IntPtr pIpNetTable, [MarshalAs(UnmanagedType.U4)]ref int pdwSize, bool bOrder);

        [DllImport("IpHlpApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int FreeMibTable(IntPtr plpNetTable);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool LookupAccountSid(
          string lpSystemName,
          [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
          StringBuilder lpName,
          ref uint cchName,
          StringBuilder ReferencedDomainName,
          ref uint cchReferencedDomainName,
          out SID_NAME_USE peUse);

    }
}
