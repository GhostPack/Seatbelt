#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Seatbelt.Interop.Wtsapi32;


namespace Seatbelt.Commands.Windows
{
    internal class RDPSessionsCommand : CommandBase
    {
        public override string Command => "RDPSessions";
        public override string Description => "Current incoming RDP sessions (argument == computername to enumerate)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public RDPSessionsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // adapted from http://www.pinvoke.net/default.aspx/wtsapi32.wtsenumeratesessions
            string computerName = "localhost";

            if (!String.IsNullOrEmpty(ThisRunTime.ComputerName))
            {
                computerName = ThisRunTime.ComputerName;
            }
            else if (args.Length == 1)
            {
                computerName = args[0];
            }

            var server = WTSOpenServer(computerName);

            try
            {
                var ppSessionInfo = IntPtr.Zero;
                var count = 0;
                var level = 1;
                var retval = WTSEnumerateSessionsEx(server, ref level, 0, ref ppSessionInfo, ref count);
                var dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO_1));
                var current = (long)ppSessionInfo;

                if (retval != 0)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var si = (WTS_SESSION_INFO_1)Marshal.PtrToStructure((IntPtr)current, typeof(WTS_SESSION_INFO_1));
                        current += dataSize;

                        // Now use WTSQuerySessionInformation to get the remote IP (if any) for the connection

                        WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientAddress, out var addressPtr, out _);
                        var address = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure(addressPtr, typeof(WTS_CLIENT_ADDRESS));

                        string sourceIp = null;
                        if (address.Address[2] != 0)
                        {
                            sourceIp = $"{address.Address[2]}.{address.Address[3]}.{address.Address[4]}.{address.Address[5]}";
                        }

                        yield return new RDPSessionsDTO()
                        {
                            SessionID = si.SessionID,
                            SessionName = si.pSessionName,
                            UserName = si.pUserName,
                            DomainName = si.pDomainName,
                            State = si.State,
                            SourceIp = sourceIp
                        };
                    }

                    WTSFreeMemory(ppSessionInfo);
                }
            }
            finally
            {
                WTSCloseServer(server);
            }
        }
    }

    internal class RDPSessionsDTO : CommandDTOBase
    {
        public int SessionID { get; set; }
        public string SessionName { get; set; }
        public string UserName { get; set; }
        public string DomainName { get; set; }
        public WTS_CONNECTSTATE_CLASS State { get; set; }
        public string SourceIp { get; set; }
    }
}
#nullable enable