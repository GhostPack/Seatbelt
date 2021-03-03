using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net;
using System.Reflection;
using Seatbelt.Interop;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    class ArpEntry
    {
        public ArpEntry(string ipAddress, string physicalAddress, string entryType)
        {
            IpAddress = ipAddress;
            PhysicalAddress = physicalAddress;
            EntryType = entryType;
        }
        public string IpAddress { get; set; }
        public string PhysicalAddress { get; set; }
        public string EntryType { get; set; }
    }

    internal class ArpTableCommand : CommandBase
    {
        public override string Command => "ARPTable";
        public override string Description => "Lists the current ARP table and adapter information (equivalent to arp -a)";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false; // not possible

        public ArpTableCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // adapted from Fred's code at https://social.technet.microsoft.com/Forums/lync/en-US/e949b8d6-17ad-4afc-88cd-0019a3ac9df9/powershell-alternative-to-arp-a?forum=ITCG

            var adapterIdToInterfaceMap = new SortedDictionary<uint, ArpTableDTO>();

            // build a mapping of index -> interface information
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                BuildIndexToInterfaceMap(networkInterface, adapterIdToInterfaceMap);
            }

            var bytesNeeded = 0;
            var result = Iphlpapi.GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);

            if (result != Win32Error.InsufficientBuffer)
            {
                throw new Exception($"GetIpNetTable: Expected insufficient buffer but got {result}");
            }

            var buffer = Marshal.AllocCoTaskMem(bytesNeeded);

            try
            {
                result = Iphlpapi.GetIpNetTable(buffer, ref bytesNeeded, false);

                if (result != 0)
                {
                    throw new Exception($"GetIpNetTable error: {result}");
                }

                // now we have the buffer, we have to marshal it. We can read the first 4 bytes to get the length of the buffer
                var entries = Marshal.ReadInt32(buffer);

                // increment the memory pointer by the size of the int
                var currentBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(typeof(int)));

                // allocate a list of entries
                var arpEntries = new List<Iphlpapi.MIB_IPNETROW>();

                // cycle through the entries
                for (var index = 0; index < entries; index++)
                {
                    arpEntries.Add((Iphlpapi.MIB_IPNETROW)Marshal.PtrToStructure(
                        new IntPtr(currentBuffer.ToInt64() + (index * Marshal.SizeOf(typeof(Iphlpapi.MIB_IPNETROW)))),
                        typeof(Iphlpapi.MIB_IPNETROW)));
                }

                // sort the list by interface index
                var sortedArpEntries = arpEntries.OrderBy(o => o.dwIndex).ToList();
                uint? currentAdapterIndex = null;

                foreach (var arpEntry in sortedArpEntries)
                {
                    var adapterIndex = (uint)arpEntry.dwIndex;

                    if (currentAdapterIndex != adapterIndex)
                    {
                        if (!adapterIdToInterfaceMap.ContainsKey(adapterIndex))
                        {
                            adapterIdToInterfaceMap[adapterIndex] = new ArpTableDTO(adapterIndex, "n/a", "n/a");
                        }

                        currentAdapterIndex = adapterIndex;
                    }

                    var ipAddress = new IPAddress(BitConverter.GetBytes(arpEntry.dwAddr));
                    var macBytes = new[]
                        {arpEntry.mac0, arpEntry.mac1, arpEntry.mac2, arpEntry.mac3, arpEntry.mac4, arpEntry.mac5};
                    var physicalAddress = BitConverter.ToString(macBytes);
                    var entryType = (Iphlpapi.ArpEntryType)arpEntry.dwType;

                    var entry = new ArpEntry(
                        ipAddress.ToString(),
                        physicalAddress,
                        entryType.ToString()
                    );

                    adapterIdToInterfaceMap[adapterIndex].Entries.Add(entry);
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero) Iphlpapi.FreeMibTable(buffer);
            }


            foreach (var adapter in adapterIdToInterfaceMap)
            {
                yield return adapter.Value;
            }
        }

        private static void BuildIndexToInterfaceMap(NetworkInterface ni, SortedDictionary<uint, ArpTableDTO> adapters)
        {
            // We don't care about the loopback interface
            // if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) return;
            
            var adapterProperties = ni.GetIPProperties();
            if (adapterProperties == null) throw new Exception("Could not get adapter IP properties");

            var index = (uint?)ni.GetType().GetField("index", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ni);
            if(index == null) throw new Exception("Could not get interface index number");

            var adapter = new ArpTableDTO(index.Value, ni.Name, ni.Description);

            adapterProperties.UnicastAddresses
                .ToList()
                .ForEach(ip => adapter.InterfaceIPs.Add(ip.Address.ToString()));

            adapterProperties.DnsAddresses
                .ToList()
                .ForEach(dns => adapter.DnsServers.Add(dns.ToString()));

            adapters.Add(index.Value, adapter);
        }
    }

    internal class ArpTableDTO : CommandDTOBase
    {
        public ArpTableDTO(uint index, string name, string description)
        {
            InterfaceIndex = index;
            InterfaceName = name;
            InterfaceDescription = description;
        }

        public uint InterfaceIndex { get; }

        public string InterfaceName { get; }
        public string InterfaceDescription { get; }
        public List<string> InterfaceIPs { get; set; } = new List<string>();
        public List<string> DnsServers { get; set; } = new List<string>();
        public List<ArpEntry> Entries { get; set; } = new List<ArpEntry>();
    }

    [CommandOutputType(typeof(ArpTableDTO))]
    internal class ArpTableTextFormatter : TextFormatterBase
    {
        public ArpTableTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ArpTableDTO)result;

            WriteLine($"  {dto.InterfaceName} --- Index {dto.InterfaceIndex}");
            WriteLine($"    Interface Description : {dto.InterfaceDescription}");
            WriteLine($"    Interface IPs      : {string.Join(", ", dto.InterfaceIPs.ToArray())}");

            if (dto.DnsServers.Count > 0)
            {
                WriteLine($"    DNS Servers        : {string.Join(", ", dto.DnsServers.ToArray())}\n");
            }

            WriteLine("    Internet Address      Physical Address      Type");

            foreach (var entry in dto.Entries)
            {
                WriteLine($"    {entry.IpAddress,-22}{entry.PhysicalAddress,-22}{entry.EntryType}");
            }

            WriteLine("\n");
        }
    }
}
