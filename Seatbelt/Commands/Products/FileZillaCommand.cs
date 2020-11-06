using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands
{
    class FileZillaConfig
    {
        public FileZillaConfig(string filePath, string name, string host, string port, string protocol, string userName, string password)
        {
            FilePath = filePath;
            Name = name;
            Host = host;
            Port = port;
            Protocol = protocol;
            UserName = userName;
            Password = password;    
        }
        public string FilePath { get; set; }

        public string Name { get; set; }

        public string Host { get; set; }

        public string Port { get; set; }

        public string Protocol { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }
    }


    internal class FileZillaCommand : CommandBase
    {
        public override string Command => "FileZilla";
        public override string Description => "FileZilla configuration files";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public FileZillaCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // inspired by https://github.com/EncodeGroup/Gopher/blob/master/Holes/FileZilla.cs (@lefterispan)

            // information on FileZilla master key encryption:
            //  https://forum.filezilla-project.org/viewtopic.php?f=3&t=64&start=1005#p156191

            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users"))
                    continue;

                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                var configs = new List<FileZillaConfig>();

                string[] paths = { $"{dir}\\AppData\\Roaming\\FileZilla\\sitemanager.xml", $"{dir}\\AppData\\Roaming\\FileZilla\\recentservers.xml" };

                foreach (var path in paths)
                {
                    if (!File.Exists(path))
                        continue;

                    var xmlDoc = new XmlDocument();
                    
                    xmlDoc.Load(path);

                    // handle "Servers" (sitemanager.xml) and "RecentServers" (recentservers.xml)
                    var servers = xmlDoc.GetElementsByTagName("Servers");
                    if(servers.Count == 0)
                        servers = xmlDoc.GetElementsByTagName("RecentServers");

                    // ensure we have at least one server to process
                    if ((servers.Count == 0) || (servers[0].ChildNodes.Count <= 0))
                        continue;

                    foreach (XmlNode server in servers[0].ChildNodes)
                    {
                        var name = "<RECENT SERVER>";
                        var tempName = server.SelectSingleNode("Name");
                        if (tempName != null)
                        {
                            name = server.SelectSingleNode("Name").InnerText;
                        }

                        var host = server.SelectSingleNode("Host").InnerText;
                        var port = server.SelectSingleNode("Port").InnerText;
                        var protocol = server.SelectSingleNode("Protocol").InnerText;
                        var user = server.SelectSingleNode("User").InnerText;

                        var tempPassword = server.SelectSingleNode("Pass");
                        var password = "<NULL>";

                        if (tempPassword != null)
                        {
                            if (tempPassword.Attributes["encoding"].Value == "base64")
                            {
                                password = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(tempPassword.InnerText));
                            }
                            else
                            {
                                password = "<PROTECTED BY MASTERKEY>";
                            }
                        }

                        var config = new FileZillaConfig(
                            path,
                            name,
                            host,
                            port,
                            protocol,
                            user,
                            password
                            );

                        configs.Add(config);
                    }
                }

                if (configs.Count > 0)
                {
                    yield return new FileZillaDTO(
                        userName,
                        configs
                    );
                }
            }
        }

        internal class FileZillaDTO : CommandDTOBase
        {
            public FileZillaDTO(string userName, List<FileZillaConfig> configs)
            {
                UserName = userName;
                Configs = configs;  
            }
            public string UserName { get; set; }
            public List<FileZillaConfig> Configs { get; set; }
        }

        [CommandOutputType(typeof(FileZillaDTO))]
        internal class FileZillaFormatter : TextFormatterBase
        {
            public FileZillaFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (FileZillaDTO)result;

                WriteLine($"  FileZilla Configs ({dto.UserName}):\n");

                foreach (var config in dto.Configs)
                {
                    WriteLine($"    FilePath  : {config.FilePath}");
                    WriteLine($"    Name      : {config.Name}");
                    WriteLine($"    Host      : {config.Host}");
                    WriteLine($"    Port      : {config.Port}");
                    WriteLine($"    Protocol  : {config.Protocol}");
                    WriteLine($"    Username  : {config.UserName}");
                    WriteLine($"    Password  : {config.Password}\n");
                }
                WriteLine();
            }
        }
    }
}
