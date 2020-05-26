#nullable disable
using Seatbelt.Interop;
using static Seatbelt.Interop.Iphlpapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    class ARPEntry
    {
        public ARPEntry(string ipAddress, string physicalAddress, string entryType)
        {
            IPAddress = ipAddress;
            PhysicalAddress = physicalAddress;
            EntryType = entryType;
        }
        public string IPAddress { get; set; }
        public string PhysicalAddress { get; set; }
        public string EntryType { get; set; }
    }

    internal class ARPTableCommand : CommandBase
    {
        public override string Command => "ARPTable";
        public override string Description => "Lists the current ARP table and adapter information (equivalent to arp -a)";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false;

        public ARPTableCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // adapted from Fred's code at https://social.technet.microsoft.com/Forums/lync/en-US/e949b8d6-17ad-4afc-88cd-0019a3ac9df9/powershell-alternative-to-arp-a?forum=ITCG

            var adapters = new SortedDictionary<int, ARPTableDTO>();

            // build a mapping of index -> interface information
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var adapter = new ARPTableDTO();

                var adapterProperties = ni?.GetIPProperties();
                if (adapterProperties == null)
                {
                    continue;
                }
                var dnsServerList = new List<string>();
                var dnsServerCollection = adapterProperties.DnsAddresses;

                if (dnsServerCollection.Count > 0)
                {
                    foreach (var dns in dnsServerCollection)
                    {
                        dnsServerList.Add(dns.ToString());
                    }
                }

                adapter.DNSServers = dnsServerList;

                try
                {
                    var p = adapterProperties.GetIPv4Properties();
                    if (p == null)
                    {
                        continue;
                    }

                    var ips = new List<string>();

                    foreach (var info in adapterProperties.UnicastAddresses)
                    {
                        if (Regex.IsMatch(info.Address.ToString(), @"^(\d+)\.(\d+)\.(\d+)\.(\d+)$"))
                        {
                            // grab all the IPv4 addresses
                            ips.Add(info.Address.ToString());
                        }
                    }

                    adapter.InterfaceIPs = ips;
                    adapter.InterfaceName = ni.Name;
                    adapter.InterfaceIndex = p.Index;

                    adapters.Add(p.Index, adapter);
                }
                catch
                {
                    // ignored
                }
            }

            var bytesNeeded = 0;

            var result = GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);

            // call the function, expecting an insufficient buffer.
            if (result != Win32Error.InsufficientBuffer)
            {
                WriteError($"GetIpNetTable: Expected insufficent buffer but got {result}");
            }

            // allocate sufficient memory for the result structure
            var buffer = Marshal.AllocCoTaskMem(bytesNeeded);

            result = GetIpNetTable(buffer, ref bytesNeeded, false);

            if (result != 0)
            {
                WriteError($"GetIpNetTable: {result}");
            }

            // now we have the buffer, we have to marshal it. We can read the first 4 bytes to get the length of the buffer
            var entries = Marshal.ReadInt32(buffer);

            // increment the memory pointer by the size of the int
            var currentBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(typeof(int)));

            // allocate a list of entries
            var arpEntries = new List<MIB_IPNETROW>();

            // cycle through the entries
            for (var index = 0; index < entries; index++)
            {
                arpEntries.Add((MIB_IPNETROW)Marshal.PtrToStructure(new IntPtr(currentBuffer.ToInt64() + (index * Marshal.SizeOf(typeof(MIB_IPNETROW)))), typeof(MIB_IPNETROW)));
            }

            // sort the list by interface index
            var sortedArpEntries = arpEntries.OrderBy(o => o.dwIndex).ToList();
            var currentIndexAdapter = -1;

            foreach (var arpEntry in sortedArpEntries)
            {
                var indexAdapter = arpEntry.dwIndex;

                if (currentIndexAdapter != indexAdapter)
                {
                    if (!adapters.ContainsKey(indexAdapter))
                    {
                        adapters[indexAdapter] = new ARPTableDTO();
                        adapters[indexAdapter].InterfaceIndex = indexAdapter;
                        adapters[indexAdapter].InterfaceName = "n/a";
                    }

                    currentIndexAdapter = indexAdapter;
                }

                var ipAddress = new IPAddress(BitConverter.GetBytes(arpEntry.dwAddr));
                var macBytes = new byte[] { arpEntry.mac0, arpEntry.mac1, arpEntry.mac2, arpEntry.mac3, arpEntry.mac4, arpEntry.mac5 };
                var physicalAddress = BitConverter.ToString(macBytes);
                var entryType = (ArpEntryType)arpEntry.dwType;

                if (adapters[indexAdapter].Entries == null)
                {
                    adapters[indexAdapter].Entries = new List<ARPEntry>();
                }

                var entry = new ARPEntry(
                    ipAddress.ToString(),
                    physicalAddress,
                    entryType.ToString()
                );

                adapters[indexAdapter].Entries.Add(entry);
            }

            FreeMibTable(buffer);

            foreach (var adapter in adapters)
            {
                yield return adapter.Value;
            }
        }
    }

    internal class ARPTableDTO : CommandDTOBase
    {
        public string InterfaceName { get; set; }

        public int InterfaceIndex { get; set; }

        public List<string> InterfaceIPs { get; set; }

        public List<string> DNSServers { get; set; }

        public List<ARPEntry> Entries { get; set; }
    }

    [CommandOutputType(typeof(ARPTableDTO))]
    internal class ARPTableTextFormatter : TextFormatterBase
    {
        public ARPTableTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ARPTableDTO)result;

            if (dto.InterfaceIPs != null)
            {
                WriteLine("\n\n  Interface            :  {0} ({1}) --- Index {2}", dto.InterfaceName, string.Join(",", (string[])dto.InterfaceIPs.ToArray()), dto.InterfaceIndex);
            }
            else
            {
                WriteLine("\n\n  Interface            :  {0} ({1}) --- Index {2}", dto.InterfaceName, "n/a", dto.InterfaceIndex);
            }

            if (dto.DNSServers != null)
            {
                WriteLine("    DNS Servers        :  {0}\n", string.Join(",", (string[])dto.DNSServers.ToArray()));
            }

            WriteLine("    Internet Address      Physical Address      Type");

            foreach (var entry in dto.Entries)
            {
                WriteLine($"    {entry.IPAddress,-22}{entry.PhysicalAddress,-22}{entry.EntryType}");
            }
        }
    }
}
#nullable enable