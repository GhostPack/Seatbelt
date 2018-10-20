using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class AllUdpConnections : IProbe
    {
        public static string ProbeName => "AllUdpConnections";

        public string List()
        {
            var sb = new StringBuilder();

            var AF_INET = 2;    // IP_v4
            uint tableBufferSize = 0;
            uint ret = 0;
            var tableBuffer = IntPtr.Zero;
            var rowPtr = IntPtr.Zero;
            MIB_UDPTABLE_OWNER_MODULE ownerModuleTable;
            MIB_UDPROW_OWNER_MODULE[] UdpRows;
            var processes = new Dictionary<string, string>();

            sb.AppendProbeHeaderLine("Active UDP Network Connections");

            try
            {
                // Adapted from https://stackoverflow.com/questions/577433/which-pid-listens-on-a-given-port-in-c-sharp/577660#577660
                // Build a PID -> process name lookup table
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process");
                var retObjectCollection = searcher.Get();

                foreach (ManagementObject process in retObjectCollection)
                {
                    if (process["CommandLine"] != null)
                    {
                        processes.Add(process["ProcessId"].ToString(), process["CommandLine"].ToString());
                    }
                    else
                    {
                        processes.Add(process["ProcessId"].ToString(), process["Name"].ToString());
                    }
                }

                // Figure out how much memory we need for the result struct
                ret = NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref tableBufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret != NativeMethods.ERROR_SUCCESS && ret != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
                {
                    // 122 == insufficient buffer size
                    sb.AppendLine($" [X] Bad check value from GetExtendedUdpTable : {ret}");
                    return sb.ToString();
                }

                tableBuffer = Marshal.AllocHGlobal((int)tableBufferSize);

                ret = NativeMethods.GetExtendedUdpTable(tableBuffer, ref tableBufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
                if (ret != NativeMethods.ERROR_SUCCESS)
                {
                    sb.AppendLine($" [X] Bad return value from GetExtendedUdpTable : {ret}");
                    return sb.ToString();
                }

                // get the number of entries in the table
                ownerModuleTable = (MIB_UDPTABLE_OWNER_MODULE)Marshal.PtrToStructure(tableBuffer, typeof(MIB_UDPTABLE_OWNER_MODULE));
                rowPtr = (IntPtr)(tableBuffer.ToInt64() + Marshal.OffsetOf(typeof(MIB_UDPTABLE_OWNER_MODULE), "Table").ToInt64());
                UdpRows = new MIB_UDPROW_OWNER_MODULE[ownerModuleTable.NumEntries];

                for (var i = 0; i < ownerModuleTable.NumEntries; i++)
                {
                    MIB_UDPROW_OWNER_MODULE udpRow =
                        (MIB_UDPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_MODULE));
                    UdpRows[i] = udpRow;
                    // next entry
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(udpRow));
                }

                sb.AppendLine("  Local Address          PID    Service                 ProcessName");
                foreach (MIB_UDPROW_OWNER_MODULE entry in UdpRows)
                {
                    var processName = "";
                    try
                    {
                        processName = processes[entry.OwningPid.ToString()];
                    }
                    catch { }

                    var serviceName = GetServiceNameFromTag(entry.OwningPid, (uint)entry.OwningModuleInfo0);

                    sb.AppendLine($"  {entry.LocalAddress + ":" + entry.LocalPort,-23}{entry.OwningPid,-7}{serviceName,-23} {processName}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }
            finally
            {
                if (tableBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tableBuffer);
                }
            }

            return sb.ToString();
        }

        // helper that gets a service name from a service tag
        private static string GetServiceNameFromTag(uint processId, uint serviceTag)
        {
            SC_SERVICE_TAG_QUERY serviceTagQuery = new SC_SERVICE_TAG_QUERY
            {
                ProcessId = processId,
                ServiceTag = serviceTag
            };

            uint res = NativeMethods.I_QueryTagInformation(IntPtr.Zero, ScServiceTagQueryType.ServiceNameFromTagInformation, ref serviceTagQuery);
            if (res == NativeMethods.ERROR_SUCCESS)
            {
                return Marshal.PtrToStringUni(serviceTagQuery.Buffer);
            }
            else
            {
                return string.Empty;
            }
        }

    }
}
