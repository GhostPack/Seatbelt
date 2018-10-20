using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Seatbelt.WindowsInterop
{

    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_GROUPS
    {
        public int GroupCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public SID_AND_ATTRIBUTES[] Groups;
    };

    public struct TOKEN_PRIVILEGES
    {
        public UInt32 PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public UInt32 Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_SESSION_INFO
    {
        public Int32 SessionID;

        [MarshalAs(UnmanagedType.LPStr)]
        public String pWinStationName;

        public WTS_CONNECTSTATE_CLASS State;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_SESSION_INFO_1
    {
        public Int32 ExecEnvId;

        public WTS_CONNECTSTATE_CLASS State;

        public Int32 SessionID;

        [MarshalAs(UnmanagedType.LPStr)]
        public String pSessionName;

        [MarshalAs(UnmanagedType.LPStr)]
        public String pHostName;

        [MarshalAs(UnmanagedType.LPStr)]
        public String pUserName;

        [MarshalAs(UnmanagedType.LPStr)]
        public String pDomainName;

        [MarshalAs(UnmanagedType.LPStr)]
        public String pFarmName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_CLIENT_ADDRESS
    {
        public uint AddressFamily;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Address;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SC_SERVICE_TAG_QUERY
    {
        public uint ProcessId;
        public uint ServiceTag;
        public uint Unknown;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_MODULE
    {
        public uint NumEntries;
        MIB_TCPROW_OWNER_MODULE Table;
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
        public readonly UInt64 CreateTimestamp;
        public readonly UInt64 OwningModuleInfo0;
        public readonly UInt64 OwningModuleInfo1;
        public readonly UInt64 OwningModuleInfo2;
        public readonly UInt64 OwningModuleInfo3;
        public readonly UInt64 OwningModuleInfo4;
        public readonly UInt64 OwningModuleInfo5;
        public readonly UInt64 OwningModuleInfo6;
        public readonly UInt64 OwningModuleInfo7;
        public readonly UInt64 OwningModuleInfo8;
        public readonly UInt64 OwningModuleInfo9;
        public readonly UInt64 OwningModuleInfo10;
        public readonly UInt64 OwningModuleInfo11;
        public readonly UInt64 OwningModuleInfo12;
        public readonly UInt64 OwningModuleInfo13;
        public readonly UInt64 OwningModuleInfo14;
        public readonly UInt64 OwningModuleInfo15;


        public ushort LocalPort
        {
            get
            {
                return BitConverter.ToUInt16(
                    new byte[2] { LocalPort2, LocalPort1 }, 0);
            }
        }

        public IPAddress LocalAddress
        {
            get { return new IPAddress(LocalAddr); }
        }

        public IPAddress RemoteAddress
        {
            get { return new IPAddress(RemoteAddr); }
        }

        public ushort RemotePort
        {
            get
            {
                return BitConverter.ToUInt16(
                    new byte[2] { RemotePort2, RemotePort1 }, 0);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPTABLE_OWNER_MODULE
    {
        public uint NumEntries;
        MIB_UDPROW_OWNER_MODULE Table;
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
        public readonly UInt64 CreateTimestamp;
        public readonly UInt32 SpecificPortBind_Flags;
        // public readonly UInt32 Flags;
        public readonly UInt64 OwningModuleInfo0;
        public readonly UInt64 OwningModuleInfo1;
        public readonly UInt64 OwningModuleInfo2;
        public readonly UInt64 OwningModuleInfo3;
        public readonly UInt64 OwningModuleInfo4;
        public readonly UInt64 OwningModuleInfo5;
        public readonly UInt64 OwningModuleInfo6;
        public readonly UInt64 OwningModuleInfo7;
        public readonly UInt64 OwningModuleInfo8;
        public readonly UInt64 OwningModuleInfo9;
        public readonly UInt64 OwningModuleInfo10;
        public readonly UInt64 OwningModuleInfo11;
        public readonly UInt64 OwningModuleInfo12;
        public readonly UInt64 OwningModuleInfo13;
        public readonly UInt64 OwningModuleInfo14;
        public readonly UInt64 OwningModuleInfo15;

        public ushort LocalPort
        {
            get
            {
                return BitConverter.ToUInt16(
                    new byte[2] { LocalPort2, LocalPort1 }, 0);
            }
        }

        public IPAddress LocalAddress
        {
            get { return new IPAddress(LocalAddr); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint remoteAddr;
        public byte remotePort1;
        public byte remotePort2;
        public byte remotePort3;
        public byte remotePort4;
        public int owningPid;

        public ushort LocalPort
        {
            get
            {
                return BitConverter.ToUInt16(
                    new byte[2] { localPort2, localPort1 }, 0);
            }
        }

        public IPAddress LocalAddress
        {
            get { return new IPAddress(localAddr); }
        }

        public IPAddress RemoteAddress
        {
            get { return new IPAddress(remoteAddr); }
        }

        public ushort RemotePort
        {
            get
            {
                return BitConverter.ToUInt16(
                    new byte[2] { remotePort2, remotePort1 }, 0);
            }
        }

        public MIB_TCP_STATE State
        {
            get { return (MIB_TCP_STATE)state; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint localAddr;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public int owningPid;

        public ushort LocalPort
        {
            get
            {
                return BitConverter.ToUInt16(
                    new byte[2] { localPort2, localPort1 }, 0);
            }
        }

        public IPAddress LocalAddress
        {
            get { return new IPAddress(localAddr); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        MIB_TCPROW_OWNER_PID table;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        MIB_TCPROW_OWNER_PID table;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LSA_STRING_IN
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        public string Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LSA_STRING_OUT
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING : IDisposable
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr buffer;

        public UNICODE_STRING(string s)
        {
            Length = (ushort)(s.Length * 2);
            MaximumLength = (ushort)(Length + 2);
            buffer = Marshal.StringToHGlobalUni(s);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;
        }

        public override string ToString()
        {
            return Marshal.PtrToStringUni(buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_HANDLE
    {
        public IntPtr LowPart;
        public IntPtr HighPart;
        public SECURITY_HANDLE(int dummy)
        {
            LowPart = HighPart = IntPtr.Zero;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_TICKET_CACHE_INFO
    {
        public LSA_STRING_OUT ServerName;
        public LSA_STRING_OUT RealmName;
        public Int64 StartTime;
        public Int64 EndTime;
        public Int64 RenewTime;
        public Int32 EncryptionType;
        public UInt32 TicketFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_TICKET_CACHE_INFO_EX
    {
        public LSA_STRING_OUT ClientName;
        public LSA_STRING_OUT ClientRealm;
        public LSA_STRING_OUT ServerName;
        public LSA_STRING_OUT ServerRealm;
        public Int64 StartTime;
        public Int64 EndTime;
        public Int64 RenewTime;
        public Int32 EncryptionType;
        public UInt32 TicketFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_QUERY_TKT_CACHE_RESPONSE
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public int CountOfTickets;
        // public KERB_TICKET_CACHE_INFO[] Tickets;
        public IntPtr Tickets;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_QUERY_TKT_CACHE_EX_RESPONSE
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public int CountOfTickets;
        // public KERB_TICKET_CACHE_INFO[] Tickets;
        public IntPtr Tickets;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_QUERY_TKT_CACHE_REQUEST
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public LUID LogonId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_RETRIEVE_TKT_REQUEST
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public LUID LogonId;
        public LSA_STRING_IN TargetName;
        public UInt64 TicketFlags;
        public KERB_CACHE_OPTIONS CacheOptions;
        public Int64 EncryptionType;
        public SECURITY_HANDLE CredentialsHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_RETRIEVE_TKT_REQUEST_UNI
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public LUID LogonId;
        public UNICODE_STRING TargetName;
        public UInt64 TicketFlags;
        public KERB_CACHE_OPTIONS CacheOptions;
        public Int64 EncryptionType;
        public SECURITY_HANDLE CredentialsHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_CRYPTO_KEY
    {
        public Int32 KeyType;
        public Int32 Length;
        public IntPtr Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_EXTERNAL_NAME
    {
        public Int16 NameType;
        public UInt16 NameCount;
        public LSA_STRING_OUT Names;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_EXTERNAL_TICKET
    {
        public IntPtr ServiceName;
        public IntPtr TargetName;
        public IntPtr ClientName;
        public LSA_STRING_OUT DomainName;
        public LSA_STRING_OUT TargetDomainName;
        public LSA_STRING_OUT AltTargetDomainName;
        public KERB_CRYPTO_KEY SessionKey;
        public UInt32 TicketFlags;
        public UInt32 Flags;
        public Int64 KeyExpirationTime;
        public Int64 StartTime;
        public Int64 EndTime;
        public Int64 RenewUntil;
        public Int64 TimeSkew;
        public Int32 EncodedTicketSize;
        public IntPtr EncodedTicket;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_RETRIEVE_TKT_RESPONSE
    {
        public KERB_EXTERNAL_TICKET Ticket;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_LOGON_SESSION_DATA
    {
        public UInt32 Size;
        public LUID LoginID;
        public LSA_STRING_OUT Username;
        public LSA_STRING_OUT LoginDomain;
        public LSA_STRING_OUT AuthenticationPackage;
        public UInt32 LogonType;
        public UInt32 Session;
        public IntPtr PSiD;
        public UInt64 LoginTime;
        public LSA_STRING_OUT LogonServer;
        public LSA_STRING_OUT DnsDomainName;
        public LSA_STRING_OUT Upn;
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

}
