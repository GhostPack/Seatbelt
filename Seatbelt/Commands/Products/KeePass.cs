using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Util;

namespace Seatbelt.Commands
{
    internal class KeePassCommand : CommandBase
    {
        public override string Command => "KeePass";
        public override string Description => "Finds KeePass configuration files";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public KeePassCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users"))
                    continue;

                if(File.Exists($"{dir}\\AppData\\Roaming\\KeePass\\KeePass.config.xml"))
                {
                    string foundFile = $"{dir}\\AppData\\Roaming\\KeePass\\KeePass.config.xml";
                    var lastAccessed = File.GetLastAccessTime(foundFile);
                    var lastModified = File.GetLastWriteTime(foundFile);
                    var size = new FileInfo(foundFile).Length;

                    yield return new KeePassDTO()
                    {
                        FileName = foundFile,
                        MasterKeyGuid = "",
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }
                if (File.Exists($"{dir}\\AppData\\Roaming\\KeePass\\ProtectedUserKey.bin"))
                {
                    string foundFile = $"{dir}\\AppData\\Roaming\\KeePass\\ProtectedUserKey.bin";
                    var lastAccessed = File.GetLastAccessTime(foundFile);
                    var lastModified = File.GetLastWriteTime(foundFile);
                    var size = new FileInfo(foundFile).Length;

                    byte[] blobBytes = File.ReadAllBytes(foundFile);
                    var offset = 24;
                    var guidMasterKeyBytes = new byte[16];
                    Array.Copy(blobBytes, offset, guidMasterKeyBytes, 0, 16);
                    var guidMasterKey = new Guid(guidMasterKeyBytes);
                    var guidString = $"{{{guidMasterKey}}}";

                    yield return new KeePassDTO()
                    {
                        FileName = foundFile,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        MasterKeyGuid = guidString,
                        Size = size
                    };
                }
            }
        }

        internal class KeePassDTO : CommandDTOBase
        {
            public string? FileName { get; set; }
            public string? MasterKeyGuid { get; set; }
            public DateTime? LastAccessed { get; set; }
            public DateTime? LastModified { get; set; }
            public long? Size { get; set; }
        }
    }
}
