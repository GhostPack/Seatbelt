using System;
using System.Runtime.InteropServices;

namespace Seatbelt
{
    public static class VaultCli
    {
        // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
        public enum VAULT_ELEMENT_TYPE : int
        {
            Undefined = -1,
            Boolean = 0,
            Short = 1,
            UnsignedShort = 2,
            Int = 3,
            UnsignedInt = 4,
            Double = 5,
            Guid = 6,
            String = 7,
            ByteArray = 8,
            TimeStamp = 9,
            ProtectedArray = 10,
            Attribute = 11,
            Sid = 12,
            Last = 13
        }

        public enum VAULT_SCHEMA_ELEMENT_ID : int
        {
            Illegal = 0,
            Resource = 1,
            Identity = 2,
            Authenticator = 3,
            Tag = 4,
            PackageSid = 5,
            AppStart = 100,
            AppEnd = 10000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct VAULT_ITEM_WIN8
        {
            public Guid SchemaId;
            public IntPtr pszCredentialFriendlyName;
            public IntPtr pResourceElement;
            public IntPtr pIdentityElement;
            public IntPtr pAuthenticatorElement;
            public IntPtr pPackageSid;
            public ulong LastModified;
            public uint dwFlags;
            public uint dwPropertiesCount;
            public IntPtr pPropertyElements;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct VAULT_ITEM_WIN7
        {
            public Guid SchemaId;
            public IntPtr pszCredentialFriendlyName;
            public IntPtr pResourceElement;
            public IntPtr pIdentityElement;
            public IntPtr pAuthenticatorElement;
            public ulong LastModified;
            public uint dwFlags;
            public uint dwPropertiesCount;
            public IntPtr pPropertyElements;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct VAULT_ITEM_ELEMENT
        {
            [FieldOffset(0)]
            public VAULT_SCHEMA_ELEMENT_ID SchemaElementId;
            [FieldOffset(8)]
            public VAULT_ELEMENT_TYPE Type;
            //[FieldOffset(16)]
            //public Guid Guid;
        }

        //typedef struct _VAULT_BYTE_BUFFER
        //{
        //    DWORD Length;
        //    PBYTE Value;
        //}
        //VAULT_BYTE_BUFFER, *PVAULT_BYTE_BUFFER;
        [StructLayout(LayoutKind.Sequential)]
        public struct VAULT_BYTE_ARRAY
        {
            public int Length;
            public IntPtr pData;
        }

        [DllImport("vaultcli.dll")]
        public static extern int VaultOpenVault(ref Guid vaultGuid, uint offset, ref IntPtr vaultHandle);

        [DllImport("vaultcli.dll")]
        public static extern int VaultCloseVault(ref IntPtr vaultHandle);

        [DllImport("vaultcli.dll")]
        public static extern int VaultFree(ref IntPtr vaultHandle);

        [DllImport("vaultcli.dll")]
        public static extern int VaultEnumerateVaults(int offset, ref int vaultCount, ref IntPtr vaultGuid);

        [DllImport("vaultcli.dll")]
        public static extern int VaultEnumerateItems(IntPtr vaultHandle, int chunkSize, ref int vaultItemCount, ref IntPtr vaultItem);

        [DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
        public static extern int VaultGetItem_WIN8(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr pPackageSid, IntPtr zero, int arg6, ref IntPtr passwordVaultPtr);

        [DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
        public static extern int VaultGetItem_WIN7(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr zero, int arg5, ref IntPtr passwordVaultPtr);

    }

}
