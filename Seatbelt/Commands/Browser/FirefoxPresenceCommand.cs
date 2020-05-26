using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Browser
{
    internal class FirefoxPresenceCommand : CommandBase
    {
        public override string Command => "FirefoxPresence";
        public override string Description => "Checks if interesting Firefox files exist";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public FirefoxPresenceCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {

            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);
            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users"))
                    continue;

                var userFirefoxBasePath = $"{dir}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\";
                if (!Directory.Exists(userFirefoxBasePath))
                    continue;

                var historyLastModified = new DateTime();
                var credentialFile3LastModified = new DateTime();
                var credentialFile4LastModified = new DateTime();

                var directories = Directory.GetDirectories(userFirefoxBasePath);
                foreach (var directory in directories)
                {
                    var firefoxHistoryFile = $"{directory}\\places.sqlite";
                    if (File.Exists(firefoxHistoryFile))
                    {
                        historyLastModified = File.GetLastWriteTime(firefoxHistoryFile);
                    }

                    var firefoxCredentialFile3 = $"{directory}\\key3.db";
                    if (File.Exists(firefoxCredentialFile3))
                    {
                        credentialFile3LastModified = File.GetLastWriteTime(firefoxCredentialFile3);
                    }

                    var firefoxCredentialFile4 = $"{directory}\\key4.db";
                    if (File.Exists(firefoxCredentialFile4))
                    {
                        credentialFile4LastModified = File.GetLastWriteTime(firefoxCredentialFile4);
                    }

                    if (historyLastModified != DateTime.MinValue || credentialFile3LastModified != DateTime.MinValue || credentialFile4LastModified != DateTime.MinValue)
                    {
                        yield return new FirefoxPresenceDTO(
                            directory,
                            historyLastModified,
                            credentialFile3LastModified,
                            credentialFile4LastModified
                        );
                    }
                }
            }
        }

        internal class FirefoxPresenceDTO : CommandDTOBase
        {
            public FirefoxPresenceDTO(string folder, DateTime historyLastModified, DateTime credentialFile3LastModified, DateTime credentialFile4LastModified)
            {
                Folder = folder;
                HistoryLastModified = historyLastModified;
                CredentialFile3LastModified = credentialFile3LastModified;
                CredentialFile4LastModified = credentialFile4LastModified;  
            }
            public string Folder { get; }
            public DateTime HistoryLastModified { get; }
            public DateTime CredentialFile3LastModified { get; }
            public DateTime CredentialFile4LastModified { get; }
        }

        [CommandOutputType(typeof(FirefoxPresenceDTO))]
        internal class FirefoxPresenceFormatter : TextFormatterBase
        {
            public FirefoxPresenceFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (FirefoxPresenceDTO)result;

                WriteLine("  {0}\\\n", dto.Folder);
                if (dto.HistoryLastModified != DateTime.MinValue)
                {
                    WriteLine("    'places.sqlite'  ({0})  :  History file, run the 'FirefoxTriage' command", dto.HistoryLastModified);
                }
                if (dto.CredentialFile3LastModified != DateTime.MinValue)
                {
                    WriteLine("    'key3.db'        ({0})  :  Credentials file, run SharpWeb (https://github.com/djhohnstein/SharpWeb)", dto.CredentialFile3LastModified);
                }
                if (dto.CredentialFile4LastModified != DateTime.MinValue)
                {
                    WriteLine("    'key4.db'        ({0})  :  Credentials file, run SharpWeb (https://github.com/djhohnstein/SharpWeb)", dto.CredentialFile4LastModified);
                }
                WriteLine();
            }
        }
    }
}
