using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Products
{
    class Workspace
    {
        public string? Name { get; set; }
        public string? Domain { get; set; }
        public string? ID { get; set; }
    }

    internal class SlackWorkspacesCommand : CommandBase
    {
        public override string Command => "SlackWorkspaces";
        public override string Description => "Parses any found 'slack-workspaces' files";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Slack };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public SlackWorkspacesCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                var userSlackWorkspacesPath = $"{dir}\\AppData\\Roaming\\Slack\\storage\\slack-workspaces";

                // parses a Slack workspaces file
                if (File.Exists(userSlackWorkspacesPath))
                {
                    var workspaces = new List<Workspace>();

                    try
                    {
                        var contents = File.ReadAllText(userSlackWorkspacesPath);

                        // reference: http://www.tomasvera.com/programming/using-javascriptserializer-to-parse-json-objects/
                        var json = new JavaScriptSerializer();
                        var deserialized = json.Deserialize<Dictionary<string, object>>(contents);

                        foreach (var w in deserialized)
                        {
                            var settings = (Dictionary<string, object>)w.Value;

                            var workspace = new Workspace();
                            if (settings.ContainsKey("name"))
                            {
                                workspace.Name = $"{settings["name"]}";
                            }
                            if (settings.ContainsKey("domain"))
                            {
                                workspace.Domain = $"{settings["domain"]}";
                            }
                            if (settings.ContainsKey("id"))
                            {
                                workspace.ID = $"{settings["id"]}";
                            }

                            workspaces.Add(workspace);
                        }
                    }
                    catch (IOException exception)
                    {
                        WriteError(exception.ToString());
                    }
                    catch (Exception exception)
                    {
                        WriteError(exception.ToString());
                    }

                    yield return new SlackWorkspacesDTO(
                        userName,
                        workspaces
                    );
                }
            }
        }

        internal class SlackWorkspacesDTO : CommandDTOBase
        {
            public SlackWorkspacesDTO(string userName, List<Workspace> workspaces)
            {
                UserName = userName;
                Workspaces = workspaces;
            }

            public string UserName { get; }
            public List<Workspace> Workspaces { get; }
        }

        [CommandOutputType(typeof(SlackWorkspacesDTO))]
        internal class SlackWorkspacesFormatter : TextFormatterBase
        {
            public SlackWorkspacesFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (SlackWorkspacesDTO)result;

                WriteLine($"  Workspaces ({dto.UserName}):\n");

                foreach (var workspace in dto.Workspaces)
                {
                    WriteLine($"    Name   : {workspace.Name}");
                    WriteLine($"    Domain : {workspace.Domain}");
                    WriteLine($"    ID     : {workspace.ID}\n");
                }
                WriteLine();
            }
        }
    }
}