#nullable disable
using System;
using System.Collections.Generic;
using static Seatbelt.Interop.Netapi32;


namespace Seatbelt.Commands.Windows
{
    internal class LocalUserCommand : CommandBase
    {
        public override string Command => "LocalUsers";
        public override string Description => "Local users, whether they're active/disabled, and pwd last set (argument == computername to enumerate)";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public LocalUserCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // takes an optional remote computername as an argument, otherwise defaults to the localhost

            string computerName = null;

            if (!String.IsNullOrEmpty(ThisRunTime.ComputerName))
            {
                computerName = ThisRunTime.ComputerName;
            }
            else if (args.Length == 1)
            {
                computerName = args[0];
            }

            // adapted from https://stackoverflow.com/questions/33935825/pinvoke-netlocalgroupgetmembers-runs-into-fatalexecutionengineerror/33939889#33939889
            foreach(var localUser in GetLocalUsers(computerName))
            {
                var enabled = ((localUser.flags >> 1) & 1) == 0;
                var pwdLastSet = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var lastLogon = new DateTime(1970, 1, 1, 0, 0, 0);

                if (localUser.passwordAge != 0)
                {
                    pwdLastSet = DateTime.Now.AddSeconds(-localUser.passwordAge);
                }

                if(localUser.last_logon != 0)
                {
                    lastLogon = lastLogon.AddSeconds(localUser.last_logon).ToLocalTime();
                }
                
                yield return new LocalUserDTO(
                    computerName ?? "localhost",
                    localUser.name,
                    enabled,
                    localUser.user_id,
                    (Priv)localUser.priv,
                    localUser.comment,
                    pwdLastSet,
                    lastLogon,
                    localUser.num_logons
                );
            }
        }
    }

    internal class LocalUserDTO : CommandDTOBase
    {
        public LocalUserDTO(string computerName, string userName, bool enabled, uint rid, Priv userType, string comment, DateTime pwdLastSet, DateTime lastLogon, uint numLogins)
        {
            ComputerName = computerName;
            UserName = userName;
            Enabled = enabled;
            Rid = rid;
            UserType = userType;
            Comment = comment;
            PwdLastSet = pwdLastSet;
            LastLogon = lastLogon;
            NumLogins = numLogins;  
        }
        public string ComputerName { get; }
        public string UserName { get; }
        public bool Enabled { get; }
        public uint Rid { get; }
        public Priv UserType { get; }
        public string Comment { get; }
        public DateTime PwdLastSet { get; }
        public DateTime LastLogon { get; }
        public uint NumLogins { get; }
    }
}
#nullable enable