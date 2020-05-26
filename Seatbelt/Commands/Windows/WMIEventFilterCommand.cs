#nullable disable
using System.Collections.Generic;
using System.Management;
using System.Security.Principal;

namespace Seatbelt.Commands.Windows
{
    internal class WMIEventFilterCommand : CommandBase
    {
        public override string Command => "WMIEventFilter";
        public override string Description => "Lists WMI Event Filters";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO: remote

        public WMIEventFilterCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var EventFilterClass = new ManagementClass(@"\\.\ROOT\Subscription:__EventFilter");

            foreach (ManagementObject EventFilter in EventFilterClass.GetInstances())
            {
                var sidBytes = (byte[])EventFilter["CreatorSID"];
                var CreatorSID = new SecurityIdentifier(sidBytes, 0);

                yield return new WMIEventFilterDTO()
                {
                    Name = EventFilter["Name"],
                    Namespace = EventFilter["__NAMESPACE"],
                    EventNamespace = EventFilter["EventNamespace"],
                    Query = EventFilter["Query"],
                    QueryLanguage = EventFilter["QueryLanguage"],
                    EventAccess = EventFilter["EventAccess"],
                    CreatorSID = CreatorSID
                };
            }

        }
    }

    internal class WMIEventFilterDTO : CommandDTOBase
    {
        public object Name { get; set; }
        public object Namespace { get; set; }
        public object EventNamespace { get; set; }
        public object Query { get; set; }
        public object QueryLanguage { get; set; }
        public object EventAccess { get; set; }
        public object CreatorSID { get; set; }
    }
}
#nullable enable