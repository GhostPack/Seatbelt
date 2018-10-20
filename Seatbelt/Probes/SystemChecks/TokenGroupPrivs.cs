using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public  class TokenGroupPrivs :IProbe
    {

        public static string ProbeName => "TokenGroupPrivs";

        public string List()
        {
            // Returns all privileges that the current process/user possesses
            // adapted from https://stackoverflow.com/questions/4349743/setting-size-of-token-privileges-luid-and-attributes-array-returned-by-gettokeni
            var sb = new StringBuilder();

            try
            {
                sb.AppendProbeHeaderLine("Current Privileges");

                var tokenInfLength = 0;
                var thisHandle = WindowsIdentity.GetCurrent().Token;
                NativeMethods.GetTokenInformation(thisHandle, TokenInformation.TokenPrivileges, IntPtr.Zero, tokenInfLength, out tokenInfLength);
                var tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

                if (NativeMethods.GetTokenInformation(WindowsIdentity.GetCurrent().Token, TokenInformation.TokenPrivileges, tokenInformation, tokenInfLength, out tokenInfLength))
                {
                    var thisPrivilegeSet = (TOKEN_PRIVILEGES)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_PRIVILEGES));
                    for (var index = 0; index < thisPrivilegeSet.PrivilegeCount; index++)
                    {
                        var laa = thisPrivilegeSet.Privileges[index];

                        var strBuilder = new StringBuilder();
                        var luidNameLen = 0;
                        var luidPointer = Marshal.AllocHGlobal(Marshal.SizeOf(laa.Luid));

                        Marshal.StructureToPtr(laa.Luid, luidPointer, true);
                        NativeMethods.LookupPrivilegeName(null, luidPointer, null, ref luidNameLen);

                        strBuilder.EnsureCapacity(luidNameLen + 1);

                        if (NativeMethods.LookupPrivilegeName(null, luidPointer, strBuilder, ref luidNameLen))
                        {
                            sb.AppendLine($"  {strBuilder.ToString(),43}:  {(LuidAttributes) laa.Attributes}");
                        }

                        Marshal.FreeHGlobal(luidPointer);
                    }
                }

            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();

        }
    }
}
