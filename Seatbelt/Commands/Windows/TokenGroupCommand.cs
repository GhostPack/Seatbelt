using System.Collections.Generic;
using System.Security.Principal;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    internal class TokenGroupCommand : CommandBase
    {
        public override string Command => "TokenGroups";
        public override string Description => "The current token's local and domain groups";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public TokenGroupCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            WriteHost("Current Token's Groups\n");

            var wi = WindowsIdentity.GetCurrent();

            foreach (var group in wi.Groups)
            {
                var groupName = "";

                try
                {
                    groupName = group.Translate(typeof(NTAccount)).ToString();
                }
                catch { }

                yield return new TokenGroupsDTO(
                    $"{(SecurityIdentifier)group}",
                    groupName
                );
            }
        }
    }

    internal class TokenGroupsDTO : CommandDTOBase
    {
        public TokenGroupsDTO(string groupSid, string groupName)
        {
            GroupSID = groupSid;
            GroupName = groupName;  
        }

        //public System.Security.Principal.SecurityIdentifier GroupSID { get; set; }
        public string GroupSID { get; set; }
        public string GroupName { get; set; }
    }

    [CommandOutputType(typeof(TokenGroupsDTO))]
    internal class TokenGroupsTextFormatter : TextFormatterBase
    {
        public TokenGroupsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (TokenGroupsDTO)result;

            WriteLine("  {0,-40} {1}", dto.GroupName, dto.GroupSID);
        }
    }
}
