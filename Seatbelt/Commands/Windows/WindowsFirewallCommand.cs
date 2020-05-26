using System;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Microsoft.Win32;

namespace Seatbelt.Commands.Windows
{
    enum FirewallAction
    {
        ALLOW = 0,
        BLOCK = 1
    }

    internal class WindowsFirewallCommand : CommandBase
    {
        public override string Command => "WindowsFirewall";
        public override string Description => "Non-standard firewall rules, \"-full\" dumps all (arguments == allow/deny/tcp/udp/in/out/domain/private/public)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public WindowsFirewallCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists local firewall policies and rules
            //      by default, only "deny" result are output unless "-full" is passed

            var directionArgs = new List<string>();
            var protocolsArgs = new List<string>();
            var actionArgs = new List<string>();
            var profileArgs = new List<string>();

            foreach (var arg in args)
            {
                if (arg.ToLower().Equals("allow"))
                {
                    actionArgs.Add("Allow");
                }
                else if (arg.ToLower().Equals("deny") || arg.ToLower().Equals("block"))
                {
                    actionArgs.Add("Block");
                }
                else if (arg.ToLower().Equals("tcp"))
                {
                    protocolsArgs.Add("TCP");
                }
                else if (arg.ToLower().Equals("udp"))
                {
                    protocolsArgs.Add("UDP");
                }
                else if (arg.ToLower().Equals("in"))
                {
                    directionArgs.Add("In");
                }
                else if (arg.ToLower().Equals("out"))
                {
                    directionArgs.Add("Out");
                }
                else if (arg.ToLower().Equals("domain"))
                {
                    profileArgs.Add("Domain");
                }
                else if (arg.ToLower().Equals("private"))
                {
                    profileArgs.Add("Private");
                }
                else if (arg.ToLower().Equals("Public"))
                {
                    profileArgs.Add("Public");
                }
            }

            WriteHost(Runtime.FilterResults ? "Collecting Windows Firewall Non-standard Rules\n\n" : "Collecting all Windows Firewall Rules\n\n");

            // Translates Windows protocol numbers to strings
            var protocols = new Dictionary<string, string>()
            {
                { "0", "HOPOPT" }, { "1", "ICMP" }, { "2", "IGMP" }, { "3", "GGP" }, { "4", "IPv4" }, { "5", "ST" }, { "6", "TCP" }, { "7", "CBT" }, { "8", "EGP" }, { "9", "IGP" }, { "10", "BBN-RCC-MON" }, { "11", "NVP-II" }, { "12", "PUP" }, { "13", "ARGUS" }, { "14", "EMCON" }, { "15", "XNET" }, { "16", "CHAOS" }, { "17", "UDP" }, { "18", "MUX" }, { "19", "DCN-MEAS" }, { "20", "HMP" }, { "21", "PRM" }, { "22", "XNS-IDP" }, { "23", "TRUNK-1" }, { "24", "TRUNK-2" }, { "25", "LEAF-1" }, { "26", "LEAF-2" }, { "27", "RDP" }, { "28", "IRTP" }, { "29", "ISO-TP4" }, { "30", "NETBLT" }, { "31", "MFE-NSP" }, { "32", "MERIT-INP" }, { "33", "DCCP" }, { "34", "3PC" }, { "35", "IDPR" }, { "36", "XTP" }, { "37", "DDP" }, { "38", "IDPR-CMTP" }, { "39", "TP++" }, { "40", "IL" }, { "41", "IPv6" }, { "42", "SDRP" }, { "43", "IPv6-Route" }, { "44", "IPv6-Frag" }, { "45", "IDRP" }, { "46", "RSVP" }, { "47", "GRE" }, { "48", "DSR" }, { "49", "BNA" }, { "50", "ESP" }, { "51", "AH" }, { "52", "I-NLSP" }, { "53", "SWIPE" }, { "54", "NARP" }, { "55", "MOBILE" }, { "56", "TLSP" }, { "57", "SKIP" }, { "58", "IPv6-ICMP" }, { "59", "IPv6-NoNxt" }, { "60", "IPv6-Opts" }, { "61", "any host" }, { "62", "CFTP" }, { "63", "any local" }, { "64", "SAT-EXPAK" }, { "65", "KRYPTOLAN" }, { "66", "RVD" }, { "67", "IPPC" }, { "68", "any distributed file system" }, { "69", "SAT-MON" }, { "70", "VISA" }, { "71", "IPCV" }, { "72", "CPNX" }, { "73", "CPHB" }, { "74", "WSN" }, { "75", "PVP" }, { "76", "BR-SAT-MON" }, { "77", "SUN-ND" }, { "78", "WB-MON" }, { "79", "WB-EXPAK" }, { "80", "ISO-IP" }, { "81", "VMTP" }, { "82", "SECURE-VMTP" }, { "83", "VINES" }, { "84", "TTP" }, { "85", "NSFNET-IGP" }, { "86", "DGP" }, { "87", "TCF" }, { "88", "EIGRP" }, { "89", "OSPFIGP" }, { "90", "Sprite-RPC" }, { "91", "LARP" }, { "92", "MTP" }, { "93", "AX.25" }, { "94", "IPIP" }, { "95", "MICP" }, { "96", "SCC-SP" }, { "97", "ETHERIP" }, { "98", "ENCAP" }, { "99", "any private encryption scheme" }, { "100", "GMTP" }, { "101", "IFMP" }, { "102", "PNNI" }, { "103", "PIM" }, { "104", "ARIS" }, { "105", "SCPS" }, { "106", "QNX" }, { "107", "A/N" }, { "108", "IPComp" }, { "109", "SNP" }, { "110", "Compaq-Peer" }, { "111", "IPX-in-IP" }, { "112", "VRRP" }, { "113", "PGM" }, { "114", "0-hop" }, { "115", "L2TP" }, { "116", "DDX" }, { "117", "IATP" }, { "118", "STP" }, { "119", "SRP" }, { "120", "UTI" }, { "121", "SMP" }, { "122", "SM" }, { "123", "PTP" }, { "124", "ISIS" }, { "125", "FIRE" }, { "126", "CRTP" }, { "127", "CRUDP" }, { "128", "SSCOPMCE" }, { "129", "IPLT" }, { "130", "SPS" }, { "131", "PIPE" }, { "132", "SCTP" }, { "133", "FC" }, { "134", "RSVP-E2E-IGNORE" }, { "135", "Mobility" }, { "136", "UDPLite" }, { "137", "MPLS-in-IP" }, { "138", "manet" }, { "139", "HIP" }, { "140", "Shim6" }, { "141", "WESP" }, { "142", "ROHC" }, { "143", "Unassigned" }, { "253", "Experimentation" }, { "254", "Experimentation" }, { "255", "Reserved" }
            };

            // base locations to search
            //  omitted -  @"SYSTEM\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy"
            string[] ruleLocations = { @"SOFTWARE\Policies\Microsoft\WindowsFirewall", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy" };
            foreach (var ruleLocation in ruleLocations)
            {
                var FirewallRules = ThisRunTime.GetValues(RegistryHive.LocalMachine, String.Format("{0}\\FirewallRules", ruleLocation));
                if (FirewallRules != null)
                {
                    var output = new WindowsFirewallDTO(ruleLocation);

                    var DomainProfileEnabled = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\DomainProfile", ruleLocation), "EnableFirewall");
                    if (DomainProfileEnabled != null)
                    {
                        output.Domain.Present = true;
                        var DomainProfileInboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\DomainProfile", ruleLocation), "DefaultInboundAction");
                        var DomainProfileOutboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\DomainProfile", ruleLocation), "DefaultOutboundAction");
                        var DomainProfileDisableNotifications = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\DomainProfile", ruleLocation), "DisableNotifications");

                        output.Domain.Enabled = DomainProfileEnabled == 1;
                        if (DomainProfileEnabled != null)
                        {
                            if (DomainProfileDisableNotifications != null)
                            {
                                output.Domain.DisableNotifications = DomainProfileDisableNotifications == 1;
                            }
                            if (DomainProfileInboundAction != null)
                            {
                                output.Domain.DefaultInboundAction = (FirewallAction)DomainProfileInboundAction;
                            }
                            if (DomainProfileOutboundAction != null)
                            {
                                output.Domain.DefaultOutboundAction = (FirewallAction)DomainProfileOutboundAction;
                            }
                        }
                    }


                    var PublicProfileEnabled = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PublicProfile", ruleLocation), "EnableFirewall");
                    if (PublicProfileEnabled != null)
                    {
                        output.Public.Present = true;
                        var PublicProfileInboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PublicProfile", ruleLocation), "DefaultInboundAction");
                        var PublicProfileOutboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PublicProfile", ruleLocation), "DefaultOutboundAction");
                        var PublicProfileDisableNotifications = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PublicProfile", ruleLocation), "DisableNotifications");

                        output.Public.Enabled = PublicProfileEnabled == 1;
                        if (PublicProfileDisableNotifications != null)
                        {
                            output.Public.DisableNotifications = PublicProfileDisableNotifications == 1;
                        }
                        if (PublicProfileInboundAction != null)
                        {
                            output.Public.DefaultInboundAction = (FirewallAction)PublicProfileInboundAction;
                        }
                        if (PublicProfileOutboundAction != null)
                        {
                            output.Public.DefaultOutboundAction = (FirewallAction)PublicProfileOutboundAction;
                        }
                    }


                    var PrivateProfileEnabled = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PrivateProfile", ruleLocation), "EnableFirewall");
                    if (PrivateProfileEnabled != null)
                    {
                        output.Private.Present = true;
                        var PrivateProfileInboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PrivateProfile", ruleLocation), "DefaultInboundAction");
                        var PrivateProfileOutboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PrivateProfile", ruleLocation), "DefaultOutboundAction");
                        var PrivateProfileDisableNotifications = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\PrivateProfile", ruleLocation), "DisableNotifications");

                        output.Private.Enabled = PrivateProfileEnabled == 1;
                        if (PrivateProfileDisableNotifications != null)
                        {
                            output.Private.DisableNotifications = PrivateProfileDisableNotifications == 1;
                        }
                        if (PrivateProfileInboundAction != null)
                        {
                            output.Private.DefaultInboundAction = (FirewallAction)PrivateProfileInboundAction;
                        }
                        if (PrivateProfileOutboundAction != null)
                        {
                            output.Private.DefaultOutboundAction = (FirewallAction)PrivateProfileOutboundAction;
                        }
                    }


                    var StandardProfileEnabled = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\StandardProfile", ruleLocation), "EnableFirewall");
                    if (StandardProfileEnabled != null)
                    {
                        output.Standard.Present = true;
                        var StandardProfileInboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\StandardProfile", ruleLocation), "DefaultInboundAction");
                        var StandardProfileOutboundAction = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\StandardProfile", ruleLocation), "DefaultOutboundAction");
                        var StandardProfileDisableNotifications = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, String.Format("{0}\\StandardProfile", ruleLocation), "DisableNotifications");

                        output.Standard.Enabled = StandardProfileEnabled == 1;
                        if (StandardProfileDisableNotifications != null)
                        {
                            output.Standard.DisableNotifications = StandardProfileDisableNotifications == 1;
                        }
                        if (StandardProfileInboundAction != null)
                        {
                            output.Standard.DefaultInboundAction = (FirewallAction)StandardProfileInboundAction;
                        }
                        if (StandardProfileOutboundAction != null)
                        {
                            output.Standard.DefaultOutboundAction = (FirewallAction)StandardProfileOutboundAction;
                        }
                    }

                    foreach (var kvp in FirewallRules)
                    {
                        var rule = new WindowsFirewallRule();

                        var props = ((string)kvp.Value).Split('|');
                        foreach (var prop in props)
                        {
                            var onv = prop.Split('=');
                            // The first argument is the version number, which doesn't have a value. That number should not be parsed
                            if (onv.Length == 1)
                            {
                                continue;
                            }
                            string key = onv[0], value = onv[1];
                            switch (onv[0])
                            {
                                case "Action":
                                    rule.Action = value;
                                    break;
                                case "Active":
                                    break;
                                case "Dir":
                                    rule.Direction = value;
                                    break;
                                case "Protocol":
                                    rule.Protocol = protocols[value];
                                    break;
                                case "Name":
                                    rule.Name = value;
                                    break;
                                case "Desc":
                                    rule.Description = value;
                                    break;
                                case "App":
                                    rule.ApplicationName = value;
                                    break;
                                case "Profile":
                                    rule.Profiles = value;
                                    break;
                                case "RPort":
                                    rule.RemotePorts = value;
                                    break;
                                case "LPort":
                                    rule.LocalPorts = value;
                                    break;
                                case "RA4":
                                    rule.RemoteAddresses = value;
                                    break;
                                case "LA4":
                                    rule.LocalAddresses = value;
                                    break;
                            }
                        }

                        if (
                            !Runtime.FilterResults ||
                            (
                                (
                                    ((actionArgs.Count == 0 && protocolsArgs.Count == 0 && directionArgs.Count == 0 && profileArgs.Count == 0) && !rule.Name.StartsWith("@") && !rule.Name.Equals("Shell Input Application"))) ||
                                    (
                                        (actionArgs.Contains("Allow") && !String.IsNullOrEmpty(rule.Action) && rule.Action.Equals("Allow")) ||
                                        (actionArgs.Contains("Block") && !String.IsNullOrEmpty(rule.Action) && rule.Action.Equals("Block")) ||
                                        (protocolsArgs.Contains("TCP") && (String.IsNullOrEmpty(rule.Protocol) || rule.Protocol.Equals("TCP"))) ||
                                        (protocolsArgs.Contains("UDP") && (String.IsNullOrEmpty(rule.Protocol) || rule.Protocol.Equals("UDP"))) ||
                                        (directionArgs.Contains("In") && (String.IsNullOrEmpty(rule.Direction) || rule.Direction.Equals("In"))) ||
                                        (directionArgs.Contains("Out") && (String.IsNullOrEmpty(rule.Direction) || rule.Direction.Equals("Out"))) ||
                                        (profileArgs.Contains("Domain") && (String.IsNullOrEmpty(rule.Profiles) || rule.Profiles.Equals("Domain"))) ||
                                        (profileArgs.Contains("Private") && (String.IsNullOrEmpty(rule.Profiles) || rule.Profiles.Equals("Private"))) ||
                                        (profileArgs.Contains("Public") && (String.IsNullOrEmpty(rule.Profiles) || rule.Profiles.Equals("Public")))
                                    )
                                )
                            )
                        {
                            output.Rules.Add(rule);
                        }
                    }

                    yield return output;
                }
            }
        }

        internal class WindowsFirewallProfileSettings
        {
            public bool Present { get; set; }
            public bool Enabled { get; set; }
            public FirewallAction DefaultInboundAction { get; set; }
            public FirewallAction DefaultOutboundAction { get; set; }
            public bool DisableNotifications { get; set; }
        }

        internal class WindowsFirewallRule
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string ApplicationName { get; set; } = string.Empty;
            public string Protocol { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Direction { get; set; } = string.Empty;
            public string Profiles { get; set; } = string.Empty;
            public string LocalAddresses { get; set; } = string.Empty;
            public string LocalPorts { get; set; } = string.Empty;
            public string RemoteAddresses { get; set; } = string.Empty;
            public string RemotePorts { get; set; } = string.Empty;
        }

        internal class WindowsFirewallDTO : CommandDTOBase
        {
            public WindowsFirewallDTO(string location)
            {
                Domain = new WindowsFirewallProfileSettings();
                Private = new WindowsFirewallProfileSettings();
                Public = new WindowsFirewallProfileSettings();
                Standard = new WindowsFirewallProfileSettings();
                Rules = new List<WindowsFirewallRule>();
                Location = location;
            }

            public String Location { get; set; }
            public WindowsFirewallProfileSettings Domain { get; set; }
            public WindowsFirewallProfileSettings Private { get; set; }
            public WindowsFirewallProfileSettings Public { get; set; }
            public WindowsFirewallProfileSettings Standard { get; set; }
            public List<WindowsFirewallRule> Rules { get; set; }
        }


        [CommandOutputType(typeof(WindowsFirewallDTO))]
        internal class AuditPolicyTextFormatter : TextFormatterBase
        {
            public AuditPolicyTextFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (WindowsFirewallDTO)result;

                WriteLine("Location                     : {0}\n", dto.Location);

                if (dto.Domain.Present)
                {
                    WriteLine("Domain Profile");
                    WriteLine("    Enabled                  : {0}", dto.Domain.Enabled);
                    WriteLine("    DisableNotifications     : {0}", dto.Domain.DisableNotifications);
                    WriteLine("    DefaultInboundAction     : {0}", dto.Domain.DefaultInboundAction);
                    WriteLine("    DefaultOutboundAction    : {0}\n", dto.Domain.DefaultOutboundAction);
                }

                if (dto.Private.Present)
                {
                    WriteLine("Private Profile");
                    WriteLine("    Enabled                  : {0}", dto.Private.Enabled);
                    WriteLine("    DisableNotifications     : {0}", dto.Private.DisableNotifications);
                    WriteLine("    DefaultInboundAction     : {0}", dto.Private.DefaultInboundAction);
                    WriteLine("    DefaultOutboundAction    : {0}\n", dto.Private.DefaultOutboundAction);
                }

                if (dto.Public.Present)
                {
                    WriteLine("Public Profile");
                    WriteLine("    Enabled                  : {0}", dto.Public.Enabled);
                    WriteLine("    DisableNotifications     : {0}", dto.Public.DisableNotifications);
                    WriteLine("    DefaultInboundAction     : {0}", dto.Public.DefaultInboundAction);
                    WriteLine("    DefaultOutboundAction    : {0}\n", dto.Public.DefaultOutboundAction);
                }

                if (dto.Standard.Present)
                {
                    WriteLine("Standard Profile");
                    WriteLine("    Enabled                  : {0}", dto.Standard.Enabled);
                    WriteLine("    DisableNotifications     : {0}", dto.Standard.DisableNotifications);
                    WriteLine("    DefaultInboundAction     : {0}", dto.Standard.DefaultInboundAction);
                    WriteLine("    DefaultOutboundAction    : {0}\n", dto.Standard.DefaultOutboundAction);
                }

                if (dto.Rules.Count > 0)
                {
                    WriteLine("Rules:\n");

                    foreach (var rule in dto.Rules)
                    {
                        WriteLine("  Name                 : {0}", rule.Name);
                        WriteLine("  Description          : {0}", rule.Description);
                        WriteLine("  ApplicationName      : {0}", rule.ApplicationName);
                        WriteLine("  Protocol             : {0}", rule.Protocol);
                        WriteLine("  Action               : {0}", rule.Action);
                        WriteLine("  Direction            : {0}", rule.Direction);
                        WriteLine("  Profiles             : {0}", rule.Profiles);
                        WriteLine("  Local Addr:Port      : {0}:{1}", rule.LocalAddresses, rule.LocalPorts);
                        WriteLine("  Remote Addr:Port     : {0}:{1}\n", rule.RemoteAddresses, rule.RemotePorts);
                    }
                }
            }
        }
    }
}
