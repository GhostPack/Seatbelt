using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.UserChecks
{
    public class DumpVault : IProbe
    {

        public static string ProbeName => "DumpVault";


        public string List()
        {
            var sb = new StringBuilder();

            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
            
            sb.AppendProbeHeaderLine("Checking Windows Vaults");

            var OSVersion = Environment.OSVersion.Version;
            var OSMajor = OSVersion.Major;
            var OSMinor = OSVersion.Minor;

            Type VAULT_ITEM;

            if (OSMajor >= 6 && OSMinor >= 2)
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

            if ((int)result != 0)
            {
                sb.AppendLine("  [ERROR] Unable to enumerate vaults. Error (0x" + result.ToString() + ")");
                return sb.ToString();
            }

            // Create dictionary to translate Guids to human readable elements
            var guidAddress = vaultGuidPtr;
            var vaultSchema = new Dictionary<Guid, string>
            {
                {new Guid("2F1A6504-0641-44CF-8BB5-3612D865F2E5"), "Windows Secure Note"},
                {new Guid("3CCD5499-87A8-4B10-A215-608888DD3B55"), "Windows Web Password Credential"},
                {new Guid("154E23D0-C644-4E6F-8CE6-5069272F999F"), "Windows Credential Picker Protector"},
                {new Guid("4BF4C442-9B8A-41A0-B380-DD4A704DDB28"), "Web Credentials"},
                {new Guid("77BC582B-F0A6-4E15-4E80-61736B6F3B29"), "Windows Credentials"},
                {new Guid("E69D7838-91B5-4FC9-89D5-230D4D4CC2BC"), "Windows Domain Certificate Credential"},
                {new Guid("3E0E35BE-1B77-43E7-B873-AED901B6275B"), "Windows Domain Password Credential"},
                {new Guid("3C886FF3-2669-4AA2-A8FB-3F6759A77548"), "Windows Extended Credential"},
                {new Guid("00000000-0000-0000-0000-000000000000"), null}
            };

            for (var i = 0; i < vaultCount; i++)
            {
                // Open vault block
                var vaultGuidString = Marshal.PtrToStructure(guidAddress, typeof(Guid));
                var vaultGuid = new Guid(vaultGuidString.ToString());
                guidAddress = (IntPtr)(guidAddress.ToInt64() + Marshal.SizeOf(typeof(Guid)));
                var vaultHandle = IntPtr.Zero;
                string vaultType;
                if (vaultSchema.ContainsKey(vaultGuid))
                {
                    vaultType = vaultSchema[vaultGuid];
                }
                else
                {
                    vaultType = vaultGuid.ToString();
                }
                result = VaultCli.VaultOpenVault(ref vaultGuid, (UInt32)0, ref vaultHandle);
                if (result != 0)
                {
                    sb.AppendLine("  [ERROR] Unable to open the following vault: " + vaultType + ". Error: 0x" + result.ToString());
                    return sb.ToString();
                }
                // Vault opened successfully! Continue.

                sb.AppendLine($"  Vault GUID     : {vaultGuid}");
                sb.AppendLine($"  Vault Type     : {vaultType}");
                sb.AppendLine();

                // Fetch all items within Vault
                var vaultItemCount = 0;
                var vaultItemPtr = IntPtr.Zero;
                result = VaultCli.VaultEnumerateItems(vaultHandle, 512, ref vaultItemCount, ref vaultItemPtr);
                if (result != 0)
                {
                    sb.AppendLine("  [ERROR] Unable to enumerate vault items from the following vault: " + vaultType + ". Error 0x" + result.ToString());
                    return sb.ToString();
                }

                var structAddress = vaultItemPtr;
                if (vaultItemCount <= 0) continue;

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
                    var lastModified = (UInt64)dateTimeInfo.GetValue(currentItem);

                    var pPackageSid = IntPtr.Zero;
                    if (OSMajor >= 6 && OSMinor >= 2)
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
                        sb.AppendLine("  [ERROR] occurred while retrieving vault item. Error: 0x" + result.ToString());
                        return sb.ToString();
                    }

                    var passwordItem = Marshal.PtrToStructure(passwordVaultItem, VAULT_ITEM);
                    var pAuthenticatorElementInfo = passwordItem.GetType().GetField("pAuthenticatorElement");
                    var pAuthenticatorElement = (IntPtr)pAuthenticatorElementInfo.GetValue(passwordItem);

                    // Fetch the credential from the authenticator element
                    var cred = GetVaultElementValue(pAuthenticatorElement);
                    object packageSid = null;
                    if (pPackageSid != IntPtr.Zero && pPackageSid != null)
                    {
                        packageSid = GetVaultElementValue(pPackageSid);
                    }

                    if (cred != null) // Indicates successful fetch
                    {
                        // Console.WriteLine("  --- IE/Edge Credential ---");
                        // Console.WriteLine("  Vault Type   : {0}", vaultType);
                        var resource = GetVaultElementValue(pResourceElement);
                        if (resource != null)
                        {
                            sb.AppendLine($"    Resource     : {resource}");
                        }
                        var identity = GetVaultElementValue(pIdentityElement);
                        if (identity != null)
                        {
                            sb.AppendLine($"    Identity     : {identity}");
                        }
                        if (packageSid != null)
                        {
                            sb.AppendLine($"    PacakgeSid  : {packageSid}");
                        }
                        sb.AppendLine($"    Credential   : {cred}");
                        // Stupid datetime
                        sb.AppendLine($"    LastModified : {DateTime.FromFileTimeUtc((long)lastModified)}");
                        sb.AppendLine();
                    }
                }
            }


            return sb.ToString();
        }


        private static object GetVaultElementValue(IntPtr vaultElementPtr)
        {
            // Helper function to extract the ItemValue field from a VAULT_ITEM_ELEMENT struct
            // pulled directly from @djhohnstein's SharpWeb project: https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs
            object results;
            var partialElement = Marshal.PtrToStructure(vaultElementPtr, typeof(VaultCli.VAULT_ITEM_ELEMENT));
            var partialElementInfo = partialElement.GetType().GetField("Type");
            var partialElementType = partialElementInfo.GetValue(partialElement);

            var elementPtr = (IntPtr)(vaultElementPtr.ToInt64() + 16);
            switch ((int)partialElementType)
            {
                case 7: // VAULT_ELEMENT_TYPE == String; These are the plaintext passwords!
                    var StringPtr = Marshal.ReadIntPtr(elementPtr);
                    results = Marshal.PtrToStringUni(StringPtr);
                    break;
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
                    results = Marshal.PtrToStructure(elementPtr, typeof(Double));
                    break;
                case 6: // VAULT_ELEMENT_TYPE == GUID
                    results = Marshal.PtrToStructure(elementPtr, typeof(Guid));
                    break;
                case 12: // VAULT_ELEMENT_TYPE == Sid
                    var sidPtr = Marshal.ReadIntPtr(elementPtr);
                    var sidObject = new SecurityIdentifier(sidPtr);
                    results = sidObject.Value;
                    break;
                default:
                    /* Several VAULT_ELEMENT_TYPES are currently unimplemented according to
                     * Lord Graeber. Thus we do not implement them. */
                    results = null;
                    break;
            }
            return results;
        }

    }
}
