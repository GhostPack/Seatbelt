#nullable disable
using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    class RDPConnection
    {
        public string RemoteHost { get; set; }
        public string UserNameHint { get; set; }
    }

    internal class RDPSavedConnectionCommand : CommandBase
    {
        public override string Command => "RDPSavedConnections";
        public override string Description => "Saved RDP connections stored in the registry";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public RDPSavedConnectionCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var SIDs = ThisRunTime.GetUserSIDs();
            //shows saved RDP connections, including username hints (if present)
            foreach (var sid in SIDs)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                var subkeys = ThisRunTime.GetSubkeyNames(RegistryHive.Users, $"{sid}\\Software\\Microsoft\\Terminal Server Client\\Servers");
                if (subkeys == null)
                {
                    continue;
                }

                if (subkeys.Length <= 0)
                {
                    continue;
                }

                var connections = new List<RDPConnection>();

                foreach (var host in subkeys)
                {
                    var usernameHint = ThisRunTime.GetStringValue(RegistryHive.Users,
                        $"{sid}\\Software\\Microsoft\\Terminal Server Client\\Servers\\{host}", "UsernameHint");

                    var connection = new RDPConnection();
                    connection.RemoteHost = host;
                    connection.UserNameHint = usernameHint;
                    connections.Add(connection);
                }

                yield return new RDPSavedConnectionDTO()
                {
                    SID = sid,
                    Connections = connections
                };
            }

            yield break;
        }

        internal class RDPSavedConnectionDTO : CommandDTOBase
        {
            public string SID { get; set; }
            public List<RDPConnection> Connections { get; set; }
        }

        [CommandOutputType(typeof(RDPSavedConnectionDTO))]
        internal class RDPSavedConnectionCommandFormatter : TextFormatterBase
        {
            public RDPSavedConnectionCommandFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (RDPSavedConnectionDTO)result;

                if (dto.Connections.Count > 0)
                {
                    WriteLine("Saved RDP Connection Information ({0})\n", dto.SID);

                    WriteLine("  RemoteHost                         UsernameHint");
                    WriteLine("  ----------                         ------------");

                    foreach (var connection in dto.Connections)
                    {
                        WriteLine($"  {connection.RemoteHost.PadRight(35)}{connection.UserNameHint}");
                    }
                    WriteLine();
                }
            }
        }
    }
}
#nullable enable