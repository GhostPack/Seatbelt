#nullable disable
using static Seatbelt.Interop.Netapi32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

namespace Seatbelt.Commands.Windows
{
    internal class PowerShellHistoryCommand : CommandBase
    { 

        public override string Command => "PowerShellHistory";
        public override string Description => "Iterates through every local user and attempts to read their PowerShell console history if successful will print it.";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;
        public Runtime ThisRunTime;

        public PowerShellHistoryCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            string computerName = ThisRunTime.ComputerName;

            // An alternative approach to obtaining local users if you do not want to use P/Invoke and system is using AD
            // https://stackoverflow.com/questions/8191696/list-all-local-users-using-directory-service

            foreach (var localUser in GetLocalUsers(computerName))
            {
                // Loop through each local user and attempt to read their consolehost history
                // This is enabled by default starting with PowerShell v5 on Windows 10
                // Does not record terminal less PowerShell sessions

                string Errored = "";
                string user = localUser.name;
                string content = "";
                try
                {
                    string path = $"C:\\Users\\{user}\\AppData\\Roaming\\Microsoft\\Windows\\PowerShell\\PSReadline\\ConsoleHost_history.txt";
                    if (System.IO.File.Exists(path))
                    {
                        // Make sure file exists before attempting to read it
                        content += System.IO.File.ReadAllText(path);
                    }
                    else
                    {
                        Errored = "ConsoleHost_History.txt was not found";
                    }
                }
                catch(System.Exception e)
                {
                    Errored = e.Message;
                }

                if (Errored.Length > 0)
                {
                    yield return new PowerShellHistoryDTO() { User = user, Error = Errored };
                }
                else 
                {
                    yield return new PowerShellHistoryDTO() { User = user, Content = content };
                }
            }
        }

        internal class PowerShellHistoryDTO : CommandDTOBase
        {
            public string? User { get; set; }
            public string? Content { get; set; }
            public string? Error { get; set; } = "";
        }
        
        [CommandOutputType(typeof(PowerShellHistoryDTO))]
        internal class PowerShellHistoryFormatter : TextFormatterBase
        {
            public PowerShellHistoryFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (PowerShellHistoryDTO)result;
                if (dto.Error.Length > 0)
                {
                    WriteLine($"An error has occurred when attempting to read the history of local user: {dto.User} error: {dto.Error}.");
                }
                else
                {
                    WriteLine($"PowerShell Console History for local user: {dto.User}: \n");
                    WriteLine(dto.Content);
                }
            }

        }
    }
}
#nullable enable