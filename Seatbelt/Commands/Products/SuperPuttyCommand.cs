using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands
{
    class SuperPuttyConfig
    {
        public SuperPuttyConfig(string filePath, string sessionId, string sessionName, string host, string port, string protocol, string userName, string extraArgs)
        {
            FilePath = filePath;
            SessionID = sessionId;
            SessionName = sessionName;
            Host = host;
            Port = port;
            Protocol = protocol;
            UserName = userName;
            ExtraArgs = extraArgs;  
        }
        public string FilePath { get; set; }

        public string SessionID { get; set; }

        public string SessionName { get; set; }

        public string Host { get; set; }

        public string Port { get; set; }

        public string Protocol { get; set; }

        public string UserName { get; set; }

        public string ExtraArgs { get; set; }
    }


    internal class SuperPuttyCommand : CommandBase
    {
        public override string Command => "SuperPutty";
        public override string Description => "SuperPutty configuration files";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public SuperPuttyCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // inspired by https://github.com/EncodeGroup/Gopher/blob/master/Holes/SuperPuTTY.cs (@lefterispan)

            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users"))
                    continue;

                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                var configs = new List<SuperPuttyConfig>();
                
                string[] paths = { $"{dir}\\Documents\\SuperPuTTY\\Sessions.XML"};

                foreach (var path in paths)
                {
                    if (!File.Exists(path))
                        continue;
                    

                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(path);

                    var sessions = xmlDoc.GetElementsByTagName("SessionData");
                    
                    if (sessions.Count == 0)
                        continue;

                    foreach (XmlNode session in sessions)
                    {
                        var filePath = path;
                        var sessionID = session.Attributes["SessionId"].Value;
                        var sessionName = session.Attributes["SessionName"].Value;
                        var host = session.Attributes["Host"].Value;
                        var port = session.Attributes["Port"].Value;
                        var protocol = session.Attributes["Proto"].Value;
                        var user = session.Attributes["Username"].Value;
                        var extraArgs = session.Attributes["ExtraArgs"].Value;

                        var config = new SuperPuttyConfig(
                            filePath,
                            sessionID,
                            sessionName,
                            host,
                            port,
                            protocol,
                            user,
                            extraArgs
                            );

                        configs.Add(config);
                    }
                }

                if (configs.Count > 0)
                {
                    yield return new SuperPuttyDTO(
                        userName,
                        configs
                    );
                }
            }
        }

        internal class SuperPuttyDTO : CommandDTOBase
        {
            public SuperPuttyDTO(string userName, List<SuperPuttyConfig> configs)
            {
                UserName = userName;
                Configs = configs;
            }
            public string UserName { get; set; }
            public List<SuperPuttyConfig> Configs { get; set; }
        }

        [CommandOutputType(typeof(SuperPuttyDTO))]
        internal class SuperPuttyFormatter : TextFormatterBase
        {
            public SuperPuttyFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (SuperPuttyDTO)result;

                WriteLine($"  SuperPutty Configs ({dto.UserName}):\n");

                foreach (var config in dto.Configs)
                {
                    WriteLine($"    FilePath    : {config.FilePath}");
                    WriteLine($"    SessionID   : {config.SessionID}");
                    WriteLine($"    SessionName : {config.SessionName}");
                    WriteLine($"    Host        : {config.Host}");
                    WriteLine($"    Port        : {config.Port}");
                    WriteLine($"    Protocol    : {config.Protocol}");
                    WriteLine($"    Username    : {config.UserName}");
                    if(!String.IsNullOrEmpty(config.ExtraArgs))
                    {
                        WriteLine($"    ExtraArgs   : {config.ExtraArgs}");
                    }
                    WriteLine();
                }
                WriteLine();
            }
        }
    }
}
