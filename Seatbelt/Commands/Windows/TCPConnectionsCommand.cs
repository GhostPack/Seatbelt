using Seatbelt.Interop;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using Seatbelt.Output.Formatters;
using static Seatbelt.Interop.Iphlpapi;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    internal class TcpConnectionsCommand : CommandBase
    {
        public override string Command => "TcpConnections";
        public override string Description => "Current TCP connections and their associated processes and services";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false;

        public TcpConnectionsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var AF_INET = 2;    // IP_v4
            uint tableBufferSize = 0;
            var tableBuffer = IntPtr.Zero;
            var rowPtr = IntPtr.Zero;
            var processNames = new Dictionary<string, string>();
            var processCommandLines = new Dictionary<string, string>();

            WriteHost("  Local Address          Foreign Address        State      PID   Service         ProcessName");

            try
            {
                // Adapted from https://stackoverflow.com/questions/577433/which-pid-listens-on-a-given-port-in-c-sharp/577660#577660
                // Build a PID -> process name lookup table
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process");
                var retObjectCollection = searcher.Get();

                foreach (ManagementObject Process in retObjectCollection)
                {
                    processNames.Add(Process["ProcessId"].ToString(), Process["Name"].ToString());
                    if (Process["CommandLine"] != null)
                    {
                        processCommandLines.Add(Process["ProcessId"].ToString(), Process["CommandLine"].ToString());
                    }
                }

                // Figure out how much memory we need for the result struct
                var ret = GetExtendedTcpTable(IntPtr.Zero, ref tableBufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret != Win32Error.Success && ret != Win32Error.InsufficientBuffer)
                {
                    // 122 == insufficient buffer size
                    WriteError($"Bad check value from GetExtendedTcpTable : {ret}");
                    yield break;
                }

                tableBuffer = Marshal.AllocHGlobal((int)tableBufferSize);

                ret = GetExtendedTcpTable(tableBuffer, ref tableBufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret != Win32Error.Success)
                {
                    WriteError($"Bad return value from GetExtendedTcpTable : {ret}");
                    yield break;
                }

                // get the number of entries in the table
                var ownerModuleTable = (MIB_TCPTABLE_OWNER_MODULE)Marshal.PtrToStructure(tableBuffer, typeof(MIB_TCPTABLE_OWNER_MODULE));
                rowPtr = (IntPtr)(tableBuffer.ToInt64() + Marshal.OffsetOf(typeof(MIB_TCPTABLE_OWNER_MODULE), "Table").ToInt64());
                var TcpRows = new MIB_TCPROW_OWNER_MODULE[ownerModuleTable.NumEntries];

                for (var i = 0; i < ownerModuleTable.NumEntries; i++)
                {
                    var tcpRow =
                        (MIB_TCPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_MODULE));
                    TcpRows[i] = tcpRow;
                    // next entry
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));
                }

                foreach (var entry in TcpRows)
                {
                    string? processName = null;
                    try
                    {
                        processName = processNames[entry.OwningPid.ToString()];
                    }
                    catch { }

                    string? processCommandLine = null;
                    try
                    {
                        processCommandLine = processCommandLines[entry.OwningPid.ToString()];
                    }
                    catch { }

                    var serviceName = Advapi32.GetServiceNameFromTag(entry.OwningPid, (uint)entry.OwningModuleInfo0);


                    yield return new TcpConnectionsDTO(
                        entry.LocalAddress.ToString(),
                        entry.LocalPort,
                        entry.RemoteAddress.ToString(),
                        entry.RemotePort,
                        entry.State,
                        entry.OwningPid,
                        processName,
                        processCommandLine,
                        serviceName
                    );
                }
            }
            finally
            {
                if (tableBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tableBuffer);
                }
            }
        }
    }

    internal class TcpConnectionsDTO : CommandDTOBase
    {
        public TcpConnectionsDTO(string localAddress, ushort localPort, string remoteAddress, ushort remotePort, MIB_TCP_STATE state, uint processId, string? processName, string? processCommandLine, string? service)
        {
            LocalAddress = localAddress;
            LocalPort = localPort;
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
            State = state;
            ProcessId = processId;
            ProcessName = processName;
            ProcessCommandLine = processCommandLine;
            ServiceName = service;
        }

        public string LocalAddress { get; }
        public ushort LocalPort { get; }
        public string RemoteAddress { get; }
        public ushort RemotePort { get; }
        public MIB_TCP_STATE State { get; }
        public uint ProcessId { get; }
        public string? ProcessName { get; }
        public string? ProcessCommandLine { get; }
        public string? ServiceName { get; }
    }

    [CommandOutputType(typeof(TcpConnectionsDTO))]
    internal class TcpConnectionsTextFormatter : TextFormatterBase
    {
        public TcpConnectionsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            if (result != null)
            {
                var dto = (TcpConnectionsDTO)result;
                if (dto.ProcessCommandLine != null)
                {
                    WriteLine("  {0,-23}{1,-23}{2,-11}{3,-6}{4,-15} {5}", dto.LocalAddress + ":" + dto.LocalPort, dto.RemoteAddress + ":" + dto.RemotePort, dto.State, dto.ProcessId, dto.ServiceName, dto.ProcessCommandLine);
                }
                else
                {
                    WriteLine("  {0,-23}{1,-23}{2,-11}{3,-6}{4,-15} {5}", dto.LocalAddress + ":" + dto.LocalPort, dto.RemoteAddress + ":" + dto.RemotePort, dto.State, dto.ProcessId, dto.ServiceName, dto.ProcessName);
                }
            }
        }
    }
}