using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Products
{

    internal class OneNoteCommand : CommandBase
    {
        public override string Command => "OneNote";
        public override string Description => "List OneNote backup files";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public Runtime ThisRunTime;

        public OneNoteCommand(Runtime runtime) : base(runtime)
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

                var userOneNoteBasePath = $"{dir}\\AppData\\Local\\Microsoft\\OneNote";
                var resultFiles = new List<String>();

                if (Directory.Exists(userOneNoteBasePath))
                {
                    var oneNoteFiles = Directory.GetFiles(userOneNoteBasePath, "*.one", SearchOption.AllDirectories);
                    resultFiles.AddRange(oneNoteFiles);
                }

                yield return new OneNoteDTO(
                    userName,
                    resultFiles
                );
            }
        }

        internal class OneNoteDTO : CommandDTOBase
        {
            public OneNoteDTO(string userName, List<string> files)
            {
                UserName = userName;
                Files = files;
            }

            public string UserName { get; }
            public List<string> Files { get;  }
        }

        [CommandOutputType(typeof(OneNoteDTO))]
        internal class OneNoteFormatter : TextFormatterBase
        {
            public OneNoteFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (OneNoteDTO)result;

                WriteLine($"\n    OneNote files ({dto.UserName}):\n");

                foreach (var file in dto.Files)
                {
                    WriteLine($"       {file}");
                }
                WriteLine();
            }
        }

    }
}