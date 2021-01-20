using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using LSA_HANDLE = System.IntPtr;
using static Seatbelt.Interop.Secur32;

namespace Seatbelt.Interop
{
    internal class Advapi32
    {
        #region Function Definitions

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeName(
            string? lpSystemName,
            IntPtr lpLuid,
            StringBuilder? lpName,
            ref int cchName);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint I_QueryTagInformation(
            IntPtr Unknown,
            SC_SERVICE_TAG_QUERY_TYPE Type,
            ref SC_SERVICE_TAG_QUERY Query
            );

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ConvertSidToStringSid(IntPtr pSid, out string strSid);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool LookupAccountSid(
          string? lpSystemName,
          [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
          StringBuilder lpName,
          ref uint cchName,
          StringBuilder ReferencedDomainName,
          ref uint cchReferencedDomainName,
          out SID_NAME_USE peUse);

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        public static extern uint LsaOpenPolicy(
            LSA_UNICODE_STRING[]? SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            int AccessMask,
            out IntPtr PolicyHandle
        );

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        public static extern uint LsaEnumerateAccountsWithUserRight(
            LSA_HANDLE PolicyHandle,
            LSA_UNICODE_STRING[] UserRights,
            out IntPtr EnumerationBuffer,
            out int CountReturned
        );

        [DllImport("advapi32")]
        public static extern int LsaNtStatusToWinError(int NTSTATUS);

        [DllImport("advapi32")]
        public static extern int LsaClose(IntPtr PolicyHandle);

        [DllImport("advapi32")]
        public static extern int LsaFreeMemory(IntPtr Buffer);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegOpenKeyEx(
            UIntPtr hKey,
            string subKey,
            uint ulOptions,
            uint samDesired,
            out IntPtr hkResult);

        [DllImport("advapi32.dll", EntryPoint = "GetNamedSecurityInfoW", CharSet = CharSet.Unicode)]
        public static extern int GetNamedSecurityInfo(
            string objectName,
            SE_OBJECT_TYPE objectType,
            System.Security.AccessControl.SecurityInfos securityInfo,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(
            IntPtr SecurityDescriptor,
            uint StringSDRevision,
            System.Security.AccessControl.SecurityInfos SecurityInformation,
            out IntPtr StringSecurityDescriptor,
            out int StringSecurityDescriptorSize);

        // for GetSystem()
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            UInt32 DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll")]
        public static extern bool DuplicateToken(
            IntPtr ExistingTokenHandle,
            int SECURITY_IMPERSONATION_LEVEL,
            ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(
            IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        internal static extern void CredFree(
            [In] IntPtr cred);

        [DllImport("advapi32.dll", EntryPoint = "CredEnumerate", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredEnumerate(
            string filter,
            int flag,
            out int count,
            out IntPtr pCredentials);

        [DllImport("Advapi32", SetLastError = false)]
        public static extern bool IsTextUnicode(
                byte[] buf,
                int len,
                ref IsTextUnicodeFlags opt
            );

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptAcquireContext(ref IntPtr hProv, string pszContainer, string pszProvider, uint dwProvType, long dwFlags);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptReleaseContext(IntPtr hProv, uint dwFlags);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptCreateHash(IntPtr hProv, uint algId, IntPtr hKey, uint dwFlags, ref IntPtr phHash);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDestroyHash(IntPtr hHash);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptHashData(IntPtr hHash, byte[] pbData, uint dataLen, uint flags);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDeriveKey(IntPtr hProv, uint Algid, IntPtr hBaseData, int flags, ref IntPtr phKey);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDestroyKey(IntPtr hKey);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDecrypt(IntPtr hKey, IntPtr hHash, bool Final, uint dwFlags, byte[] pbData, ref uint pdwDataLen);




        #endregion


        #region Enum Definitions

        public enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin
        }

        public enum SC_SERVICE_TAG_QUERY_TYPE
        {
            ServiceNameFromTagInformation = 1,
            ServiceNamesReferencingModuleInformation = 2,
            ServiceNameTagMappingInformation = 3
        }

        public enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE = 0,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
        }


        [Flags]
        public enum LuidAttributes : uint
        {
            DISABLED = 0x00000000,
            SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001,
            SE_PRIVILEGE_ENABLED = 0x00000002,
            SE_PRIVILEGE_REMOVED = 0x00000004,
            SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000
        }


        public enum CredentialType : uint
        {
            None = 0,
            Generic = 1,
            DomainPassword = 2,
            DomainCertificate = 3,
            DomainVisiblePassword = 4,
            GenericCertificate = 5,
            DomainExtended = 6,
            Maximum = 7,
            CredTypeMaximum = Maximum + 1000
        }

        public enum PersistenceType : uint
        {
            Session = 1,
            LocalComputer = 2,
            Enterprise = 3
        }

        // for unicode detection
        [Flags]
        public enum IsTextUnicodeFlags : int
        {
            IS_TEXT_UNICODE_ASCII16 = 0x0001,
            IS_TEXT_UNICODE_REVERSE_ASCII16 = 0x0010,

            IS_TEXT_UNICODE_STATISTICS = 0x0002,
            IS_TEXT_UNICODE_REVERSE_STATISTICS = 0x0020,

            IS_TEXT_UNICODE_CONTROLS = 0x0004,
            IS_TEXT_UNICODE_REVERSE_CONTROLS = 0x0040,

            IS_TEXT_UNICODE_SIGNATURE = 0x0008,
            IS_TEXT_UNICODE_REVERSE_SIGNATURE = 0x0080,

            IS_TEXT_UNICODE_ILLEGAL_CHARS = 0x0100,
            IS_TEXT_UNICODE_ODD_LENGTH = 0x0200,
            IS_TEXT_UNICODE_DBCS_LEADBYTE = 0x0400,
            IS_TEXT_UNICODE_NULL_BYTES = 0x1000,

            IS_TEXT_UNICODE_UNICODE_MASK = 0x000F,
            IS_TEXT_UNICODE_REVERSE_MASK = 0x00F0,
            IS_TEXT_UNICODE_NOT_UNICODE_MASK = 0x0F00,
            IS_TEXT_UNICODE_NOT_ASCII_MASK = 0xF000
        }

        #endregion


        #region Structure Defintions

        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public int Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LSA_UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_ENUMERATION_INFORMATION
        {
            public IntPtr PSid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SC_SERVICE_TAG_QUERY
        {
            public uint ProcessId;
            public uint ServiceTag;
            public uint Unknown;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOKEN_STATISTICS
        {
            public LUID TokenId;
            public LUID AuthenticationId;
            public long ExpirationTime;
            public uint TokenType;
            public uint ImpersonationLevel;
            public uint DynamicCharged;
            public uint DynamicAvailable;
            public uint GroupCount;
            public uint PrivilegeCount;
            public LUID ModifiedId;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct CREDENTIAL
        {
            public int Flags;
            public CredentialType Type;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
            public long LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public PersistenceType Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
        }

        #endregion


        #region Helper Functions

        // Based off of code by Lee Christensen(@tifkin_) from https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-NetworkConnection.ps1#L1046
        public static string? GetServiceNameFromTag(uint processId, uint serviceTag)
        {
            var serviceTagQuery = new SC_SERVICE_TAG_QUERY
            {
                ProcessId = processId,
                ServiceTag = serviceTag
            };

            var res = I_QueryTagInformation(IntPtr.Zero, SC_SERVICE_TAG_QUERY_TYPE.ServiceNameFromTagInformation, ref serviceTagQuery);

            return res == Win32Error.Success ? Marshal.PtrToStringUni(serviceTagQuery.Buffer) : null;
        }

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

            var err = 0;
            if (!LookupAccountSid(null, accountSidByes, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out var sidUse))
            {
                err = Marshal.GetLastWin32Error();
                if (err == Win32Error.InsufficientBuffer)
                {
                    name.EnsureCapacity((int)cchName);
                    referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
                    err = 0;
                    if (!LookupAccountSid(null, accountSidByes, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
                        err = Marshal.GetLastWin32Error();
                }
            }

            return err == 0 ? $"{referencedDomainName}\\{name}" : "";
        }

        #endregion
    }
}
