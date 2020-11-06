using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Util;

namespace Seatbelt.Commands
{
    internal class OracleSQLDeveloperCommand : CommandBase
    {
        public override string Command => "OracleSQLDeveloper";
        public override string Description => "Finds Oracle SQLDeveloper connections.xml files";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        // NOTE: to decrypt, use https://pypi.org/project/sqldeveloperpassworddecryptor/

        public OracleSQLDeveloperCommand(Runtime runtime) : base(runtime)
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

                foreach (string foundFile in MiscUtil.GetFileList(@"connections.xml", $"{dir}\\AppData\\Roaming\\SQL Developer\\"))
                {
                    var lastAccessed = File.GetLastAccessTime(foundFile);
                    var lastModified = File.GetLastWriteTime(foundFile);
                    var size = new FileInfo(foundFile).Length;

                    yield return new OracleConnectionsDTO()
                    {
                        FileName = foundFile,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }
            }
        }

        internal class OracleConnectionsDTO : CommandDTOBase
        {
            public string? FileName { get; set;  }
            public DateTime? LastAccessed { get; set; }
            public DateTime? LastModified { get; set; }
            public long? Size { get; set; }
        }
    }
}
