using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands
{
    internal class RemoteDesktopConnectionManagerCommand : CommandBase
    {
        public override string Command => "RDCManFiles";
        public override string Description => "Windows Remote Desktop Connection Manager settings files";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public RemoteDesktopConnectionManagerCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);
            var found = false;

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users"))
                    continue;
                
                var userRDManFile = $"{dir}\\AppData\\Local\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings";
                if (!File.Exists(userRDManFile))
                    continue;
                

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(userRDManFile);

                // grab the recent RDG files
                var filesToOpen = xmlDoc.GetElementsByTagName("FilesToOpen");
                var items = filesToOpen[0].ChildNodes;

                var lastAccessed = File.GetLastAccessTime(userRDManFile);
                var lastModified = File.GetLastWriteTime(userRDManFile);

                var rdgFiles = new List<string>();

                foreach (XmlNode rdgFile in items)
                {
                    found = true;
                    rdgFiles.Add(rdgFile.InnerText);
                }

                yield return new RemoteDesktopConnectionManagerDTO(
                    userRDManFile,
                    lastAccessed,
                    lastModified,
                    rdgFiles
                );
            }

            if (found)
            {
                WriteHost("  [*] You can use SharpDPAPI or the Mimikatz \"dpapi::rdg\" module to decrypt any found .rdg files");
            }
        }

        internal class RemoteDesktopConnectionManagerDTO : CommandDTOBase
        {
            public RemoteDesktopConnectionManagerDTO(string fileName, DateTime lastAccessed, DateTime lastModified, List<string> rdgFiles)
            {
                FileName = fileName;
                LastAccessed = lastAccessed;
                LastModified = lastModified;
                RdgFiles = rdgFiles;    
            }
            public string FileName { get; }

            public DateTime LastAccessed { get; }

            public DateTime LastModified { get; }

            public List<string> RdgFiles { get; }
        }


        [CommandOutputType(typeof(RemoteDesktopConnectionManagerDTO))]
        internal class RemoteDesktopConnectionManagerFormatter : TextFormatterBase
        {
            public RemoteDesktopConnectionManagerFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (RemoteDesktopConnectionManagerDTO)result;

                WriteLine("  RDCManFile   : {0}", dto.FileName);
                WriteLine("  Accessed     : {0}", dto.LastAccessed);
                WriteLine("  Modified     : {0}", dto.LastModified);

                foreach(var rdgFile in dto.RdgFiles)
                {
                    WriteLine("    .RDG File  : {0}", rdgFile);
                }

                WriteLine();
            }
        }
    }
}
