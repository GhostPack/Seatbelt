using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Util;

namespace Seatbelt.Commands
{
    internal class McAfeeConfigsCommand : CommandBase
    {
        public override string Command => "McAfeeConfigs";
        public override string Description => "Finds McAfee configuration files";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false; // TODO when remote file searching is worked out... though it might take a while

        public McAfeeConfigsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // if -full is passed, or any argument is passed, recursively search common locations for configuration files
            if (!Runtime.FilterResults || (args.Length == 1))
            {
                string[] paths = { @"C:\Program Files\", @"C:\Program Files (x86)\", @"C:\ProgramData\", @"C:\Documents and Settings\", @"C:\Users\" };
                foreach (string path in paths)
                {
                    foreach (string foundFile in MiscUtil.GetFileList(@"ma.db|SiteMgr.xml|SiteList.xml", path))
                    {
                        var lastAccessed = File.GetLastAccessTime(foundFile);
                        var lastModified = File.GetLastWriteTime(foundFile);
                        var size = new FileInfo(foundFile).Length;

                        yield return new McAfeeConfigsDTO()
                        {
                            FileName = foundFile,
                            LastAccessed = lastAccessed,
                            LastModified = lastModified,
                            Size = size
                        };
                    }
                }
            }
            else
            {
                string[] paths = {  $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\All Users\\McAfee\\Agent\\DB\\ma.db",
                                    $"{Environment.GetEnvironmentVariable("SystemDrive")}\\ProgramData\\McAfee\\Agent\\DB\\ma.db"};

                foreach (string path in paths)
                {
                    if (File.Exists(path))
                    {
                        var lastAccessed = File.GetLastAccessTime(path);
                        var lastModified = File.GetLastWriteTime(path);
                        var size = new FileInfo(path).Length;

                        yield return new McAfeeConfigsDTO()
                        {
                            FileName = path,
                            LastAccessed = lastAccessed,
                            LastModified = lastModified,
                            Size = size
                        };
                    }
                }
            }
        }

        internal class McAfeeConfigsDTO : CommandDTOBase
        {
            public string? FileName { get; set;  }
            public DateTime? LastAccessed { get; set; }
            public DateTime? LastModified { get; set; }
            public long? Size { get; set; }
        }
    }
}
