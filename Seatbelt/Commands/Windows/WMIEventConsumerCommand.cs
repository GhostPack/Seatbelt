using System.Collections.Generic;
using System.Management;
using System.Security.Principal;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    internal class WMIEventConsumerCommand : CommandBase
    {
        public override string Command => "WMIEventConsumer";
        public override string Description => "Lists WMI Event Consumers";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO: remote

        public WMIEventConsumerCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // recurse and get members of the '__EventConsumer' SuperClass
            var opt = new EnumerationOptions();
            opt.EnumerateDeep = true;

            var EventConsumerClass = new ManagementClass(@"\\.\ROOT\Subscription:__EventConsumer");
            // https://wutils.com/wmi/root/subscription/commandlineeventconsumer/cs-samples.html

            foreach (ManagementObject EventConsumer in EventConsumerClass.GetInstances(opt))
            {
                var systemprops = EventConsumer.SystemProperties;
                var ConsumerType = $"{systemprops["__CLASS"].Value}";

                var sidBytes = (byte[])EventConsumer["CreatorSID"];
                var creatorSid = new SecurityIdentifier(sidBytes, 0);

                var properties = new Dictionary<string, object>();

                foreach (var prop in EventConsumer.Properties)
                {
                    if(!prop.Name.Equals("CreatorSID"))
                    {
                        properties[prop.Name] = prop.Value;
                    }
                }

                yield return new WMIEventConsumerDTO(
                    $"{EventConsumer["Name"]}",
                    creatorSid,
                    ConsumerType,
                    properties
                );
            }
        }
    }

    internal class WMIEventConsumerDTO : CommandDTOBase
    {
        public WMIEventConsumerDTO(object name, object consumerType, object creatorSid, object properties)
        {
            Name = name;
            ConsumerType = consumerType;
            CreatorSid = creatorSid;
            Properties = properties;    
        }
        public object Name { get; }

        public object ConsumerType { get; }

        public object CreatorSid { get; }

        public object Properties { get; }
    }

    [CommandOutputType(typeof(WMIEventConsumerDTO))]
    internal class WMIEventConsumeFormatter : TextFormatterBase
    {
        public WMIEventConsumeFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (WMIEventConsumerDTO)result;

            WriteLine("  {0,-30}    :   {1}", "Name", dto.Name);
            WriteLine("  {0,-30}    :   {1}", "ConsumerType", dto.ConsumerType);
            WriteLine("  {0,-30}    :   {1}", "CreatorSID", dto.CreatorSid);
            foreach(var kvp in (Dictionary<string, object>)dto.Properties)
            {
                WriteLine("  {0,-30}    :   {1}", kvp.Key, kvp.Value);
            }
        }
    }
}
