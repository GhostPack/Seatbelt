using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    public class Iphlpapi
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref uint dwOutBufLen,
            bool sort,
            int ipVersion,
            TCP_TABLE_CLASS tblClass,
            int reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedUdpTable(
            IntPtr pUdpTable,
            ref uint dwOutBufLen,
            bool sort,
            int ipVersion,
            UDP_TABLE_CLASS tblClass,
            int reserved);

        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int GetIpNetTable(IntPtr pIpNetTable, [MarshalAs(UnmanagedType.U4)]ref int pdwSize, bool bOrder);

        [DllImport("IpHlpApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int FreeMibTable(IntPtr plpNetTable);

        public enum TCP_TABLE_CLASS : int
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        public enum MIB_TCP_STATE
        {
            CLOSED = 1,
            LISTEN = 2,
            SYN_SENT = 3,
            SYN_RCVD = 4,
            ESTAB = 5,
            FIN_WAIT1 = 6,
            FIN_WAIT2 = 7,
            CLOSE_WAIT = 8,
            CLOSING = 9,
            LAST_ACK = 10,
            TIME_WAIT = 11,
            DELETE_TCB = 12
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPTABLE_OWNER_MODULE
        {
            public uint NumEntries;
            private readonly MIB_TCPROW_OWNER_MODULE Table;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_MODULE
        {
            public readonly MIB_TCP_STATE State;
            public readonly uint LocalAddr;
            private readonly byte LocalPort1;
            private readonly byte LocalPort2;
            private readonly byte LocalPort3;
            private readonly byte LocalPort4;
            public readonly uint RemoteAddr;
            private readonly byte RemotePort1;
            private readonly byte RemotePort2;
            private readonly byte RemotePort3;
            private readonly byte RemotePort4;
            public readonly uint OwningPid;
            public readonly ulong CreateTimestamp;
            public readonly ulong OwningModuleInfo0;
            public readonly ulong OwningModuleInfo1;
            public readonly ulong OwningModuleInfo2;
            public readonly ulong OwningModuleInfo3;
            public readonly ulong OwningModuleInfo4;
            public readonly ulong OwningModuleInfo5;
            public readonly ulong OwningModuleInfo6;
            public readonly ulong OwningModuleInfo7;
            public readonly ulong OwningModuleInfo8;
            public readonly ulong OwningModuleInfo9;
            public readonly ulong OwningModuleInfo10;
            public readonly ulong OwningModuleInfo11;
            public readonly ulong OwningModuleInfo12;
            public readonly ulong OwningModuleInfo13;
            public readonly ulong OwningModuleInfo14;
            public readonly ulong OwningModuleInfo15;


            public ushort LocalPort => BitConverter.ToUInt16(new byte[2] { LocalPort2, LocalPort1 }, 0);

            public IPAddress LocalAddress => new IPAddress(LocalAddr);

            public IPAddress RemoteAddress => new IPAddress(RemoteAddr);

            public ushort RemotePort => BitConverter.ToUInt16(new byte[2] { RemotePort2, RemotePort1 }, 0);
        }

        #region UDP Interop
        public enum UDP_TABLE_CLASS : int
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPTABLE_OWNER_MODULE
        {
            public uint NumEntries;
            private readonly MIB_UDPROW_OWNER_MODULE Table;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPROW_OWNER_MODULE
        {
            public readonly uint LocalAddr;
            private readonly byte LocalPort1;
            private readonly byte LocalPort2;
            private readonly byte LocalPort3;
            private readonly byte LocalPort4;
            public readonly uint OwningPid;
            public readonly ulong CreateTimestamp;
            public readonly uint SpecificPortBind_Flags;
            // public readonly UInt32 Flags;
            public readonly ulong OwningModuleInfo0;
            public readonly ulong OwningModuleInfo1;
            public readonly ulong OwningModuleInfo2;
            public readonly ulong OwningModuleInfo3;
            public readonly ulong OwningModuleInfo4;
            public readonly ulong OwningModuleInfo5;
            public readonly ulong OwningModuleInfo6;
            public readonly ulong OwningModuleInfo7;
            public readonly ulong OwningModuleInfo8;
            public readonly ulong OwningModuleInfo9;
            public readonly ulong OwningModuleInfo10;
            public readonly ulong OwningModuleInfo11;
            public readonly ulong OwningModuleInfo12;
            public readonly ulong OwningModuleInfo13;
            public readonly ulong OwningModuleInfo14;
            public readonly ulong OwningModuleInfo15;

            public ushort LocalPort => BitConverter.ToUInt16(new byte[2] { LocalPort2, LocalPort1 }, 0);

            public IPAddress LocalAddress => new IPAddress(LocalAddr);
        }

        public enum ArpEntryType
        {
            Other = 1,
            Invalid = 2,
            Dynamic = 3,
            Static = 4,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_IPNETROW
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwIndex;
            [MarshalAs(UnmanagedType.U4)]
            public int dwPhysAddrLen;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac0;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac1;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac2;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac3;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac4;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac5;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac6;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac7;
            [MarshalAs(UnmanagedType.U4)]
            public int dwAddr;
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
        }

        #endregion

    }
}
