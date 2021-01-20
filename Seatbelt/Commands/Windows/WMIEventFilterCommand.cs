using System.Collections.Generic;
using System.Management;
using System.Security.Principal;

namespace Seatbelt.Commands.Windows
{
    internal class WmiEventFilterCommand : CommandBase
    {
        public override string Command => "WMIEventFilter";
        public override string Description => "Lists WMI Event Filters";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO: remote

        public WmiEventFilterCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var EventFilterClass = new ManagementClass(@"\\.\ROOT\Subscription:__EventFilter");

            foreach (ManagementObject EventFilter in EventFilterClass.GetInstances())
            {
                var sidBytes = (byte[])EventFilter["CreatorSID"];
                var creatorSid = new SecurityIdentifier(sidBytes, 0);

                yield return new WmiEventFilterDTO(
                    EventFilter["Name"],
                    EventFilter["__NAMESPACE"],
                    EventFilter["EventNamespace"],
                    EventFilter["Query"],
                    EventFilter["QueryLanguage"],
                    EventFilter["EventAccess"],
                    creatorSid
                );
            }

        }
    }

    internal class WmiEventFilterDTO : CommandDTOBase
    {
        public WmiEventFilterDTO(object name, object ns, object eventNamespace, object query, object queryLanguage, object eventAccess, object creatorSid)
        {
            Name = name;
            Namespace = ns;
            EventNamespace = eventNamespace;
            Query = query;
            QueryLanguage = queryLanguage;
            EventAccess = eventAccess;
            CreatorSid = creatorSid;    
        }
        public object Name { get; }
        public object Namespace { get; }
        public object EventNamespace { get; }
        public object Query { get; }
        public object QueryLanguage { get; }
        public object EventAccess { get; }
        public object CreatorSid { get; }
    }
}