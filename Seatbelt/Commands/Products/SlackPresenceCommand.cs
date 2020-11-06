using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands
{
    internal class SlackPresenceCommand : CommandBase
    {
        public override string Command => "SlackPresence";
        public override string Description => "Checks if interesting Slack files exist";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Slack };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public SlackPresenceCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                var slackBasePath = $"{dir}\\AppData\\Roaming\\Slack\\";
                if (!Directory.Exists(slackBasePath))
                {
                    continue;
                }

                DateTime? cookiesLastWriteTime = null,
                    workspacesLastWriteTime = null,
                    downloadsLastWriteTime = null;

                var userSlackCookiesPath = $"{dir}\\AppData\\Roaming\\Slack\\Cookies";
                if (File.Exists(userSlackCookiesPath))
                {
                    cookiesLastWriteTime = File.GetLastWriteTime(userSlackCookiesPath);
                }

                var userSlackWorkspacesPath = $"{dir}\\AppData\\Roaming\\Slack\\storage\\slack-workspaces";
                if (File.Exists(userSlackWorkspacesPath))
                {
                    workspacesLastWriteTime = File.GetLastWriteTime(userSlackWorkspacesPath);
                }

                var userSlackDownloadsPath = $"{dir}\\AppData\\Roaming\\Slack\\storage\\slack-downloads";
                if (File.Exists(userSlackDownloadsPath))
                {
                    downloadsLastWriteTime = File.GetLastWriteTime(userSlackDownloadsPath);
                }

                if (cookiesLastWriteTime != null || workspacesLastWriteTime != null || downloadsLastWriteTime != null)
                {
                    yield return new SlackPresenceDTO(
                        folder: $"{dir}\\AppData\\Roaming\\Slack\\",
                        cookiesLastWriteTime,
                        workspacesLastWriteTime,
                        downloadsLastWriteTime
                    );
                }
            }
        }

        internal class SlackPresenceDTO : CommandDTOBase
        {
            public SlackPresenceDTO(string folder, DateTime? cookiesLastModified, DateTime? workspacesLastModified, DateTime? downloadsLastModified)
            {
                Folder = folder;
                CookiesLastModified = cookiesLastModified;
                WorkspacesLastModified = workspacesLastModified;
                DownloadsLastModified = downloadsLastModified;
            }
            public string? Folder { get; }
            public DateTime? CookiesLastModified { get; }
            public DateTime? WorkspacesLastModified { get; }
            public DateTime? DownloadsLastModified { get; }
        }

        [CommandOutputType(typeof(SlackPresenceDTO))]
        internal class SlackPresenceFormatter : TextFormatterBase
        {
            public SlackPresenceFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (SlackPresenceDTO)result;

                WriteLine("  {0}\n", dto.Folder);
                if (dto.CookiesLastModified != DateTime.MinValue)
                {
                    WriteLine("    'Cookies'                   ({0})  :  Download the 'Cookies' and 'storage\\slack-workspaces' files to clone Slack access", dto.CookiesLastModified);
                }
                if (dto.WorkspacesLastModified != DateTime.MinValue)
                {
                    WriteLine("    '\\storage\\slack-workspaces' ({0})  :  Run the 'SlackWorkspaces' command", dto.WorkspacesLastModified);
                }
                if (dto.DownloadsLastModified != DateTime.MinValue)
                {
                    WriteLine("    '\\storage\\slack-downloads'  ({0})  :  Run the 'SlackDownloads' command", dto.DownloadsLastModified);
                }

                WriteLine();
            }
        }
    }
}
