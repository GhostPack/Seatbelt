using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using static Seatbelt.Interop.Secur32;
using static Seatbelt.Interop.Advapi32;
using static Seatbelt.Interop.Kernel32;
using System.Diagnostics;

namespace Seatbelt.Util
{
    internal static class SecurityUtil
    {
        public static bool IsHighIntegrity()
        {
            // returns true if the current process is running with adminstrative privs in a high integrity context
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string[] GetTokenGroupSids()
        {
            // Returns all SIDs that the current user is a part of, whether they are disabled or not.
            // slightly adapted from https://stackoverflow.com/questions/2146153/how-to-get-the-logon-sid-in-c-sharp/2146418#2146418

            var TokenInfLength = 0;

            // first call gets length of TokenInformation
            var Result = GetTokenInformation(WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenGroups, IntPtr.Zero, TokenInfLength, out TokenInfLength);
            var TokenInformation = Marshal.AllocHGlobal(TokenInfLength);
            Result = GetTokenInformation(WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenGroups, TokenInformation, TokenInfLength, out TokenInfLength);

            if (!Result)
            {
                Marshal.FreeHGlobal(TokenInformation);
                throw new Exception("Unable to get token info.");
            }

            var groups = (TOKEN_GROUPS)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_GROUPS));
            var userSIDS = new string[groups.GroupCount];
            var sidAndAttrSize = Marshal.SizeOf(new SID_AND_ATTRIBUTES());
            for (var i = 0; i < groups.GroupCount; i++)
            {
                var sidAndAttributes = (SID_AND_ATTRIBUTES)Marshal.PtrToStructure(
                    new IntPtr(TokenInformation.ToInt64() + i * sidAndAttrSize + IntPtr.Size), typeof(SID_AND_ATTRIBUTES));

                ConvertSidToStringSid(sidAndAttributes.Sid, out var sid);
                userSIDS[i] = sid;
            }

            Marshal.FreeHGlobal(TokenInformation);
            return userSIDS;
        }

        public static bool IsLocalAdmin()
        {
            // checks if the "S-1-5-32-544" in the current token groups set, meaning the user is a local administrator
            return GetTokenGroupSids().Contains("S-1-5-32-544");
        }

        [Flags]
        public enum GenericAceMask : uint
        {
            FileReadData = 0x00000001,
            FileWriteData = 0x00000002,
            FileAppendData = 0x00000004,
            FileReadEA = 0x00000008,
            FileWriteEA = 0x00000010,
            FileExecute = 0x00000020,
            FileDeleteChild = 0x00000040,
            FileReadAttributes = 0x00000080,
            FileWriteAttributes = 0x00000100,

            Delete = 0x00010000,
            ReadControl = 0x00020000,
            WriteDac = 0x00040000,
            WriteOwner = 0x00080000,
            Synchronize = 0x00100000,

            AccessSystemSecurity = 0x01000000,
            MaximumAllowed = 0x02000000,

            GenericAll = 0x10000000,
            GenericExecute = 0x20000000,
            GenericWrite = 0x40000000,
            GenericRead = 0x80000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_INFOS
        {
            public string Owner;
            public RawSecurityDescriptor SecurityDescriptor;
            public string SDDL;
        }

        public static SECURITY_INFOS GetSecurityInfos(string ObjectName, SE_OBJECT_TYPE ObjectType)
        {
            var pSidOwner = IntPtr.Zero;
            var pSidGroup = IntPtr.Zero;
            var pDacl = IntPtr.Zero;
            var pSacl = IntPtr.Zero;
            var pSecurityDescriptor = IntPtr.Zero;
            var info = SecurityInfos.DiscretionaryAcl | SecurityInfos.Owner;

            var infos = new SECURITY_INFOS();

            // get the security infos
            var errorReturn = GetNamedSecurityInfo(ObjectName, ObjectType, info, out pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);
            if (errorReturn != 0)
            {
                return infos;
            }

            if (ConvertSecurityDescriptorToStringSecurityDescriptor(pSecurityDescriptor, 1, SecurityInfos.DiscretionaryAcl | SecurityInfos.Owner, out var pSddlString, out _))
            {
                infos.SDDL = Marshal.PtrToStringUni(pSddlString) ?? string.Empty;
            }
            var ownerSid = new SecurityIdentifier(pSidOwner);
            infos.Owner = ownerSid.Value;

            if (pSddlString != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pSddlString);
            }

            if (pSecurityDescriptor != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pSecurityDescriptor);
            }

            return infos;
        }
    }
}
