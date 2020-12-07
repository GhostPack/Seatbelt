using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using static Seatbelt.VaultCli;


namespace Seatbelt.Commands.Windows
{
    class VaultEntry
    {
        public Guid SchemaGuidId { get; set; }
        public VaultItemValue? Resource { get; set; }
        public VaultItemValue? Identity { get; set; }

        public VaultItemValue? PackageSid { get; set; }

        public VaultItemValue? Credential { get; set; }

        public DateTime LastModifiedUtc { get; set; }
    }

    internal class WindowsVaultCommand : CommandBase
    {
        public override string Command => "WindowsVault";
        public override string Description => "Credentials saved in the Windows Vault (i.e. logins from Internet Explorer and Edge).";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false; // not possible

        private readonly Dictionary<Guid, string> VaultSchema = new Dictionary<Guid, string>
        {
            { new Guid("2F1A6504-0641-44CF-8BB5-3612D865F2E5"), "Windows Secure Note" },
            { new Guid("3CCD5499-87A8-4B10-A215-608888DD3B55"), "Windows Web Password Credential" },
            { new Guid("154E23D0-C644-4E6F-8CE6-5069272F999F"), "Windows Credential Picker Protector" },
            { new Guid("4BF4C442-9B8A-41A0-B380-DD4A704DDB28"), "Web Credentials" },
            { new Guid("77BC582B-F0A6-4E15-4E80-61736B6F3B29"), "Windows Credentials" },
            { new Guid("E69D7838-91B5-4FC9-89D5-230D4D4CC2BC"), "Windows Domain Certificate Credential" },
            { new Guid("3E0E35BE-1B77-43E7-B873-AED901B6275B"), "Windows Domain Password Credential" },
            { new Guid("3C886FF3-2669-4AA2-A8FB-3F6759A77548"), "Windows Extended Credential" },
            { new Guid("00000000-0000-0000-0000-000000000000"), "<blank>" }
        };

        public WindowsVaultCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs

            var vaultCount = 0;
            var vaultGuidPtr = IntPtr.Zero;
            var result = VaultEnumerateVaults(0, ref vaultCount, ref vaultGuidPtr);

            var vaultItemType = Environment.OSVersion.Version > new Version("6.2") ?
                typeof(VAULT_ITEM_WIN8) :
                typeof(VAULT_ITEM_WIN7);

            if (result != 0)
            {
                WriteError($"Unable to enumerate vaults. Error code: {result}");
                yield break;
            }

            // Create dictionary to translate Guids to human readable elements
            var guidAddress = vaultGuidPtr;


            for (var i = 0; i < vaultCount; i++)
            {
                // Open vault block
                var vaultGuidString = Marshal.PtrToStructure(guidAddress, typeof(Guid));
                var vaultGuid = new Guid(vaultGuidString.ToString());
                guidAddress = (IntPtr)(guidAddress.ToInt64() + Marshal.SizeOf(typeof(Guid)));
                var vaultHandle = IntPtr.Zero;

                var vaultType = VaultSchema.ContainsKey(vaultGuid) ?
                    VaultSchema[vaultGuid] :
                    vaultGuid.ToString();

                result = VaultOpenVault(ref vaultGuid, (uint)0, ref vaultHandle);
                if (result != 0)
                {
                    WriteError($"Unable to open the following vault(GUID: {vaultGuid}: {vaultType} . Error code: {result}");
                    continue;
                }
                // Vault opened successfully! Continue.

                var entries = new List<VaultEntry>();

                // Fetch all items within Vault
                var vaultItemCount = 0;
                var vaultItemPtr = IntPtr.Zero;
                result = VaultEnumerateItems(vaultHandle, 512, ref vaultItemCount, ref vaultItemPtr);
                if (result != 0)
                {
                    WriteError($"Unable to enumerate vault items from the following vault: {vaultType}. Error code: {result}");
                    continue;
                }
                var currentVaultItem = vaultItemPtr;
                if (vaultItemCount > 0)
                {
                    // For each vault item...
                    for (var j = 1; j <= vaultItemCount; j++)
                    {
                        var entry = ParseVaultItem(vaultHandle, vaultGuid, currentVaultItem);

                        //if (Runtime.FilterResults && string.IsNullOrEmpty(entry.Credential))
                        //    continue;

                        entries.Add(entry);

                        currentVaultItem = (IntPtr)(currentVaultItem.ToInt64() + Marshal.SizeOf(vaultItemType));
                    }
                }

                yield return new WindowsVaultDTO(
                    vaultGuid,
                    vaultType,
                    entries
                );
            }
        }

        private void GetVaultItem(IntPtr vaultHandle, IntPtr vaultItemPtr, out Guid schemaId, out IntPtr? pPackageSid, out IntPtr pResourceElement, out IntPtr pIdentityElement, out ulong lastModified, out IntPtr pAuthenticatorElement)
        {
            int result;
            var OSVersion = Environment.OSVersion.Version;
            //if (OSMajor >= 6 && OSMinor >= 2)
            var vaultItemType = OSVersion > new Version("6.2") ?
                typeof(VAULT_ITEM_WIN8) :
                typeof(VAULT_ITEM_WIN7);

            // Begin fetching vault item...
            var currentItem = Marshal.PtrToStructure(vaultItemPtr, vaultItemType);

            // Field Info retrieval
            var schemaIdField = currentItem.GetType().GetField("SchemaId");
            var tempSchemaGuidId = new Guid(schemaIdField.GetValue(currentItem).ToString());

            var pResourceElementField = currentItem.GetType().GetField("pResourceElement");
            var tempResourceElement = (IntPtr)pResourceElementField.GetValue(currentItem);

            var pIdentityElementField = currentItem.GetType().GetField("pIdentityElement");
            var tempIdentityElement = (IntPtr)pIdentityElementField.GetValue(currentItem);

            var lastModifiedField = currentItem.GetType().GetField("LastModified");
            var tempLastModified = (ulong)lastModifiedField.GetValue(currentItem);

            // Newer versions have package sid
            IntPtr? tempPackageSid = null;
            if (vaultItemType == typeof(VAULT_ITEM_WIN8))
            {
                var pPackageSidInfo = currentItem.GetType().GetField("pPackageSid");
                tempPackageSid = (IntPtr)pPackageSidInfo.GetValue(currentItem);
            }


            var passwordVaultItem = IntPtr.Zero;
            result = vaultItemType == typeof(VAULT_ITEM_WIN8) ?
                VaultGetItem_WIN8(vaultHandle, ref tempSchemaGuidId, tempResourceElement, tempIdentityElement, tempPackageSid ?? IntPtr.Zero, IntPtr.Zero, 0, ref passwordVaultItem) :
                VaultGetItem_WIN7(vaultHandle, ref tempSchemaGuidId, tempResourceElement, tempIdentityElement, IntPtr.Zero, 0, ref passwordVaultItem);

            if (result != 0)
                throw new Exception($"Could not retrieve vault vault item. Error code: {result}");

            // Return values
            schemaId = tempSchemaGuidId;
            pPackageSid = tempPackageSid;
            pResourceElement = tempResourceElement;
            pIdentityElement = tempIdentityElement;
            lastModified = tempLastModified;

            var passwordItem = Marshal.PtrToStructure(passwordVaultItem, vaultItemType);
            var pAuthenticatorElementInfo = passwordItem.GetType().GetField("pAuthenticatorElement");
            pAuthenticatorElement = (IntPtr)pAuthenticatorElementInfo.GetValue(passwordItem);
        }

        private VaultEntry ParseVaultItem(IntPtr vaultHandle, Guid vaultGuid, IntPtr vaultItemPtr)
        {
            GetVaultItem(vaultHandle, vaultItemPtr,out var schemaGuid, out var pPackageSid, out var pResourceElement, out var pIdentityElement, out var lastModified, out var pAuthenticatorElement);

            // Cred
            VaultItemValue? cred = null;
            try
            {
                // Fetch the credential from the authenticator element
                cred = GetVaultElementValue(pAuthenticatorElement);
            }
            catch (NotImplementedException e)
            {
                WriteError($"Could not parse authenticator for Vault GUID {vaultGuid}: {e}");
            }


            // Package SID
            VaultItemValue? packageSid = null;
            if (pPackageSid != null && pPackageSid != IntPtr.Zero)
            {
                try
                {
                    packageSid = GetVaultElementValue(pPackageSid.Value);
                }
                catch (NotImplementedException e)
                {
                    WriteError($"Could not parse package SID for Vault GUID {vaultGuid}: {e}");
                }
            }


            // Resource
            VaultItemValue? resource = null;
            try
            {
                resource = GetVaultElementValue(pResourceElement);
            }
            catch (NotImplementedException e)
            {
                WriteError($"Could not parse authenticator for Vault GUID {vaultGuid}: {e}");
            }


            VaultItemValue? identity = null;
            try
            {
                identity = GetVaultElementValue(pIdentityElement);
            }
            catch (NotImplementedException e)
            {
                WriteError($"Could not parse identity for Vault GUID {vaultGuid}: {e}");
            }

            return new VaultEntry
            {
                SchemaGuidId = schemaGuid,
                Identity = identity,
                Resource = resource,
                Credential = cred,
                PackageSid = packageSid,
                LastModifiedUtc = DateTime.FromFileTimeUtc((long)lastModified)
            };
        }

        private VaultItemValue GetVaultElementValue(IntPtr vaultElementPtr)
        {
            object value;

            var item = (VAULT_ITEM_ELEMENT)Marshal.PtrToStructure(vaultElementPtr, typeof(VAULT_ITEM_ELEMENT));

            // Types: https://github.com/SpiderLabs/portia/blob/master/modules/Get-VaultCredential.ps1#L40-L54
            var elementPtr = (IntPtr)(vaultElementPtr.ToInt64() + 16);
            switch (item.Type)
            {
                case VAULT_ELEMENT_TYPE.Boolean:
                    value = Marshal.ReadByte(elementPtr);
                    value = (bool)value;
                    break;
                case VAULT_ELEMENT_TYPE.Short:
                    value = Marshal.ReadInt16(elementPtr);
                    break;
                case VAULT_ELEMENT_TYPE.UnsignedShort:
                    value = Marshal.ReadInt16(elementPtr);
                    break;
                case VAULT_ELEMENT_TYPE.Int:
                    value = Marshal.ReadInt32(elementPtr);
                    break;
                case VAULT_ELEMENT_TYPE.UnsignedInt:
                    value = Marshal.ReadInt32(elementPtr);
                    break;
                case VAULT_ELEMENT_TYPE.Double:
                    value = Marshal.PtrToStructure(elementPtr, typeof(double));
                    break;
                case VAULT_ELEMENT_TYPE.Guid:
                    value = Marshal.PtrToStructure(elementPtr, typeof(Guid));
                    break;
                case VAULT_ELEMENT_TYPE.String:
                    var StringPtr = Marshal.ReadIntPtr(elementPtr);
                    value = Marshal.PtrToStringUni(StringPtr);
                    break;
                case VAULT_ELEMENT_TYPE.Sid:
                    var sidPtr = Marshal.ReadIntPtr(elementPtr);
                    var sidObject = new System.Security.Principal.SecurityIdentifier(sidPtr);
                    value = sidObject.Value;
                    break;
                case VAULT_ELEMENT_TYPE.ByteArray:
                    var o = (VAULT_BYTE_ARRAY)Marshal.PtrToStructure(elementPtr, typeof(VAULT_BYTE_ARRAY));
                    var array = new byte[o.Length];
                    if (o.Length > 0)
                    {
                        Marshal.Copy(o.pData, array, 0, o.Length);
                    }
                    value = array;
                    break;

                //case VAULT_ELEMENT_TYPE.Undefined:
                //case VAULT_ELEMENT_TYPE.TimeStamp:
                //case VAULT_ELEMENT_TYPE.ProtectedArray:
                //case VAULT_ELEMENT_TYPE.Attribute:
                //case VAULT_ELEMENT_TYPE.Last:
                default:
                    throw new NotImplementedException($"VAULT_ELEMENT_TYPE '{item.Type}' is currently not implemented");
            }
            return new VaultItemValue(item.Type, value);
        }

        internal class WindowsVaultDTO : CommandDTOBase
        {
            public WindowsVaultDTO(Guid vaultGuid, string vaultType, List<VaultEntry> vaultEntries)
            {
                VaultGUID = vaultGuid;
                VaultType = vaultType;
                VaultEntries = vaultEntries;
            }

            public Guid VaultGUID { get; set; }

            public string VaultType { get; set; }

            public List<VaultEntry> VaultEntries { get; set; }
        }

        [CommandOutputType(typeof(WindowsVaultDTO))]
        internal class WindowsVaultFormatter : TextFormatterBase
        {
            public WindowsVaultFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (WindowsVaultDTO)result;

                WriteLine($"\n  Vault GUID     : {dto.VaultGUID}");
                WriteLine($"  Vault Type     : {dto.VaultType}");
                WriteLine($"  Item count     : {dto.VaultEntries.Count}");

                foreach (var entry in dto.VaultEntries)
                {
                    WriteLine("      SchemaGuid   : " + entry.SchemaGuidId);
                    WriteLine("      Resource     : " + ItemToString(entry.Resource));
                    WriteLine("      Identity     : " + ItemToString(entry.Identity));
                    WriteLine("      PackageSid   : " + ItemToString(entry.PackageSid));
                    WriteLine("      Credential   : " + ItemToString(entry.Credential));
                    WriteLine($"      LastModified : {entry.LastModifiedUtc}");
                }
            }

            private string ItemToString(VaultItemValue? item)
            {
                if (item == null)
                    return "(null)";

                string valueStr;
                switch (item.VaultElementType)
                {
                    case VAULT_ELEMENT_TYPE.Boolean:
                    case VAULT_ELEMENT_TYPE.Short:
                    case VAULT_ELEMENT_TYPE.UnsignedShort:
                    case VAULT_ELEMENT_TYPE.Int:
                    case VAULT_ELEMENT_TYPE.UnsignedInt:
                    case VAULT_ELEMENT_TYPE.Double:
                    case VAULT_ELEMENT_TYPE.Guid:
                    case VAULT_ELEMENT_TYPE.String:
                    case VAULT_ELEMENT_TYPE.Sid:
                        valueStr = $"String: {item.Value.ToString()}";
                        break;
                    case VAULT_ELEMENT_TYPE.ByteArray:
                        valueStr = BitConverter.ToString((byte[])item.Value).Replace("-", " "); ;
                        break;
                    default:
                        valueStr = $"Unable to print a value of type {item.VaultElementType}. Please report an issue!";
                        break;
                }

                return valueStr;
            }
        }
    }

    internal class VaultItemValue
    {
        public VaultItemValue(VAULT_ELEMENT_TYPE vaultElementType, object value)
        {
            VaultElementType = vaultElementType;
            Value = value;
        }
        public VAULT_ELEMENT_TYPE VaultElementType { get; set; }
        public object Value { get; set; }
    }
}
