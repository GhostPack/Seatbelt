using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

namespace Seatbelt.Commands
{
    internal class PuttySessionsCommand : CommandBase
    {
        public override string Command => "PuttySessions";
        public override string Description => "Saved Putty configuration (interesting fields) and SSH host keys";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public PuttySessionsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var SIDs = ThisRunTime.GetUserSIDs();
            foreach (var sid in SIDs)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                var subKeys = ThisRunTime.GetSubkeyNames(RegistryHive.Users, $"{sid}\\Software\\SimonTatham\\PuTTY\\Sessions\\");
                if (subKeys == null)
                    continue;

                var Sessions = new List<Dictionary<string, string>>();

                foreach (var sessionName in subKeys)
                {
                    var Settings = new Dictionary<string, string>
                    {
                        ["SessionName"] = sessionName
                    };

                    string[] keys =
                    {
                        "HostName",
                        "UserName",
                        "PublicKeyFile",
                        "PortForwardings",
                        "ConnectionSharing",
                        "AgentFwd"
                    };

                    foreach (var key in keys)
                    {
#nullable disable
                        var result = ThisRunTime.GetStringValue(RegistryHive.Users, $"{sid}\\Software\\SimonTatham\\PuTTY\\Sessions\\{sessionName}", key);
                        if (!string.IsNullOrEmpty(result))
                        {
                            Settings[key] = result;
                        }
#nullable enable
                    }

                    Sessions.Add(Settings);
                }

                if (Sessions.Count != 0)
                {
                    yield return new PuttySessionsDTO(
                        sid,
                        Sessions
                    );
                }
            }
        }

        internal class PuttySessionsDTO : CommandDTOBase
        {
            public PuttySessionsDTO(string sid, List<Dictionary<string, string>> sessions)
            {
                Sid = sid;
                Sessions = sessions;
            }
            public string Sid { get; }

            public List<Dictionary<string,string>> Sessions { get; }
        }

        [CommandOutputType(typeof(PuttySessionsDTO))]
        internal class ExplorerRunCommandFormatter : TextFormatterBase
        {
            public ExplorerRunCommandFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (PuttySessionsDTO)result;

                WriteLine("  {0} :\n", dto.Sid);

                foreach (var session in dto.Sessions)
                {
                    WriteLine("     {0,-20} : {1}", "SessionName", session["SessionName"]);

                    foreach (var key in session.Keys)
                    {
                        if(!key.Equals("SessionName"))
                        {
                            WriteLine("     {0,-20} : {1}", key, session[key]);
                        }
                    }
                    WriteLine();
                }
                WriteLine();
            }
        }
    }
}
