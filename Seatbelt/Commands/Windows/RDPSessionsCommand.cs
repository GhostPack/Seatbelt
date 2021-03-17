#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                        WTSFreeMemory(addressPtr);

                        // Get Source Hostname
                        WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientName, out var hostnamePtr, out _);
                        string sourceHostname = Marshal.PtrToStringAuto(hostnamePtr);
                        WTSFreeMemory(hostnamePtr);

                        //Get Source Display

                        WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientDisplay, out var displayPtr, out _);
                        WTS_CLIENT_DISPLAY sourceDisplay = (WTS_CLIENT_DISPLAY)Marshal.PtrToStructure(displayPtr, typeof(WTS_CLIENT_DISPLAY));

                        string sourceResolution = "";
                        if (sourceDisplay.HorizontalResolution != 0)
                        {
                            sourceResolution = String.Format("{0}x{1} @ {2} bits per pixel", sourceDisplay.HorizontalResolution, sourceDisplay.VerticalResolution, sourceDisplay.ColorDepth);
                        }
                        WTSFreeMemory(displayPtr);

                        // Get Client Build
                        WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientBuildNumber, out var clientBuildNumberPtr, out _);
                        int sourceBuildNumber = Marshal.ReadInt32(clientBuildNumberPtr);
                        string sourceClientBuild = "";
                        if(sourceBuildNumber != 0)
                        {
                            sourceClientBuild = sourceBuildNumber.ToString();
                        }

                        WTSFreeMemory(clientBuildNumberPtr);

                        // Get the last input time to calculate idle time
                        string idleTimeString = "";

                        // Vista / Windows Server 2008+. - Previous versions we need to implement WINSTATIONINFORMATION - TODO
                        if (Environment.OSVersion.Version >= new Version(6, 0))
                        {
                            WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSSessionInfo, out var sessionInfoPtr, out _);
                            WTSINFO sessionInfo = (WTSINFO)Marshal.PtrToStructure(sessionInfoPtr, typeof(WTSINFO));
                            
                            long lastInput = sessionInfo.LastInputTime;
                            if (lastInput != 0)
                            {
                                DateTime lastInputDt = DateTime.FromFileTimeUtc(lastInput);
                                TimeSpan idleTime = DateTime.Now - lastInputDt;
                                idleTimeString = String.Format("{0} minute{1}", idleTime.Minutes, idleTime.Minutes == 1 ? "" : "s");
                            }
                            WTSFreeMemory(sessionInfoPtr);
                        }

                        yield return new RDPSessionsDTO()
                        {
                            SessionID = si.SessionID,
                            SessionName = si.pSessionName,
                            UserName = si.pUserName,
                            DomainName = si.pDomainName,
                            State = si.State,
                            IdleTime = idleTimeString,
                            SourceIp = sourceIp,
                            SourceHostname = sourceHostname,
                            SourceResolution = sourceResolution,
                            SourceClientBuild = sourceClientBuild,
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
        public string IdleTime { get; set; }
        public string SourceIp { get; set; }
        
        public string SourceHostname { get; set; }

        public string SourceResolution { get; set; }
        public string SourceClientBuild { get; set; }
    }
}
#nullable enable