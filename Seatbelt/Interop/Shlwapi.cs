using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    class Shlwapi
    {
        public static bool IsWindowsServer()
        {
            return IsOS(OS_ANYSERVER);
        }

        const int OS_ANYSERVER = 29;

        [DllImport("shlwapi.dll", SetLastError = true, EntryPoint = "#437")]
        private static extern bool IsOS(int os);
    }
}
