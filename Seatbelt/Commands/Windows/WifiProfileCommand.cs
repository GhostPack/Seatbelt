using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Xml;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using static Seatbelt.Interop.Wlanapi;

namespace Seatbelt.Commands.Windows
{
    class WifiProfileEntry
    {
        public string? Profile { get; set; }
        public string? SSID { get; set; }
        public string? Authentication { get; set; }
        public string? PassPhrase { get; set; }
        public string? Interface { get; set; }
        public string? State { get; set; }

    }

    internal class WifiProfileCommand : CommandBase
    {
        public override string Command => "WifiProfile";
        public override string Description => "Enumerates the saved Wifi profiles and extract the ssid, authentication type, cleartext key/passphrase (when possible)";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false;

        public WifiProfileCommand(Runtime runtime) : base(runtime)
        {
        }

        enum StateEnum
        {
            NotReady = 0x00000000,
            Connected = 0x00000001,
            AdHocNetworkFormed = 0x00000002,
            Disconnecting = 0x00000003,
            Disconnected = 0x00000004,
            Associating = 0x00000005,
            Discovering = 0x00000006,
            Authenticating = 0x00000007,
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {

            int numberOfInterfaces = 0;
            int numberOfProfiles = 0;

            List<WifiProfileEntry> wifiProfileEntries = new List<WifiProfileEntry>();

            string profile;
            string sSID;
            string authentication;
            string passPhrase;
            string wifiInterface;
            object state;

            var ClientHandle = IntPtr.Zero;
            uint NegotiatedVersion = 0;
            var ret = WlanOpenHandle(2, IntPtr.Zero, out NegotiatedVersion, out ClientHandle);

            if (ret == Interop.Win32Error.Success)
            {
                var InterfaceListPtr = IntPtr.Zero;

                var retWlanEnum = WlanEnumInterfaces(ClientHandle, IntPtr.Zero, ref InterfaceListPtr);

                if (retWlanEnum == Interop.Win32Error.Success)
                {
                    numberOfInterfaces = Marshal.ReadInt32(InterfaceListPtr);

                    var WlanInterfaceInfoPtr = (IntPtr)(InterfaceListPtr.ToInt64() + 8);

                    for (int i = 0; i < numberOfInterfaces; i++)
                    {

                        WLAN_INTERFACE_INFO WlanInterfaceInfo = (WLAN_INTERFACE_INFO)Marshal.PtrToStructure(WlanInterfaceInfoPtr, typeof(WLAN_INTERFACE_INFO));
                        wifiInterface = WlanInterfaceInfo.strInterfaceDescription;
                        state = (StateEnum)WlanInterfaceInfo.isState;

                        IntPtr ProfileListPtr = IntPtr.Zero;

                        var retGetProfileList = WlanGetProfileList(ClientHandle, WlanInterfaceInfo.InterfaceGuid, IntPtr.Zero, out ProfileListPtr);

                        if (retGetProfileList == Interop.Win32Error.Success)
                        {

                            int currentNbrOfProfiles = Marshal.ReadInt32(ProfileListPtr);
                            //Get total of profiles for all interfaces
                            numberOfProfiles += currentNbrOfProfiles;

                            // Calculate the pointer to the first WLAN_PROFILE_INFO structure 
                            var WlanProfileInfoPtr = (IntPtr)(ProfileListPtr.ToInt64() + 8); // dwNumberOfItems + dwIndex

                            for (int j = 0; j < currentNbrOfProfiles; j++)
                            {

                                WLAN_PROFILE_INFO WlanProfileInfo = (WLAN_PROFILE_INFO)Marshal.PtrToStructure(WlanProfileInfoPtr, typeof(WLAN_PROFILE_INFO));

                                string ProfileXml = "";

                                UInt32 WlanProfileFlags = 4; // WLAN_PROFILE_GET_PLAINTEXT_KEY
                                UInt32 WlanProfileAccessFlags = 0;

                                var retGetProfile = WlanGetProfile(ClientHandle, WlanInterfaceInfo.InterfaceGuid, WlanProfileInfo.strProfileName, IntPtr.Zero, out ProfileXml, ref WlanProfileFlags, out WlanProfileAccessFlags);

                                if (retGetProfile == Interop.Win32Error.Success)
                                {
                                    var xmlDoc = new XmlDocument();

                                    xmlDoc.LoadXml(ProfileXml);

                                    XmlNamespaceManager mgr = new XmlNamespaceManager(xmlDoc.NameTable);
                                    mgr.AddNamespace("ns", "http://www.microsoft.com/networking/WLAN/profile/v1");

                                    profile = "";
                                    sSID = "";
                                    authentication = "";
                                    passPhrase = "";


                                    //Catch if keyMaterial is empty
                                    try
                                    {
                                        profile = xmlDoc.SelectSingleNode("//ns:WLANProfile/ns:name", mgr).InnerText;
                                        sSID = xmlDoc.SelectSingleNode("//ns:WLANProfile/ns:SSIDConfig/ns:SSID/ns:name", mgr).InnerText;
                                        passPhrase = xmlDoc.SelectSingleNode("//ns:WLANProfile/ns:MSM/ns:security/ns:sharedKey/ns:keyMaterial", mgr).InnerText;
                                        authentication = xmlDoc.SelectSingleNode("//ns:WLANProfile/ns:MSM/ns:security/ns:authEncryption/ns:authentication", mgr).InnerText;
                                    }
                                    catch { }

                                    WifiProfileEntry CurrentWifiProfileEntry = new WifiProfileEntry
                                    {
                                        Profile = profile,
                                        SSID = sSID,
                                        Authentication = authentication,
                                        PassPhrase = passPhrase,
                                        Interface = wifiInterface,
                                        State = state.ToString()
                                    };

                                    wifiProfileEntries.Add(CurrentWifiProfileEntry);
                                }
                                else
                                {
                                    WriteError($"WlanGetProfile() failed: {retGetProfile}");

                                }

                                WlanProfileInfoPtr = (IntPtr)(WlanProfileInfoPtr.ToInt64() + Marshal.SizeOf(WlanProfileInfo));
                            }

                            WlanFreeMemory(ProfileListPtr);

                        }
                        else
                        {
                            WriteError($"WlanGetProfileList() failed: {retGetProfileList}");
                        }

                        //info
                        WlanInterfaceInfoPtr = (IntPtr)(WlanInterfaceInfoPtr.ToInt64() + Marshal.SizeOf(WlanInterfaceInfo));

                    }

                    //WlanFree
                    WlanFreeMemory(InterfaceListPtr);
                }
                else
                {
                    WriteError($"WlanEnumInterfaces() failed: {retWlanEnum}");
                    yield break;
                }

                var retClose = WlanCloseHandle(ClientHandle, IntPtr.Zero);
                if (ret != Interop.Win32Error.Success) {
                    WriteError($"WlanCloseHandle() failed: {retClose}");
                }

            }
            else
            {
                WriteError($"WlanOpenHandle() failed: {ret}");
                yield break;
            }

            yield return new WifiProfileDTO(numberOfInterfaces, numberOfProfiles, wifiProfileEntries);
        }

        internal class WifiProfileDTO : CommandDTOBase
        {
            public WifiProfileDTO(int nbrInterface, int nbrProfile, List<WifiProfileEntry> wifiProfileEntries)
            {
                NumberOfInterfaces = nbrInterface;
                NumberOfProfiles = nbrProfile;
                WifiProfileEntries = wifiProfileEntries;
            }

            public int NumberOfInterfaces { get; set; }

            public int NumberOfProfiles { get; set; }

            public List<WifiProfileEntry> WifiProfileEntries { get; set; }
        }

        [CommandOutputType(typeof(WifiProfileDTO))]
        internal class WindowsVaultFormatter : TextFormatterBase
        {
            public WindowsVaultFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (WifiProfileDTO)result;

                WriteLine($"Number of interfaces : {dto.NumberOfInterfaces}");
                WriteLine($"Number of profiles   : {dto.NumberOfProfiles}\n");

                foreach (WifiProfileEntry entry in dto.WifiProfileEntries)
                {
                    WriteLine($"Profile        : {entry.Profile}");
                    WriteLine($"SSID           : {entry.SSID}");
                    WriteLine($"Interface      : {entry.Interface}");
                    WriteLine($"State          : {entry.State}");
                    WriteLine($"Authentication : {entry.Authentication}");
                    WriteLine($"PassPrhase     : {entry.PassPhrase}\n");
                }

            }
        }
    }
}