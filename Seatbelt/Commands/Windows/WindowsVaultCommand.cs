#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    class VaultEntry
    {
        public string Resource { get; set; }
        public string Identity { get; set; }

        public string PackageSid { get; set; }

        public string Credential { get; set; }

        public DateTime LastModified { get; set; }
    }

    internal class WindowsVaultCommand : CommandBase
    {
        public override string Command => "WindowsVault"; 
        public override string Description => "Credentials saved in the Windows Vault (i.e. logins from Internet Explorer and Edge)."; 
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public WindowsVaultCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
            var OSVersion = Environment.OSVersion.Version;

            Type VAULT_ITEM;

            //if (OSMajor >= 6 && OSMinor >= 2)
            if (OSVersion > new Version("6.2"))
            {
                VAULT_ITEM = typeof(VaultCli.VAULT_ITEM_WIN8);
            }
            else
            {
                VAULT_ITEM = typeof(VaultCli.VAULT_ITEM_WIN7);
            }

            var vaultCount = 0;
            var vaultGuidPtr = IntPtr.Zero;
            var result = VaultCli.VaultEnumerateVaults(0, ref vaultCount, ref vaultGuidPtr);

            //var result = CallVaultEnumerateVaults(VaultEnum, 0, ref vaultCount, ref vaultGuidPtr);

            if (result != 0)
            {
                WriteError("Unable to enumerate vaults. Error (0x" + result + ")");
                yield break;
            }

            // Create dictionary to translate Guids to human readable elements
            var guidAddress = vaultGuidPtr;
            var vaultSchema = new Dictionary<Guid, string>
            {
                { new Guid("2F1A6504-0641-44CF-8BB5-3612D865F2E5"), "Windows Secure Note" },
                { new Guid("3CCD5499-87A8-4B10-A215-608888DD3B55"), "Windows Web Password Credential" },
                { new Guid("154E23D0-C644-4E6F-8CE6-5069272F999F"), "Windows Credential Picker Protector" },
                { new Guid("4BF4C442-9B8A-41A0-B380-DD4A704DDB28"), "Web Credentials" },
                { new Guid("77BC582B-F0A6-4E15-4E80-61736B6F3B29"), "Windows Credentials" },
                { new Guid("E69D7838-91B5-4FC9-89D5-230D4D4CC2BC"), "Windows Domain Certificate Credential" },
                { new Guid("3E0E35BE-1B77-43E7-B873-AED901B6275B"), "Windows Domain Password Credential" },
                { new Guid("3C886FF3-2669-4AA2-A8FB-3F6759A77548"), "Windows Extended Credential" },
                { new Guid("00000000-0000-0000-0000-000000000000"), null }
            };

            for (var i = 0; i < vaultCount; i++)
            {
                // Open vault block
                var vaultGuidString = Marshal.PtrToStructure(guidAddress, typeof(Guid));
                var vaultGuid = new Guid(vaultGuidString.ToString());
                guidAddress = (IntPtr)(guidAddress.ToInt64() + Marshal.SizeOf(typeof(Guid)));
                var vaultHandle = IntPtr.Zero;
                string vaultType;

                vaultType = vaultSchema.ContainsKey(vaultGuid) ? vaultSchema[vaultGuid] : vaultGuid.ToString();
                result = VaultCli.VaultOpenVault(ref vaultGuid, (uint)0, ref vaultHandle);
                if (result != 0)
                {
                    WriteError("Unable to open the following vault: " + vaultType + ". Error: 0x" + result);
                    continue;
                }
                // Vault opened successfully! Continue.

                var entries = new List<VaultEntry>();

                // Fetch all items within Vault
                var vaultItemCount = 0;
                var vaultItemPtr = IntPtr.Zero;
                result = VaultCli.VaultEnumerateItems(vaultHandle, 512, ref vaultItemCount, ref vaultItemPtr);
                if (result != 0)
                {
                    WriteError("Unable to enumerate vault items from the following vault: " + vaultType + ". Error 0x" + result);
                    continue;
                }
                var structAddress = vaultItemPtr;
                if (vaultItemCount > 0)
                {
                    // For each vault item...
                    for (var j = 1; j <= vaultItemCount; j++)
                    {
                        // Begin fetching vault item...
                        var currentItem = Marshal.PtrToStructure(structAddress, VAULT_ITEM);
                        structAddress = (IntPtr)(structAddress.ToInt64() + Marshal.SizeOf(VAULT_ITEM));

                        var passwordVaultItem = IntPtr.Zero;
                        // Field Info retrieval
                        var schemaIdInfo = currentItem.GetType().GetField("SchemaId");
                        var schemaId = new Guid(schemaIdInfo.GetValue(currentItem).ToString());
                        var pResourceElementInfo = currentItem.GetType().GetField("pResourceElement");
                        var pResourceElement = (IntPtr)pResourceElementInfo.GetValue(currentItem);
                        var pIdentityElementInfo = currentItem.GetType().GetField("pIdentityElement");
                        var pIdentityElement = (IntPtr)pIdentityElementInfo.GetValue(currentItem);
                        var dateTimeInfo = currentItem.GetType().GetField("LastModified");
                        var lastModified = (ulong)dateTimeInfo.GetValue(currentItem);

                        var pPackageSid = IntPtr.Zero;
                        if (OSVersion > new Version("6.2"))
                        {
                            // Newer versions have package sid
                            var pPackageSidInfo = currentItem.GetType().GetField("pPackageSid");
                            pPackageSid = (IntPtr)pPackageSidInfo.GetValue(currentItem);
                            result = VaultCli.VaultGetItem_WIN8(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, pPackageSid, IntPtr.Zero, 0, ref passwordVaultItem);
                        }
                        else
                        {
                            result = VaultCli.VaultGetItem_WIN7(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, IntPtr.Zero, 0, ref passwordVaultItem);
                        }

                        if (result != 0)
                        {
                            WriteError("Could not retrieve vault vault item. Error: 0x" + result);
                            continue;
                        }

                        var passwordItem = Marshal.PtrToStructure(passwordVaultItem, VAULT_ITEM);
                        var pAuthenticatorElementInfo = passwordItem.GetType().GetField("pAuthenticatorElement");
                        var pAuthenticatorElement = (IntPtr)pAuthenticatorElementInfo.GetValue(passwordItem);

                        try
                        {
                            // Fetch the credential from the authenticator element
                            var cred = GetVaultElementValue(pAuthenticatorElement);
                            object packageSid = null;
                            if (pPackageSid != IntPtr.Zero && pPackageSid != null)
                            {
                                packageSid = GetVaultElementValue(pPackageSid);
                            }

                            if (cred != null) // Indicates successful fetch
                            {
                                if (Runtime.FilterResults)
                                {
                                    if (String.IsNullOrEmpty(cred.ToString().TrimEnd()))
                                    {
                                        continue;
                                    }
                                }

                                var entry = new VaultEntry();

                                var resource = GetVaultElementValue(pResourceElement);
                                if (resource != null)
                                {
                                    entry.Resource = $"{resource}";
                                }
                                var identity = GetVaultElementValue(pIdentityElement);
                                if (identity != null)
                                {
                                    entry.Identity = $"{identity}";
                                }
                                if (packageSid != null)
                                {
                                    entry.PackageSid = $"{packageSid}";
                                }

                                entry.Credential = $"{cred}";
                                entry.LastModified = DateTime.FromFileTimeUtc((long)lastModified);

                                entries.Add(entry);
                            }
                        }
                        catch (Exception e)
                        {
                            WriteError("Exception: " + e.Message);
                            continue;
                        }
                    }
                }

                yield return new WindowsVaultDTO()
                {
                    VaultGUID = vaultGuid,
                    VaultType = vaultType,
                    VaultEntries = entries
                };
            }
        }

        private object GetVaultElementValue(IntPtr vaultElementPtr)
        {
            // Helper function to extract the ItemValue field from a VAULT_ITEM_ELEMENT struct
            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
            object results;
            var partialElement = Marshal.PtrToStructure(vaultElementPtr, typeof(VaultCli.VAULT_ITEM_ELEMENT));
            var partialElementInfo = partialElement.GetType().GetField("Type");
            var partialElementType = partialElementInfo.GetValue(partialElement);

            // Types: https://github.com/SpiderLabs/portia/blob/master/modules/Get-VaultCredential.ps1#L40-L54
            var elementPtr = (IntPtr)(vaultElementPtr.ToInt64() + 16);
            switch ((int)partialElementType)
            {
                case 0: // VAULT_ELEMENT_TYPE == bool
                    results = Marshal.ReadByte(elementPtr);
                    results = (bool)results;
                    break;
                case 1: // VAULT_ELEMENT_TYPE == Short
                    results = Marshal.ReadInt16(elementPtr);
                    break;
                case 2: // VAULT_ELEMENT_TYPE == Unsigned Short
                    results = Marshal.ReadInt16(elementPtr);
                    break;
                case 3: // VAULT_ELEMENT_TYPE == Int
                    results = Marshal.ReadInt32(elementPtr);
                    break;
                case 4: // VAULT_ELEMENT_TYPE == Unsigned Int
                    results = Marshal.ReadInt32(elementPtr);
                    break;
                case 5: // VAULT_ELEMENT_TYPE == Double
                    results = Marshal.PtrToStructure(elementPtr, typeof(double));
                    break;
                case 6: // VAULT_ELEMENT_TYPE == GUID
                    results = Marshal.PtrToStructure(elementPtr, typeof(Guid));
                    break;
                case 7: // VAULT_ELEMENT_TYPE == String; These are the plaintext passwords!
                    var StringPtr = Marshal.ReadIntPtr(elementPtr);
                    results = Marshal.PtrToStringUni(StringPtr);
                    break;
                case 12: // VAULT_ELEMENT_TYPE == Sid
                    var sidPtr = Marshal.ReadIntPtr(elementPtr);
                    var sidObject = new System.Security.Principal.SecurityIdentifier(sidPtr);
                    results = sidObject.Value;
                    break;
                default:
                    /* Several VAULT_ELEMENT_TYPES are currently unimplemented according to
                     * Lord Graeber. Thus we do not implement them. */
                    throw new Exception(String.Format("VAULT_ELEMENT_TYPE '{0}' is currently unimplemented", partialElementType));
            }
            return results;
        }

        internal class WindowsVaultDTO : CommandDTOBase
        {
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
                WriteLine($"  Vault Type     : {dto.VaultType}\n");

                foreach(var entry in dto.VaultEntries)
                {
                    if (!String.IsNullOrEmpty(entry.Resource))
                    {
                        WriteLine($"    Resource     : {entry.Resource}");
                    }
                    if (!String.IsNullOrEmpty(entry.Identity))
                    {
                        WriteLine($"    Identity     : {entry.Identity}");
                    }
                    if (!String.IsNullOrEmpty(entry.PackageSid))
                    {
                        WriteLine($"    PacakgeSid  : {entry.PackageSid}");
                    }
                    WriteLine($"    Credential   : {entry.Credential}");
                    WriteLine($"    LastModified : {entry.LastModified}\n");
                }
            }
        }
    }
}
#nullable enable