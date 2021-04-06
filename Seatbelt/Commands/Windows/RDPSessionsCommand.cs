using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Seatbelt.Commands;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
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
            var computerName = "localhost";

            if (!string.IsNullOrEmpty(ThisRunTime.ComputerName))
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
                var sessionCount = 0;
                var level = 1;

                if (!WTSEnumerateSessionsEx(server, ref level, 0, ref ppSessionInfo, ref sessionCount))
                {
                    var errCode = Marshal.GetLastWin32Error();
                    WriteError($"Failed to enumerate sessions on {server}. Error: {errCode} - " + new Win32Exception(errCode).Message);
                    yield break;
                }

                var current = ppSessionInfo;

                for (var i = 0; i < sessionCount; i++)
                {
                    var si = (WTS_SESSION_INFO_1)Marshal.PtrToStructure(current, typeof(WTS_SESSION_INFO_1));
                    current = (IntPtr)(current.ToInt64() + Marshal.SizeOf(typeof(WTS_SESSION_INFO_1)));

                    // Now use WTSQuerySessionInformation to get the remote IP (if any) for the connection
                    IPAddress? clientIp = null;
                    if (WTSQuerySessionInformation(server, si.SessionID, WTS_INFO_CLASS.WTSClientAddress,
                        out var addressPtr, out _))
                    {
                        var address =
                            (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure(addressPtr, typeof(WTS_CLIENT_ADDRESS));

                        // TODO: Support other address families
                        if (address.AddressFamily == ADDRESS_FAMILY.AF_INET)
                        {
                            var str = string.Join(".", address.Address.Skip(2).Take(4).Select(b => b.ToString()).ToArray());
                            clientIp = IPAddress.Parse(str);
                        }

                        WTSFreeMemory(addressPtr);
                    }

                    // Get Source Hostname
                    string? clientHostname = null;
                    if (WTSQuerySessionInformation(server, si.SessionID, WTS_INFO_CLASS.WTSClientName,
                        out var hostnamePtr, out _))
                    {
                        clientHostname = Marshal.PtrToStringAuto(hostnamePtr);
                        WTSFreeMemory(hostnamePtr);
                    }

                    //Get Source Display
                    WTS_CLIENT_DISPLAY? clientResolution = null;
                    if (WTSQuerySessionInformation(server, si.SessionID, WTS_INFO_CLASS.WTSClientDisplay,
                        out var displayPtr, out _))
                    {
                        clientResolution =
                            (WTS_CLIENT_DISPLAY)Marshal.PtrToStructure(displayPtr, typeof(WTS_CLIENT_DISPLAY));

                        WTSFreeMemory(displayPtr);
                    }

                    // Get Client Build
                    int? clientBuild = null;
                    if (WTSQuerySessionInformation(server, si.SessionID, WTS_INFO_CLASS.WTSClientBuildNumber,
                        out var clientBuildNumberPtr, out _))
                    {
                        clientBuild = Marshal.ReadInt32(clientBuildNumberPtr);
                        WTSFreeMemory(clientBuildNumberPtr);
                    }

                    // Get last input time
                    // Vista / Windows Server 2008+. - Previous versions we need to implement WINSTATIONINFORMATION - TODO
                    long? lastInputTime = null;
                    if (Environment.OSVersion.Version >= new Version(6, 0) &&
                        WTSQuerySessionInformation(server, si.SessionID, WTS_INFO_CLASS.WTSSessionInfo,
                        out var sessionInfoPtr, out _))
                    {
                        var sessionInfo = (WTSINFO)Marshal.PtrToStructure(sessionInfoPtr, typeof(WTSINFO));

                        lastInputTime = sessionInfo.LastInputTime;

                        WTSFreeMemory(sessionInfoPtr);
                    }

                    // Get Client's hardwareId
                    byte[]? clientHardwareId = null;
                    if (WTSQuerySessionInformation(server, si.SessionID, WTS_INFO_CLASS.WTSClientHardwareId, out var buffer, out var bytesRead))
                    {
                        clientHardwareId = new byte[bytesRead];
                        Marshal.Copy(buffer, clientHardwareId, 0, (int)bytesRead);
                        WTSFreeMemory(buffer);
                    }

                    // Get Client's directory
                    string? clientDirectory = null;
                    if (WTSQuerySessionInformation(server, si.SessionID, WTS_INFO_CLASS.WTSClientDirectory, out buffer, out _))
                    {
                        clientDirectory = Marshal.PtrToStringUni(buffer);
                        WTSFreeMemory(buffer);
                    }

                    yield return new RDPSessionsDTO(
                        si.SessionID,
                        si.pSessionName,
                        si.pUserName,
                        si.pDomainName,
                        si.State,
                        si.pHostName,
                        si.pFarmName,
                        lastInputTime,
                        clientIp,
                        clientHostname,
                        clientResolution,
                        clientBuild,
                        clientHardwareId,
                        clientDirectory
                    );
                }

                WTSFreeMemory(ppSessionInfo);
            }
            finally
            {
                WTSCloseServer(server);
            }
        }
    }

    internal class RDPSessionsDTO : CommandDTOBase
    {
        public RDPSessionsDTO(uint sessionId, string sessionName, string userName, string domainName, WTS_CONNECTSTATE_CLASS state, string hostName, string farmName, long? lastInputTime, IPAddress? clientIp, string? clientHostname, WTS_CLIENT_DISPLAY? clientResolution, int? clientBuild, byte[]? clientHardwareId, string? clientDirectory)
        {
            SessionID = sessionId;
            SessionName = sessionName;
            UserName = userName;
            DomainName = domainName;
            State = state;
            HostName = hostName;
            FarmName = farmName;
            LastInputTime = lastInputTime;
            ClientIp = clientIp;
            ClientHostname = clientHostname;
            ClientResolution = clientResolution;
            ClientBuild = clientBuild;
            ClientHardwareId = clientHardwareId;
            ClientDirectory = clientDirectory;
        }
        public uint SessionID { get; }
        public string SessionName { get; }
        public string UserName { get; }
        public string DomainName { get; }
        public WTS_CONNECTSTATE_CLASS State { get; }
        public string HostName { get; }
        public string FarmName { get; }

        public long? LastInputTime { get; }
        public IPAddress? ClientIp { get; }
        public string? ClientHostname { get; }
        public WTS_CLIENT_DISPLAY? ClientResolution { get; }
        public int? ClientBuild { get; }
        public byte[]? ClientHardwareId { get; }
        public string? ClientDirectory { get; }
    }

    [CommandOutputType(typeof(RDPSessionsDTO))]
    internal class RdpSessionsTextFormatter : TextFormatterBase
    {
        public RdpSessionsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (RDPSessionsDTO)result;

            var lastInputStr = "";
            if (dto.LastInputTime != null)
            {
                var lastInputDt = DateTime.FromFileTimeUtc((long)dto.LastInputTime);
                var t = DateTime.UtcNow - lastInputDt;

                lastInputStr = $"{t.Hours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s:{t.Milliseconds:D3}ms";
            }


            var clientResolution = "";
            if (dto.ClientResolution != null && dto.ClientResolution?.HorizontalResolution != 0)
            {
                var res = dto.ClientResolution;
                clientResolution =
                    $"{res?.HorizontalResolution}x{res?.VerticalResolution} @ {res?.ColorDepth} bits per pixel";
            }

            WriteLine($"  {"SessionID",-30}:  {dto.SessionID}");
            WriteLine($"  {"SessionName",-30}:  {dto.SessionName}");
            WriteLine($"  {"UserName",-30}:  {dto.DomainName}\\{dto.UserName}");
            WriteLine($"  {"State",-30}:  {dto.State}");
            WriteLine($"  {"HostName",-30}:  {dto.HostName}");
            WriteLine($"  {"FarmName",-30}:  {dto.FarmName}");
            WriteLine($"  {"LastInput",-30}:  {lastInputStr}");
            WriteLine($"  {"ClientIP",-30}:  {dto.ClientIp}");
            WriteLine($"  {"ClientHostname",-30}:  {dto.ClientHostname}");
            WriteLine($"  {"ClientResolution",-30}:  {clientResolution}");
            WriteLine($"  {"ClientBuild",-30}:  {dto.ClientBuild}");
            WriteLine($"  {"ClientHardwareId",-30}:  {string.Join(",", dto.ClientHardwareId.Select(b => b.ToString()).ToArray())}");
            WriteLine($"  {"ClientDirectory",-30}:  {dto.ClientDirectory}\n");
        }
    }
}