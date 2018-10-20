using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class ARPTable :IProbe
    {
        public static string ProbeName => "ARPTable";

        public string List()
        {
            var sb = new StringBuilder();

            // adapted from Fred's code at https://social.technet.microsoft.com/Forums/lync/en-US/e949b8d6-17ad-4afc-88cd-0019a3ac9df9/powershell-alternative-to-arp-a?forum=ITCG

            sb.AppendProbeHeaderLine("Current ARP Table");

            try
            {
                var adapters = new Dictionary<int, string>();
                var hostNames = new Dictionary<string, string>();

                // build a mapping of index -> interface information
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni != null)
                    {
                        var adapterProperties = ni.GetIPProperties();
                        if (adapterProperties != null)
                        {
                            var dnsServers = "";
                            var dnsServerList = new List<string>();
                            var dnsServerCollection = adapterProperties.DnsAddresses;
                            if (dnsServerCollection.Count > 0)
                            {
                                foreach (var dns in dnsServerCollection)
                                {
                                    dnsServerList.Add(dns.ToString());
                                }
                                dnsServers = String.Join(", ", dnsServerList.ToArray());
                            }

                            try
                            {
                                var p = adapterProperties.GetIPv4Properties();
                                if (p != null)
                                {
                                    var ips = new ArrayList();

                                    foreach (var info in adapterProperties.UnicastAddresses)
                                    {
                                        if (Regex.IsMatch(info.Address.ToString(), @"^(\d+)\.(\d+)\.(\d+)\.(\d+)$"))
                                        {
                                            // grab all the IPv4 addresses
                                            ips.Add(info.Address.ToString());
                                        }
                                    }

                                    // build a "Ethernet1 (172.16.213.246) --- Index 8" type string for the index
                                    var description = String.Format("{0} ({1}) --- Index {2}", ni.Name,
                                        string.Join(",", (string[])ips.ToArray(Type.GetType("System.String"))),
                                        p.Index);
                                    if (!String.IsNullOrEmpty(dnsServers))
                                    {
                                        description += String.Format("\r\n    DNS Servers : {0}\r\n", dnsServers);
                                    }

                                    adapters.Add(p.Index, description);
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(ex.Message);
                            }
                        }
                    }
                }

                var bytesNeeded = 0;

                int result = NativeMethods.GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);

                // call the function, expecting an insufficient buffer.
                if (result != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
                {
                    sb.AppendLine($"  [X] Exception: {result}");
                }


                // allocate sufficient memory for the result structure
                IntPtr buffer = Marshal.AllocCoTaskMem(bytesNeeded);

                result = NativeMethods.GetIpNetTable(buffer, ref bytesNeeded, false);

                if (result != 0)
                {
                    sb.AppendLine($"  [X] Exception allocating buffer: {result}");
                }

                // now we have the buffer, we have to marshal it. We can read the first 4 bytes to get the length of the buffer
                var entries = Marshal.ReadInt32(buffer);

                // increment the memory pointer by the size of the int
                var currentBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(typeof(int)));

                // allocate a list of entries
                List<MIB_IPNETROW> arpEntries = new List<MIB_IPNETROW>();

                // cycle through the entries
                for (var index = 0; index < entries; index++)
                {
                    arpEntries.Add((MIB_IPNETROW)Marshal.PtrToStructure(new IntPtr(currentBuffer.ToInt64() + (index * Marshal.SizeOf(typeof(MIB_IPNETROW)))), typeof(MIB_IPNETROW)));
                }

                // sort the list by interface index
                List<MIB_IPNETROW> sortedARPEntries = arpEntries.OrderBy(o => o.dwIndex).ToList();
                var currentIndexAdaper = -1;

                foreach (MIB_IPNETROW arpEntry in sortedARPEntries)
                {
                    int indexAdapter = arpEntry.dwIndex;

                    if (currentIndexAdaper != indexAdapter)
                    {
                        if (adapters.ContainsKey(indexAdapter))
                        {
                            sb.AppendLine()
                                .AppendLine($"  Interface     : {adapters[indexAdapter]}");
                        }
                        else
                        {
                            sb.AppendLine()
                                .AppendLine($"  Interface     : n/a --- Index {indexAdapter}");
                        }
                        sb.AppendLine("    Internet Address      Physical Address      Type");
                        currentIndexAdaper = indexAdapter;
                    }

                    var ipAddr = new IPAddress(BitConverter.GetBytes(arpEntry.dwAddr));
                    var macBytes = new byte[] { arpEntry.mac0, arpEntry.mac1, arpEntry.mac2, arpEntry.mac3, arpEntry.mac4, arpEntry.mac5 };
                    var physAddr = BitConverter.ToString(macBytes);
                    var entryType = (ArpEntryType)arpEntry.dwType;

                    sb.AppendLine($"    {ipAddr,-22}{physAddr,-22}{entryType}");
                }

                NativeMethods.FreeMibTable(buffer);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

    }
}
