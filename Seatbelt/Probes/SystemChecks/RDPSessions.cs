using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class RDPSessions :IProbe
    {

        public static string ProbeName => "RDPSessions";


        public string List()
        {
            var sb = new StringBuilder();
            
            // adapted from http://www.pinvoke.net/default.aspx/wtsapi32.wtsenumeratesessions
            var server = IntPtr.Zero;
            var ret = new List<string>();
            server = Helpers.OpenServer("localhost");

            sb.AppendProbeHeaderLine("Current Host RDP Sessions (qwinsta)");
           
            try
            {
                var ppSessionInfo = IntPtr.Zero;

                var count = 0;
                var level = 1;
                Int32 retval = NativeMethods.WTSEnumerateSessionsEx(server, ref level, 0, ref ppSessionInfo, ref count);
                var dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO_1));
                var current = (Int64)ppSessionInfo;

                if (retval != 0)
                {
                    for (var i = 0; i < count; i++)
                    {
                        WTS_SESSION_INFO_1 si = (WTS_SESSION_INFO_1)Marshal.PtrToStructure((IntPtr)current, typeof(WTS_SESSION_INFO_1));
                        current += dataSize;

                        sb.AppendLine($"  SessionID:       {si.SessionID}");
                        sb.AppendLine($"  SessionName:     {si.pSessionName}");
                        sb.AppendLine($"  UserName:        {si.pUserName}");
                        sb.AppendLine($"  DomainName:      {si.pDomainName}");
                        sb.AppendLine($"  State:           {si.State}");

                        // Now use WTSQuerySessionInformation to get the remote IP (if any) for the connection
                        var addressPtr = IntPtr.Zero;
                        uint bytes = 0;

                        NativeMethods.WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientAddress, out addressPtr, out bytes);
                        WTS_CLIENT_ADDRESS address = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure((IntPtr)addressPtr, typeof(WTS_CLIENT_ADDRESS));

                        if (address.Address[2] != 0)
                        {
                            var sourceIP = $"{address.Address[2]}.{address.Address[3]}.{address.Address[4]}.{address.Address[5]}";
                            sb.AppendLine($"  SourceIP:        {sourceIP}").AppendLine();
                        }
                        else
                        {
                            sb.AppendLine("  SourceIP: ").AppendLine();
                        }
                    }

                    NativeMethods.WTSFreeMemory(ppSessionInfo);
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }
            finally
            {
                Helpers.CloseServer(server);
            }


            return sb.ToString();
        }
    }
}
