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
    internal class UdpConnectionsCommand : CommandBase
    {
        public override string Command => "UdpConnections";
        public override string Description => "Current UDP connections and associated processes and services";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false;

        public UdpConnectionsCommand(Runtime runtime) : base(runtime)
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

            WriteHost("  Local Address          PID    Service                 ProcessName");

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

                var ret = GetExtendedUdpTable(IntPtr.Zero, ref tableBufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret != Win32Error.Success && ret != Win32Error.InsufficientBuffer)
                {
                    // 122 == insufficient buffer size
                    WriteError($"Bad check value from GetExtendedUdpTable : {ret}");
                    yield break;
                }

                tableBuffer = Marshal.AllocHGlobal((int)tableBufferSize);

                ret = GetExtendedUdpTable(tableBuffer, ref tableBufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret != Win32Error.Success)
                {
                    WriteError($"Bad return value from GetExtendedUdpTable : {ret}");
                    yield break;
                }

                // get the number of entries in the table
                var ownerModuleTable = (MIB_UDPTABLE_OWNER_MODULE)Marshal.PtrToStructure(tableBuffer, typeof(MIB_UDPTABLE_OWNER_MODULE));
                rowPtr = (IntPtr)(tableBuffer.ToInt64() + Marshal.OffsetOf(typeof(MIB_UDPTABLE_OWNER_MODULE), "Table").ToInt64());
                var UdpRows = new MIB_UDPROW_OWNER_MODULE[ownerModuleTable.NumEntries];

                for (var i = 0; i < ownerModuleTable.NumEntries; i++)
                {
                    var udpRow =
                        (MIB_UDPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_MODULE));
                    UdpRows[i] = udpRow;
                    // next entry
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(udpRow));
                }

                foreach (var entry in UdpRows)
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

                    yield return new UdpConnectionsDTO(
                        entry.LocalAddress.ToString(),
                        entry.LocalPort,
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

    internal class UdpConnectionsDTO : CommandDTOBase
    {
        public UdpConnectionsDTO(string localAddress, ushort localPort, uint processId, string? processName, string? processCommandLine, string? service)
        {
            LocalAddress = localAddress;
            LocalPort = localPort;
            ProcessId = processId;
            ProcessName = processName;
            ProcessCommandLine = processCommandLine;
            ServiceName = service;
        }
        public string LocalAddress { get; set; }
        public ushort LocalPort { get; set; }
        public uint ProcessId { get; set; }
        public string? ProcessName { get; }
        public string? ProcessCommandLine { get; }
        public string? ServiceName { get; }
    }

    [CommandOutputType(typeof(UdpConnectionsDTO))]
    internal class UdpConnectionsTextFormatter : TextFormatterBase
    {
        public UdpConnectionsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            if (result != null)
            {
                var dto = (UdpConnectionsDTO)result;
                if (dto.ProcessCommandLine != null)
                {
                    WriteLine("  {0,-23}{1,-7}{2,-23} {3}", dto.LocalAddress + ":" + dto.LocalPort, dto.ProcessId, dto.ServiceName, dto.ProcessCommandLine);
                }
                else
                {
                    WriteLine("  {0,-23}{1,-7}{2,-23} {3}", dto.LocalAddress + ":" + dto.LocalPort, dto.ProcessId, dto.ServiceName, dto.ProcessName);
                }
            }
        }
    }
}
