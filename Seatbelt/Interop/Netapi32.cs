using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    internal class Netapi32
    {
        [DllImport("Netapi32.dll")]
        public static extern uint NetLocalGroupGetMembers(
            [MarshalAs(UnmanagedType.LPWStr)] string? ServerName,
            [MarshalAs(UnmanagedType.LPWStr)] string LocalGroupName,
            int Level,
            out IntPtr BufPtr,
            int PreferredMaxLength,
            out uint EntriesRead,
            out uint TotalEntries,
            out IntPtr ResumeHandle);

        [DllImport("Netapi32.dll")]
        public static extern uint NetLocalGroupEnum(
            [MarshalAs(UnmanagedType.LPWStr)] string? ServerName,
            int Level,
            out IntPtr BufPtr,
            int PreferredMaxLength,
            out uint EntriesRead,
            out uint TotalEntries,
            out IntPtr ResumeHandle);

        [DllImport("Netapi32.dll")]
        public static extern uint NetUserEnum(
            [MarshalAs(UnmanagedType.LPWStr)] string ServerName,
            uint Level,
            uint Filter,
            out IntPtr BufPtr,
            uint PreferredMaxLength,
            out uint EntriesRead,
            out uint TotalEntries,
            out IntPtr ResumeHandle);

        [DllImport("Netapi32.dll")]
        public static extern int NetApiBufferFree(IntPtr Buffer);

        [DllImport("Netapi32.dll")]
        public static extern uint NetGetJoinInformation(
            [MarshalAs(UnmanagedType.LPWStr)] string lpServer,
            [MarshalAs(UnmanagedType.LPWStr)] string LocalGroupName,
            int Level,
            out IntPtr BufPtr,
            int PreferredMaxLength,
            out int EntriesRead,
            out int TotalEntries,
            out IntPtr ResumeHandle);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern void NetFreeAadJoinInformation(IntPtr pJoinInfo);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetGetAadJoinInformation(string pcszTenantId,out IntPtr ppJoinInfo);

        public static uint MAX_PREFERRED_LENGTH = unchecked((uint)-1);

        public enum SidNameUse
        {
            User = 1,
            Group,
            Domain,
            Alias,
            WellKnownGroup,
            DeletedAccount,
            Invalid,
            Unknown,
            Computer
        }

        public enum Priv
        {
            Guest = 0,
            User = 1,
            Administrator = 2
        }

        // LOCALGROUP_MEMBERS_INFO_2 - Structure for holding members details
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LOCALGROUP_MEMBERS_INFO_2
        {
            public IntPtr lgrmi2_sid;
            public SidNameUse lgrmi2_sidusage;
            public string lgrmi2_domainandname;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LOCALGROUP_INFO_1
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string name;
            [MarshalAs(UnmanagedType.LPWStr)] public string comment;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct USER_INFO_3
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string name;
            [MarshalAs(UnmanagedType.LPWStr)] public string password;
            public uint passwordAge;
            public uint priv;
            [MarshalAs(UnmanagedType.LPWStr)] public string home_dir;
            [MarshalAs(UnmanagedType.LPWStr)] public string comment;
            public uint flags;
            [MarshalAs(UnmanagedType.LPWStr)] public string script_path;
            public uint auth_flags;
            [MarshalAs(UnmanagedType.LPWStr)] public string full_name;
            [MarshalAs(UnmanagedType.LPWStr)] public string usr_comment;
            [MarshalAs(UnmanagedType.LPWStr)] public string parms;
            [MarshalAs(UnmanagedType.LPWStr)] public string workstations;
            public uint last_logon;
            public uint last_logoff;
            public uint acct_expires;
            public uint max_storage;
            public uint units_per_week;
            public IntPtr logon_hours;
            public uint bad_pw_count;
            public uint num_logons;
            [MarshalAs(UnmanagedType.LPWStr)] public string logon_server;
            public uint country_code;
            public uint code_page;
            public uint user_id;
            public uint primary_group_id;
            [MarshalAs(UnmanagedType.LPWStr)] public string profile;
            [MarshalAs(UnmanagedType.LPWStr)] public string home_dir_drive;
            public uint password_expired;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DSREG_JOIN_INFO
        {
            public int joinType;
            public IntPtr pJoinCertificate;
            [MarshalAs(UnmanagedType.LPWStr)] public string DeviceId;
            [MarshalAs(UnmanagedType.LPWStr)] public string IdpDomain;
            [MarshalAs(UnmanagedType.LPWStr)] public string TenantId;
            [MarshalAs(UnmanagedType.LPWStr)] public string JoinUserEmail;
            [MarshalAs(UnmanagedType.LPWStr)] public string TenantDisplayName;
            [MarshalAs(UnmanagedType.LPWStr)] public string MdmEnrollmentUrl;
            [MarshalAs(UnmanagedType.LPWStr)] public string MdmTermsOfUseUrl;
            [MarshalAs(UnmanagedType.LPWStr)] public string MdmComplianceUrl;
            [MarshalAs(UnmanagedType.LPWStr)] public string UserSettingSyncUrl;
            public IntPtr pUserInfo;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DSREG_USER_INFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string UserEmail;
            [MarshalAs(UnmanagedType.LPWStr)] public string UserKeyId;
            [MarshalAs(UnmanagedType.LPWStr)] public string UserKeyName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_PUBLIC_KEY_INFO
        {
            public CRYPT_ALGORITHM_IDENTIFIER Algorithm;
            public CRYPT_BIT_BLOB PublicKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_CONTEXT
        {
            public uint dwCertEncodingType;
            public IntPtr pbCertEncoded;
            public uint cbCertEncoded;
            public IntPtr pCertInfo;
            public IntPtr hCertStore;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPTOAPI_BLOB //x86 - 8, x64 - 16
        {
            public Int32 cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CRYPT_OBJID_BLOB //8
        {
            public Int32 cbData;
            [MarshalAs(UnmanagedType.ByValArray)]
            public byte[] pbData;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_INFO
        {
            public uint dwVersion; //4
            public CRYPTOAPI_BLOB SerialNumber; //8 - 16
            public CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm; //12 - 16
            public CRYPTOAPI_BLOB Issuer; //8 - 16
            public System.Runtime.InteropServices.ComTypes.FILETIME NotBefore; //8
            public System.Runtime.InteropServices.ComTypes.FILETIME NotAfter; //8
            public CRYPTOAPI_BLOB Subject; //8 - 16
            public CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;//24 - 40
            public CRYPT_BIT_BLOB IssuerUniqueId;//12 - 24
            public CRYPT_BIT_BLOB SubjectUniqueId;//12 - 24
            public uint cExtension;//4
            public IntPtr rgExtension;//4 - 8
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_ALGORITHM_IDENTIFIER //12 - 16
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public String pszObjId; //4
            public CRYPT_OBJID_BLOB Parameters;//8
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_BIT_BLOB //12-24
        {
            public Int32 cbData; //4
            public IntPtr pbData;//4-8
            public Int32 cUnusedBits;//4
        }


        // TODO: Need to refactor and create separate classes for everywhere this is used
        public class Principal
        {
            public Principal(string sid, SidNameUse? @class, string user, string domain)
            {
                Sid = sid;
                Class = @class;
                User = user;
                Domain = domain;    
            }
            public string Sid { get; }
            public SidNameUse? Class { get; }
            public string User { get; }
            public string Domain { get; }
        }

        #region Helper Functions
        public static IEnumerable<Principal>? GetLocalGroupMembers(string? computerName, string groupName)
        {
            // returns the "DOMAIN\user" members for a specified local group name
            // adapted from boboes' code at https://stackoverflow.com/questions/33935825/pinvoke-netlocalgroupgetmembers-runs-into-fatalexecutionengineerror/33939889#33939889
            var members = new List<Principal>();
            var retVal = NetLocalGroupGetMembers(computerName, groupName, 2, out var bufPtr, -1, out var EntriesRead, out var TotalEntries, out var Resume);

            if (retVal != 0)
            {
                var errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new Exception("Error code " + retVal + ": " + errorMessage);
            }

            if (EntriesRead == 0)
                return members;

            var names = new string[EntriesRead];
            var memberInfo = new LOCALGROUP_MEMBERS_INFO_2[EntriesRead];
            var iter = bufPtr;

            for (var i = 0; i < EntriesRead; i++)
            {
                memberInfo[i] = (LOCALGROUP_MEMBERS_INFO_2)Marshal.PtrToStructure(iter, typeof(LOCALGROUP_MEMBERS_INFO_2));

                //x64 safe
                iter = new IntPtr(iter.ToInt64() + Marshal.SizeOf(typeof(LOCALGROUP_MEMBERS_INFO_2)));


                var nameParts = memberInfo[i].lgrmi2_domainandname.Split('\\');
                var domain = nameParts[0];
                var userName = "";
                if (nameParts.Length > 1)
                {
                    userName = nameParts[1];
                }

                Advapi32.ConvertSidToStringSid(memberInfo[i].lgrmi2_sid, out var sid);

                members.Add(new Principal(
                    sid,
                    memberInfo[i].lgrmi2_sidusage,
                    userName,
                    domain
                ));
            }
            NetApiBufferFree(bufPtr);

            return members;
        }

        public static IEnumerable<LOCALGROUP_INFO_1> GetLocalGroups(string? computerName)
        {
            // Returns local groups (and comments)
            var retVal = NetLocalGroupEnum(computerName, 1, out var bufPtr, -1, out var entriesRead, out var totalEntries, out var resumeHandle);

            if (retVal != 0)
            {
                var errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new Exception("Error code " + retVal + ": " + errorMessage);
            }

            var groups = new List<LOCALGROUP_INFO_1>();

            if (entriesRead == 0)
                return groups;

            var names = new string[entriesRead];
            var groupInfo = new LOCALGROUP_INFO_1[entriesRead];
            var iter = bufPtr;


            for (var i = 0; i < entriesRead; i++)
            {
                groupInfo[i] = (LOCALGROUP_INFO_1)Marshal.PtrToStructure(iter, typeof(LOCALGROUP_INFO_1));
                groups.Add(groupInfo[i]);

                //x64 safe
                iter = new IntPtr(iter.ToInt64() + Marshal.SizeOf(typeof(LOCALGROUP_INFO_1)));
            }
            NetApiBufferFree(bufPtr);

            return groups;

        }

        public static IEnumerable<USER_INFO_3> GetLocalUsers(string computerName)
        {
            // Returns local users
            //  FILTER_NORMAL_ACCOUNT == 2
            var users = new List<USER_INFO_3>();
            var retVal = NetUserEnum(computerName, 3, 2, out var bufPtr, MAX_PREFERRED_LENGTH, out var EntriesRead, out var TotalEntries, out var Resume);

            if (retVal != 0)
            {
                var errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new Exception("Error code " + retVal + ": " + errorMessage);
            }

            if (EntriesRead == 0)
                return users;

            var names = new string[EntriesRead];
            var userInfo = new USER_INFO_3[EntriesRead];
            var iter = bufPtr;


            for (var i = 0; i < EntriesRead; i++)
            {
                userInfo[i] = (USER_INFO_3)Marshal.PtrToStructure(iter, typeof(USER_INFO_3));
                users.Add(userInfo[i]);

                //x64 safe
                iter = new IntPtr(iter.ToInt64() + Marshal.SizeOf(typeof(USER_INFO_3)));
            }
            NetApiBufferFree(bufPtr);

            return users;
        }
        #endregion
    }
}
