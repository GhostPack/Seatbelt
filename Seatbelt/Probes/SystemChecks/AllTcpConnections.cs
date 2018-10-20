using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class AllTcpConnections : IProbe
    {

        public static string ProbeName => "AllTcpConnections";

        public string List()
        {
            var sb = new StringBuilder();

            var AF_INET = 2;    // IP_v4
            uint tableBufferSize = 0;
            uint ret = 0;
            var tableBuffer = IntPtr.Zero;
            var rowPtr = IntPtr.Zero;
            MIB_TCPTABLE_OWNER_MODULE ownerModuleTable;
            MIB_TCPROW_OWNER_MODULE[] tcpRows;
            var processes = new Dictionary<string, string>();

            sb.AppendProbeHeaderLine("Active TCP Network Connections");

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
                ret = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref tableBufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret != NativeMethods.ERROR_SUCCESS && ret != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
                {
                    // 122 == insufficient buffer size
                    sb.AppendLine($" [X] Bad check value from GetExtendedTcpTable : {ret}");
                    return sb.ToString();
                }

                tableBuffer = Marshal.AllocHGlobal((int)tableBufferSize);

                ret = NativeMethods.GetExtendedTcpTable(tableBuffer, ref tableBufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
                if (ret != NativeMethods.ERROR_SUCCESS)
                {
                    sb.AppendLine($" [X] Bad return value from GetExtendedTcpTable : {ret}");
                    return sb.ToString();
                }

                // get the number of entries in the table
                ownerModuleTable = (MIB_TCPTABLE_OWNER_MODULE)Marshal.PtrToStructure(tableBuffer, typeof(MIB_TCPTABLE_OWNER_MODULE));
                rowPtr = (IntPtr)(tableBuffer.ToInt64() + Marshal.OffsetOf(typeof(MIB_TCPTABLE_OWNER_MODULE), "Table").ToInt64());
                tcpRows = new MIB_TCPROW_OWNER_MODULE[ownerModuleTable.NumEntries];

                for (var i = 0; i < ownerModuleTable.NumEntries; i++)
                {
                    MIB_TCPROW_OWNER_MODULE tcpRow = (MIB_TCPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_MODULE));
                    tcpRows[i] = tcpRow;
                    // next entry
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));
                }

                sb.AppendLine("  Local Address          Foreign Address        State      PID   Service         ProcessName");
                foreach (MIB_TCPROW_OWNER_MODULE entry in tcpRows)
                {
                    var processName = "";
                    try
                    {
                        processName = processes[entry.OwningPid.ToString()];
                    }
                    catch { }

                    var serviceName = GetServiceNameFromTag(entry.OwningPid, (uint)entry.OwningModuleInfo0);

                    sb.AppendLine(
                        $"  {entry.LocalAddress + ":" + entry.LocalPort,-23}{entry.RemoteAddress + ":" + entry.RemotePort,-23}{entry.State,-11}{entry.OwningPid,-6}{serviceName,-15} {processName}");
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
