using System;
using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    public class Wlanapi
    {
        /// <summary>
        /// Defines the state of the interface. e.g. connected, disconnected.
        /// </summary>
        public enum WLAN_INTERFACE_STATE
        {
            /// <summary>
            /// wlan_interface_state_not_ready -> 0
            /// </summary>
            wlan_interface_state_not_ready = 0,

            /// <summary>
            /// wlan_interface_state_connected -> 1
            /// </summary>
            wlan_interface_state_connected = 1,

            /// <summary>
            /// wlan_interface_state_ad_hoc_network_formed -> 2
            /// </summary>
            wlan_interface_state_ad_hoc_network_formed = 2,

            /// <summary>
            /// wlan_interface_state_disconnecting -> 3
            /// </summary>
            wlan_interface_state_disconnecting = 3,

            /// <summary>
            /// wlan_interface_state_disconnected -> 4
            /// </summary>
            wlan_interface_state_disconnected = 4,

            /// <summary>
            /// wlan_interface_state_associating -> 5
            /// </summary>
            wlan_interface_state_associating = 5,

            /// <summary>
            /// wlan_interface_state_discovering -> 6
            /// </summary>
            wlan_interface_state_discovering = 6,

            /// <summary>
            /// wlan_interface_state_authenticating -> 7
            /// </summary>
            wlan_interface_state_authenticating = 7,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            /// GUID->_GUID
            public Guid InterfaceGuid;

            /// WCHAR[256]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;

            /// WLAN_INTERFACE_STATE->_WLAN_INTERFACE_STATE
            public WLAN_INTERFACE_STATE isState;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_PROFILE_INFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public uint dwFlags;
        }

        public struct WLAN_PROFILE_INFO_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_PROFILE_INFO[] ProfileInfo;

            public WLAN_PROFILE_INFO_LIST(IntPtr ppProfileList)
            {
                dwNumberOfItems = (uint)Marshal.ReadInt32(ppProfileList);
                dwIndex = (uint)Marshal.ReadInt32(ppProfileList, 4);
                ProfileInfo = new WLAN_PROFILE_INFO[dwNumberOfItems];
                IntPtr ppProfileListTemp = new IntPtr(ppProfileList.ToInt64() + 8);

                for (int i = 0; i < dwNumberOfItems; i++)
                {
                    ppProfileList = new IntPtr(ppProfileListTemp.ToInt64() + i * Marshal.SizeOf(typeof(WLAN_PROFILE_INFO)));
                    ProfileInfo[i] = (WLAN_PROFILE_INFO)Marshal.PtrToStructure(ppProfileList, typeof(WLAN_PROFILE_INFO));
                }
            }
        }

        [DllImport("Wlanapi.dll")]
        public static extern int WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, [Out] out uint pdwNegotiatedVersion, out IntPtr ClientHandle);

        [DllImport("Wlanapi.dll")]
        public static extern uint WlanEnumInterfaces([In] IntPtr hClientHandle, IntPtr pReserved, ref IntPtr ppInterfaceList);

        [DllImport("Wlanapi.dll")]
        public static extern uint WlanGetProfileList(IntPtr hClientHandle, [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, IntPtr pReserved, out IntPtr ppProfileList);

        [DllImport("Wlanapi.dll")]
        public static extern uint WlanGetProfile(IntPtr clientHandle, [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [MarshalAs(UnmanagedType.LPWStr)] string profileName, IntPtr pReserved, [MarshalAs(UnmanagedType.LPWStr)] out string profileXml, ref uint flags, out uint pdwGrantedAccess);

        [DllImport("Wlanapi", EntryPoint = "WlanFreeMemory")]
        public static extern void WlanFreeMemory([In] IntPtr pMemory);

        [DllImport("Wlanapi", EntryPoint = "WlanCloseHandle")]
        public static extern uint WlanCloseHandle([In] IntPtr hClientHandle, IntPtr pReserved);
    }
}
