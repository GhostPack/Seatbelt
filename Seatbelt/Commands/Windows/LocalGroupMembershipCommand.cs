using System;
using System.Collections.Generic;
using System.Linq;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using static Seatbelt.Interop.Netapi32;


namespace Seatbelt.Commands.Windows
{
    internal class LocalGroupMembershipCommand : CommandBase
    {
        public override string Command => "LocalGroups";
        public override string Description => "Non-empty local groups, \"-full\" displays all groups (argument == computername to enumerate)";
        public override CommandGroup[] Group => new[] {CommandGroup.System, CommandGroup.Remote};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public LocalGroupMembershipCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // takes an optional remote computername as an argument, otherwise defaults to the localhost

            string? computerName = null;
            if(!String.IsNullOrEmpty(ThisRunTime.ComputerName))
            {
                computerName = ThisRunTime.ComputerName;
            }
            else if (args.Length >= 1)
            {
                computerName = args[0];
            }

            // adapted from https://stackoverflow.com/questions/33935825/pinvoke-netlocalgroupgetmembers-runs-into-fatalexecutionengineerror/33939889#33939889

            WriteHost(Runtime.FilterResults ? "Non-empty Local Groups (and memberships)\n\n" : "All Local Groups (and memberships)\n\n");

            foreach(var group in GetLocalGroups(computerName))
            {
                var members = GetLocalGroupMembers(computerName, group.name)?.ToList();
                if (members != null && members.Any())
                {       
                    yield return new LocalGroupMembershipDTO(
                        computerName ?? Environment.MachineName,
                        group.name,
                        group.comment,
                        members
                    );
                }
            }
        }
    }

    internal class LocalGroupMembershipDTO : CommandDTOBase
    {
        public LocalGroupMembershipDTO(string computerName, string groupName, string groupComment, IEnumerable<Principal> members)
        {
            ComputerName = computerName;
            GroupName = groupName;
            GroupComment = groupComment;
            Members = members;
        }

        public string ComputerName { get; }
        public string GroupName { get; }
        public string GroupComment { get; }
        public IEnumerable<Principal> Members { get; }
    }

    [CommandOutputType(typeof(LocalGroupMembershipDTO))]
    internal class LocalGroupMembershipTextFormatter : TextFormatterBase
    {
        public LocalGroupMembershipTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {

            if (result == null)
            {
                return;
            }

            var dto = (LocalGroupMembershipDTO)result;
            WriteLine("  ** {0}\\{1} ** ({2})\n", dto.ComputerName, dto.GroupName, dto.GroupComment);

            if (dto.Members != null)
            {
                foreach (var member in dto.Members)
                {
                    WriteLine($"  {member.Class,-15} {member.Domain + "\\" + member.User,-40} {member.Sid}");
                }
            }

            WriteLine();
        }
    }
}
