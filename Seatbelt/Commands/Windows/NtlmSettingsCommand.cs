#nullable disable
using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;
using System;
using System.Collections.Generic;


namespace Seatbelt.Commands.Windows
{
    internal class NtlmSettingsCommand : CommandBase
    {
        public override string Command => "NTLMSettings";
        public override string Description => "NTLM authentication settings";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public NtlmSettingsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            yield return new NtlmSettingsDTO()
            {
                LanmanCompatibilityLevel = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Lsa", "LmCompatibilityLevel"),

                ClientRequireSigning = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "RequireSecuritySignature") == 1,
                ClientNegotiateSigning = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "EnableSecuritySignature") == 1,
                ServerRequireSigning = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanManServer\Parameters", "RequireSecuritySignature") == 1,
                ServerNegotiateSigning = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanManServer\Parameters", "EnableSecuritySignature") == 1,

                //ExtendedProtectionForAuthentication = RegistryUtil.GetValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\LSA", "SuppressExtendedProtection"),

                LdapSigning = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LDAP", "LDAPClientIntegrity"),
                //DCLdapSigning = RegistryUtil.GetValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\NTDS\Parameters", "LDAPServerIntegrity"),
                //LdapChannelBinding = RegistryUtil.GetValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\NTDS\Parameters", "LdapEnforceChannelBinding"),

                NTLMMinClientSec = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0", "NtlmMinClientSec"),
                NTLMMinServerSec = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0", "NtlmMinServerSec"),

                //DCRestrictions = RegistryUtil.GetValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\Netlogon\Parameters", "RestrictNTLMInDomain"),    // Network security: Restrict NTLM:  NTLM authentication in this domain
                //DCExceptions = RegistryUtil.GetValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\Netlogon\Parameters", "DCAllowedNTLMServers"),      // Network security: Restrict NTLM: Add server exceptions in this domain
                //DCAuditing = RegistryUtil.GetValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\Netlogon\Parameters", "AuditNTLMInDomain"),           // Network security: Restrict NTLM: Audit NTLM authentication in this domain

                InboundRestrictions = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Lsa\MSV1_0", "RestrictReceivingNTLMTraffic"), // Network security: Restrict NTLM: Incoming NTLM traffic
                OutboundRestrictions = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Lsa\MSV1_0", "RestrictSendingNTLMTraffic"),  // Network security: Restrict NTLM: Outgoing NTLM traffic to remote servers
                InboundAuditing = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Lsa\MSV1_0", "AuditReceivingNTLMTraffic"),        // Network security: Restrict NTLM: Audit Incoming NTLM Traffic
                OutboundExceptions = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Lsa\MSV1_0", "ClientAllowedNTLMServers"),      // Network security: Restrict NTLM: Add remote server exceptions for NTLM authentication

            };
        }
    }

    [Flags]
    enum SessionSecurity : uint
    {
        None = 0x00000000,
        Integrity = 0x00000010, // Message integrity
        Confidentiality = 0x00000020, // Message confidentiality
        NTLMv2 = 0x00080000,
        Require128BitKey = 0x20000000,
        Require56BitKey = 0x80000000
    }

    internal class NtlmSettingsDTO : CommandDTOBase
    {
        public uint? LanmanCompatibilityLevel { get; set; }

        public bool ClientRequireSigning { get; set; }
        public bool ClientNegotiateSigning { get; set; }
        public bool ServerRequireSigning { get; set; }
        public bool ServerNegotiateSigning { get; set; }

        //public string ExtendedProtectionForAuthentication { get; set; }

        public uint? LdapSigning { get; set; }
        //public string LdapChannelBinding { get; set; }

        public uint? NTLMMinClientSec { get; set; }
        public uint? NTLMMinServerSec { get; set; }

        //public string DCRestrictions { get; internal set; }
        //public string DCExceptions { get; internal set; }
        //public string DCAuditing { get; internal set; }

        public uint? InboundRestrictions { get; internal set; }
        public uint? OutboundRestrictions { get; internal set; }
        public uint? InboundAuditing { get; internal set; }
        public string OutboundExceptions { get; internal set; }
    }

    [CommandOutputType(typeof(NtlmSettingsDTO))]
    internal class NtlmSettingsTextFormatter : TextFormatterBase
    {
        public NtlmSettingsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (NtlmSettingsDTO)result;

            string lmStr = null;
            switch (dto.LanmanCompatibilityLevel)
            {
                case 0: lmStr = "Send LM & NTLM responses"; break;
                case 1: lmStr = "Send LM & NTLM - Use NTLMv2 session security if negotiated"; break;
                case 2: lmStr = "Send NTLM response only"; break;
                case null:
                case 3:
                    lmStr = "Send NTLMv2 response only - Win7+ default"; break;
                case 4: lmStr = "Send NTLMv2 response only. DC: Refuse LM"; break;
                case 5: lmStr = "Send NTLMv2 response only. DC: Refuse LM & NTLM"; break;
                default: lmStr = "Unknown"; break;
            }
            WriteLine("  LanmanCompatibilityLevel    : {0}({1})", dto.LanmanCompatibilityLevel, lmStr);



            WriteLine("\n  NTLM Signing Settings");
            WriteLine("      ClientRequireSigning    : {0}", dto.ClientRequireSigning);
            WriteLine("      ClientNegotiateSigning  : {0}", dto.ClientNegotiateSigning);
            WriteLine("      ServerRequireSigning    : {0}", dto.ServerRequireSigning);
            WriteLine("      ServerNegotiateSigning  : {0}", dto.ServerNegotiateSigning);


            string ldapSigningStr;
            switch (dto.LdapSigning)
            {
                case 0: ldapSigningStr = "No signing"; break;
                case 1: case null: ldapSigningStr = "Negotiate signing"; break;
                case 2: ldapSigningStr = "Require Signing"; break;
                default: ldapSigningStr = "Unknown"; break;
            }
            WriteLine("      LdapSigning             : {0} ({1})", dto.LdapSigning, ldapSigningStr);


            WriteLine("\n  Session Security");

            if (dto.NTLMMinClientSec != null)
            {
                var clientSessionSecurity = (SessionSecurity)dto.NTLMMinClientSec;
                WriteLine("      NTLMMinClientSec        : {0} ({1})", dto.NTLMMinClientSec, clientSessionSecurity);

                if (dto.LanmanCompatibilityLevel < 3 && !clientSessionSecurity.HasFlag(SessionSecurity.NTLMv2))
                {
                    WriteLine("        [!] NTLM clients support NTLMv1!");
                }
            }

            if (dto.NTLMMinServerSec != null)
            {
                var serverSessionSecurity = (SessionSecurity)dto.NTLMMinServerSec;
                WriteLine("      NTLMMinServerSec        : {0} ({1})\n", dto.NTLMMinServerSec, serverSessionSecurity);

                if (dto.LanmanCompatibilityLevel < 3 && !serverSessionSecurity.HasFlag(SessionSecurity.NTLMv2))
                {
                    WriteLine("        [!] NTLM services on this machine support NTLMv1!");
                }
            }

            string inboundRestrictStr;
            switch (dto.InboundRestrictions)
            {
                case 0: inboundRestrictStr = "Allow all"; break;
                case 1: inboundRestrictStr = "Deny all domain accounts"; break;
                case 2: inboundRestrictStr = "Deny all accounts"; break;
                default: inboundRestrictStr = "Not defined"; break;
            }

            string outboundRestrictStr;
            switch (dto.OutboundRestrictions)
            {
                case 0: outboundRestrictStr = "Allow all"; break;
                case 1: outboundRestrictStr = "Audit all"; break;
                case 2: outboundRestrictStr = "Deny all"; break;
                default: outboundRestrictStr = "Not defined"; break;
            }

            string inboundAuditStr;
            switch (dto.InboundAuditing)
            {
                case 0: inboundAuditStr = "Disable"; break;
                case 1: inboundAuditStr = "Enable auditing for domain accounts"; break;
                case 2: inboundAuditStr = "Enable auditing for all accounts"; break;
                default: inboundAuditStr = "Not defined"; break;
            }

            WriteLine("\n  NTLM Auditing and Restrictions");
            WriteLine("      InboundRestrictions     : {0}({1})", dto.InboundRestrictions, inboundRestrictStr);
            WriteLine("      OutboundRestrictions    : {0}({1})", dto.OutboundRestrictions, outboundRestrictStr);
            WriteLine("      InboundAuditing         : {0}({1})", dto.InboundAuditing, inboundAuditStr);
            WriteLine("      OutboundExceptions      : {0}", dto.OutboundExceptions);
        }
    }
}
#nullable enable