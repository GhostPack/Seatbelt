#nullable disable
using Seatbelt.Output.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using Seatbelt.Output.TextWriters;
using System.Text.RegularExpressions;
using static Seatbelt.Interop.Netapi32;
using Seatbelt.Util;


namespace Seatbelt.Commands.Windows
{
    internal class UserRightAssignmentsCommand : CommandBase
    {
        public override string Command => "UserRightAssignments";
        public override string Description => "Configured User Right Assignments (e.g. SeDenyNetworkLogonRight, SeShutdownPrivilege, etc.) argument == computername to enumerate";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false;


        private readonly string[] _allPrivileges = new[]
{
            "SeAssignPrimaryTokenPrivilege",
            "SeAuditPrivilege",
            "SeBackupPrivilege",
            "SeBatchLogonRight",
            "SeChangeNotifyPrivilege",
            "SeCreateGlobalPrivilege",
            "SeCreatePagefilePrivilege",
            "SeCreatePermanentPrivilege",
            "SeCreateSymbolicLinkPrivilege",
            "SeCreateTokenPrivilege",
            "SeDebugPrivilege",
            "SeDenyBatchLogonRight",
            "SeDenyInteractiveLogonRight",
            "SeDenyNetworkLogonRight",
            "SeDenyRemoteInteractiveLogonRight",
            "SeDenyServiceLogonRight",
            "SeEnableDelegationPrivilege",
            "SeImpersonatePrivilege",
            "SeIncreaseBasePriorityPrivilege",
            "SeIncreaseQuotaPrivilege",
            "SeIncreaseWorkingSetPrivilege",
            "SeInteractiveLogonRight",
            "SeLoadDriverPrivilege",
            "SeLockMemoryPrivilege",
            "SeMachineAccountPrivilege",
            "SeManageVolumePrivilege",
            "SeNetworkLogonRight",
            "SeProfileSingleProcessPrivilege",
            "SeRelabelPrivilege",
            "SeRemoteInteractiveLogonRight",
            "SeRemoteShutdownPrivilege",
            "SeRestorePrivilege",
            "SeSecurityPrivilege",
            "SeServiceLogonRight",
            "SeShutdownPrivilege",
            "SeSyncAgentPrivilege",
            "SeSystemEnvironmentPrivilege",
            "SeSystemProfilePrivilege",
            "SeSystemtimePrivilege",
            "SeTakeOwnershipPrivilege",
            "SeTcbPrivilege",
            "SeTimeZonePrivilege",
            "SeTrustedCredManAccessPrivilege",
            "SeUndockPrivilege"
        };


        private readonly string[] _defaultPrivileges = new[]
        {
            "SeAssignPrimaryTokenPrivilege",
            "SeAuditPrivilege",
            "SeBackupPrivilege",
            "SeBatchLogonRight",
            //"SeChangeNotifyPrivilege",
            //"SeCreateGlobalPrivilege",
            //"SeCreatePagefilePrivilege",
            //"SeCreatePermanentPrivilege",
            "SeCreateSymbolicLinkPrivilege",
            "SeCreateTokenPrivilege",
            "SeDebugPrivilege",
            "SeDenyBatchLogonRight",
            "SeDenyInteractiveLogonRight",
            "SeDenyNetworkLogonRight",
            "SeDenyRemoteInteractiveLogonRight",
            "SeDenyServiceLogonRight",
            "SeEnableDelegationPrivilege",
            "SeImpersonatePrivilege",
            //"SeIncreaseBasePriorityPrivilege",
            //"SeIncreaseQuotaPrivilege",
            //"SeIncreaseWorkingSetPrivilege",
            "SeInteractiveLogonRight",
            "SeLoadDriverPrivilege",
            //"SeLockMemoryPrivilege",
            //"SeMachineAccountPrivilege",
            //"SeManageVolumePrivilege",
            "SeNetworkLogonRight",
            //"SeProfileSingleProcessPrivilege",
            "SeRelabelPrivilege",
            "SeRemoteInteractiveLogonRight",
            "SeRemoteShutdownPrivilege",
            "SeRestorePrivilege",
            "SeSecurityPrivilege",
            "SeServiceLogonRight",
            "SeShutdownPrivilege",
            "SeSyncAgentPrivilege",
            "SeSystemEnvironmentPrivilege",
            //"SeSystemProfilePrivilege",
            //"SeSystemtimePrivilege",
            "SeTakeOwnershipPrivilege",
            "SeTcbPrivilege",
            //"SeTimeZonePrivilege",
            "SeTrustedCredManAccessPrivilege",
            //"SeUndockPrivilege"
        };

        public UserRightAssignmentsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (!SecurityUtil.IsHighIntegrity())
            {
                WriteHost("Must be an administrator to enumerate User Right Assignments");
                yield break;
            }

            var computerName = "localhost";
            string filter = null;
            LsaWrapper lsa = null;
            if (args.Length >= 1)
            {
                computerName = args[0];
            }

            if (args.Length >= 2)
            {
                filter = ".*" + args[1] + ".*";
            }

            try
            {
                lsa = new LsaWrapper(computerName);
            }
            catch (UnauthorizedAccessException)
            {
                WriteError("Insufficient privileges");
                yield break;
            }
            catch (Exception e)
            {
                WriteError("Unhandled exception enumerating user right assignments: " + e);
                yield break;
            }

            var privilegeSet = filter == null ? _defaultPrivileges : _allPrivileges.Where(p => Regex.IsMatch(p, filter, RegexOptions.IgnoreCase)).ToArray();

            foreach (var priv in privilegeSet)
            {
                var principals = lsa.ReadPrivilege(priv);

                yield return new UserRightAssignmentsDTO()
                {
                    Right = priv,
                    Principals = principals
                };
            }

            if (lsa != null)
            {
                lsa.Dispose();
            }
        }


    }

    internal class UserRightAssignmentsDTO : CommandDTOBase
    {
        public string Right { get; set; }
        public List<Principal> Principals { get; set; }
    }

    [CommandOutputType(typeof(UserRightAssignmentsDTO))]
    internal class UserRightAssignmentsTextFormatter : TextFormatterBase
    {
        public UserRightAssignmentsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {

            var dto = (UserRightAssignmentsDTO)result;
            WriteLine($"{dto.Right}:");

            if (dto.Principals.Count <= 0) return;
            foreach (var t in dto.Principals)
            {
                var accountName = "";

                accountName = !string.IsNullOrEmpty(t.Domain) ? $"{t.Domain}\\{t.User}" : t.User;

                if (string.IsNullOrEmpty(accountName))
                {
                    accountName = t.Sid;
                }

                WriteLine("    " + accountName);
            }

            WriteLine();
        }
    }
}
#nullable enable