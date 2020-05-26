using System;
using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    public class Secur32
    {
        #region Function Defintions
        [DllImport("secur32.dll", SetLastError = false)]
        public static extern uint LsaFreeReturnBuffer(
            IntPtr buffer
        );

        [DllImport("Secur32.dll", SetLastError = false)]
        public static extern uint LsaEnumerateLogonSessions(
            out UInt64 LogonSessionCount,
            out IntPtr LogonSessionList
        );

        [DllImport("Secur32.dll", SetLastError = false)]
        public static extern uint LsaGetLogonSessionData(
            IntPtr luid,
            out IntPtr ppLogonSessionData
        );

        [DllImport("Secur32.dll", SetLastError = false)]
        public static extern uint EnumerateSecurityPackages(
            out UInt64 pcPackages,
            out IntPtr ppPackageInfo
        );

        [DllImport("Secur32.dll")]
        public static extern int FreeContextBuffer(IntPtr pvContextBuffer);
        #endregion

        #region Structure Defintions
        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_STRING_IN
        {
            public ushort Length;
            public ushort MaximumLength;
            public string Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_STRING_OUT
        {
            public ushort Length;
            public ushort MaximumLength;
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

        public enum SECURITY_LOGON_TYPE : uint
        {
            Interactive = 2,        // logging on interactively.
            Network,                // logging using a network.
            Batch,                  // logon for a batch process.
            Service,                // logon for a service account.
            Proxy,                  // Not supported.
            Unlock,                 // Tattempt to unlock a workstation.
            NetworkCleartext,       // network logon with cleartext credentials
            NewCredentials,         // caller can clone its current token and specify new credentials for outbound connections
            RemoteInteractive,      // terminal server session that is both remote and interactive
            CachedInteractive,      // attempt to use the cached credentials without going out across the network
            CachedRemoteInteractive,// same as RemoteInteractive, except used internally for auditing purposes
            CachedUnlock            // attempt to unlock a workstation
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_LOGON_SESSION_DATA
        {
            public uint Size;
            public LUID LoginID;
            public LSA_STRING_OUT Username;
            public LSA_STRING_OUT LoginDomain;
            public LSA_STRING_OUT AuthenticationPackage;
            public uint LogonType;
            public uint Session;
            public IntPtr PSiD;
            public ulong LoginTime;
            public LSA_STRING_OUT LogonServer;
            public LSA_STRING_OUT DnsDomainName;
            public LSA_STRING_OUT Upn;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecPkgInfo
        {
            public SECPKG_FLAGS fCapabilities;
            public short wVersion;
            public short wRPCID;
            public uint cbMaxToken;
            [MarshalAs(UnmanagedType.LPStr)]
            public string Name;
            [MarshalAs(UnmanagedType.LPStr)]
            public string Comment;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public UInt32 LowPart;
            public Int32 HighPart;

            public LUID(UInt64 value)
            {
                LowPart = (UInt32)(value & 0xffffffffL);
                HighPart = (Int32)(value >> 32);
            }

            public LUID(LUID value)
            {
                LowPart = value.LowPart;
                HighPart = value.HighPart;
            }

            public LUID(string value)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(value, @"^0x[0-9A-Fa-f]+$"))
                {
                    // if the passed LUID string is of form 0xABC123
                    UInt64 uintVal = Convert.ToUInt64(value, 16);
                    LowPart = (UInt32)(uintVal & 0xffffffffL);
                    HighPart = (Int32)(uintVal >> 32);
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d+$"))
                {
                    // if the passed LUID string is a decimal form
                    UInt64 uintVal = UInt64.Parse(value);
                    LowPart = (UInt32)(uintVal & 0xffffffffL);
                    HighPart = (Int32)(uintVal >> 32);
                }
                else
                {
                    ArgumentException argEx = new ArgumentException("Passed LUID string value is not in a hex or decimal form", value);
                    throw argEx;
                }
            }

            public override int GetHashCode()
            {
                UInt64 Value = ((UInt64)HighPart << 32) + LowPart;
                return Value.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is LUID && (((ulong)this) == (LUID)obj);
            }

            public byte[] GetBytes()
            {
                byte[] bytes = new byte[8];

                byte[] lowBytes = BitConverter.GetBytes(LowPart);
                byte[] highBytes = BitConverter.GetBytes(HighPart);

                Array.Copy(lowBytes, 0, bytes, 0, 4);
                Array.Copy(highBytes, 0, bytes, 4, 4);

                return bytes;
            }

            public override string ToString()
            {
                UInt64 Value = ((UInt64)HighPart << 32) + LowPart;
                return String.Format("0x{0:x}", (ulong)Value);
            }

            public static bool operator ==(LUID x, LUID y)
            {
                return (((ulong)x) == ((ulong)y));
            }

            public static bool operator !=(LUID x, LUID y)
            {
                return (((ulong)x) != ((ulong)y));
            }

            public static implicit operator ulong(LUID luid)
            {
                // enable casting to a ulong
                UInt64 Value = ((UInt64)luid.HighPart << 32);
                return Value + luid.LowPart;
            }
        }

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

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_HANDLE
        {
            public IntPtr LowPart;
            public IntPtr HighPart;
        };

        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
            public LUID_AND_ATTRIBUTES[] Privileges;

            public TOKEN_PRIVILEGES(uint PrivilegeCount, LUID_AND_ATTRIBUTES[] Privileges)
            {
                this.PrivilegeCount = PrivilegeCount;
                this.Privileges = Privileges;
            }
        }

        #endregion

        #region Enum Definitions

        [Flags]
        public enum SECPKG_FLAGS : uint
        {
            INTEGRITY = 0x1,
            PRIVACY = 0x2,
            TOKEN_ONLY = 0x4,
            DATAGRAM = 0x8,
            CONNECTION = 0x10,
            MULTI_REQUIRED = 0x20,
            CLIENT_ONLY = 0x40,
            EXTENDED_ERROR = 0x80,
            IMPERSONATION = 0x100,
            ACCEPT_WIN32_NAME = 0x200,
            STREAM = 0x400,
            NEGOTIABLE = 0X800,
            GSS_COMPATIBLE = 0x1000,
            LOGON = 0x2000,
            ASCII_BUFFERS = 0x4000,
            FRAGMENT = 0x8000,
            MUTUAL_AUTH = 0x10000,
            DELEGATION = 0x20000,
            READONLY_WITH_CHECKSUM = 0x40000,
            RESTRICTED_TOKENS = 0x80000,
            NEGO_EXTENDER = 0x00100000,
            NEGOTIABLE2 = 0x00200000,
            APPCONTAINER_PASSTHROUGH = 0x00400000,
            APPCONTAINER_CHECKS = 0x00800000
        }
        #endregion

        #region Helper Functions

        #endregion
    }
}
