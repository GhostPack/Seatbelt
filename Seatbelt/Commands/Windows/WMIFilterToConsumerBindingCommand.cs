using System.Collections.Generic;
using System.Management;
using System.Security.Principal;

namespace Seatbelt.Commands.Windows
{
    internal class WMIFilterToConsumerBindingCommand : CommandBase
    {
        public override string Command => "WMIFilterBinding";
        public override string Description => "Lists WMI Filter to Consumer Bindings";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO: remote

        public WMIFilterToConsumerBindingCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var BindingrClass = new ManagementClass(@"\\.\ROOT\Subscription:__FilterToConsumerBinding");

            foreach (ManagementObject Binding in BindingrClass.GetInstances())
            {
                var sidBytes = (byte[])Binding["CreatorSID"];
                var CreatorSID = new SecurityIdentifier(sidBytes, 0);

                yield return new WMIFilterToConsumerBindingDTO(
                    Binding["Filter"],
                    Binding["Consumer"],
                    CreatorSID
                );
            }
        }
    }

    internal class WMIFilterToConsumerBindingDTO : CommandDTOBase
    {
        public WMIFilterToConsumerBindingDTO(object consumer, object filter, object creatorSid)
        {
            Consumer = consumer;
            Filter = filter;
            CreatorSID = creatorSid;
        }
        public object Consumer { get; }

        public object Filter { get; }

        public object CreatorSID { get; }
    }
}
