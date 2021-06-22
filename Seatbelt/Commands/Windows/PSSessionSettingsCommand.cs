using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;
using System.Xml;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace Seatbelt.Commands.Windows
{
    class PluginAccess
    {
        public PluginAccess(string principal, string sid, string permission)
        {
            Principal = principal;
            Sid = sid;
            Permission = permission;    
        }
        public string Principal { get; }
        public string Sid { get; }
        public string Permission { get; }
    }

    internal class PSSessionSettingsCommand : CommandBase
    {
        public override string Command => "PSSessionSettings";
        public override string Description => "Enumerates PS Session Settings from the registry";
        public override CommandGroup[] Group => new[] {CommandGroup.System, CommandGroup.Remote};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public PSSessionSettingsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (!SecurityUtil.IsHighIntegrity() && !ThisRunTime.ISRemote())
            {
                WriteError("Unable to collect. Must be an administrator.");
                yield break;
            }

            var plugins = new[] { "Microsoft.PowerShell", "Microsoft.PowerShell.Workflow", "Microsoft.PowerShell32" };
            foreach (var plugin in plugins)
            {
                var config = ThisRunTime.GetStringValue(RegistryHive.LocalMachine,
                    $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WSMAN\\Plugin\\{plugin}", "ConfigXML");

                if(config == null) continue;;

                var access = new List<PluginAccess>();

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(config);
                var security = xmlDoc.GetElementsByTagName("Security");

                if (security.Count <= 0) 
                    continue;

                foreach (XmlAttribute attr in security[0].Attributes)
                {
                    if (attr.Name != "Sddl")
                        continue;

                    var desc = new RawSecurityDescriptor(attr.Value);
                    foreach (QualifiedAce ace in desc.DiscretionaryAcl)
                    {
                        var principal = ace.SecurityIdentifier.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
                        var accessStr = ace.AceQualifier.ToString();

                        access.Add(new PluginAccess(
                            principal,
                            ace.SecurityIdentifier.ToString(),
                            accessStr
                        ));
                    }
                }

                yield return new PSSessionSettingsDTO(
                    plugin,
                    access
                );
            }
        }
    }

    internal class PSSessionSettingsDTO : CommandDTOBase
    {
        public PSSessionSettingsDTO(string plugin, List<PluginAccess> permission)
        {
            Plugin = plugin;
            Permission = permission;    
        }
        public string Plugin { get; }
        public List<PluginAccess> Permission { get; }
    }

    [CommandOutputType(typeof(PSSessionSettingsDTO))]
    internal class PSSessionSettingsFormatter : TextFormatterBase
    {
        public PSSessionSettingsFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (PSSessionSettingsDTO)result;

            WriteLine("  Name : {0}", dto.Plugin);

            foreach (var access in dto.Permission)
            {
                WriteLine("    {0,-35}    {1,-22}", access.Principal, access.Permission);
            }

            WriteLine();
        }
    }
}
