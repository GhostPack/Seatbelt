#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    class OutlookDownload
    {
        public string FileName { get; set; }

        public DateTime LastAccessed { get; set; }

        public DateTime LastModified { get; set; }
    }

    internal class OutlookDownloadsCommand : CommandBase
    {
        public override string Command => "OutlookDownloads";
        public override string Description => "List files downloaded by Outlook";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public OutlookDownloadsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users"))
                {
                    continue;
                }

                var userOutlookBasePath = $"{dir}\\AppData\\Local\\Microsoft\\Windows\\INetCache\\Content.Outlook\\";
                if (!Directory.Exists(userOutlookBasePath))
                {
                    continue;
                }

                var directories = Directory.GetDirectories(userOutlookBasePath);
                foreach (var directory in directories)
                {
                    var files = Directory.GetFiles(directory);

                    var Downloads = new List<OutlookDownload>();

                    foreach (var file in files)
                    {
                        var download = new OutlookDownload();
                        download.FileName = Path.GetFileName(file);
                        download.LastAccessed = File.GetLastAccessTime(file);
                        download.LastModified = File.GetLastAccessTime(file);
                        Downloads.Add(download);
                    }

                    yield return new OutlookDownloadsDTO()
                    {
                        Folder = $"{directory}",
                        Downloads = Downloads
                    };
                }
            }
        }

        internal class OutlookDownloadsDTO : CommandDTOBase
        {
            public string Folder { get; set; }
            public List<OutlookDownload> Downloads { get; set; }
        }

        [CommandOutputType(typeof(OutlookDownloadsDTO))]
        internal class OutlookDownloadsFormatter : TextFormatterBase
        {
            public OutlookDownloadsFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (OutlookDownloadsDTO)result;

                WriteLine("  Folder : {0}\n", dto.Folder);
                WriteLine($"    LastAccessed              LastModified              FileName");
                WriteLine($"    ------------              ------------              --------");

                foreach (var download in dto.Downloads)
                {
                    WriteLine("    {0,-22}    {1,-22}    {2}", download.LastAccessed, download.LastModified, download.FileName);
                }

                WriteLine();
            }
        }
    }
}
#nullable enable