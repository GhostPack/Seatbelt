#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    class MasterKey
    {
        public string FileName { get; set; }

        public DateTime LastAccessed { get; set; }

        public DateTime LastModified { get; set; }
    }

    internal class DpapiMasterKeysCommand : CommandBase
    {
        public override string Command => "DpapiMasterKeys";
        public override string Description => "List DPAPI master keys";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;


        public DpapiMasterKeysCommand(Runtime runtime) : base(runtime)
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

                var userDpapiBasePath = $"{dir}\\AppData\\Roaming\\Microsoft\\Protect\\";
                if (!Directory.Exists(userDpapiBasePath))
                {
                    continue;
                }

                var directories = Directory.GetDirectories(userDpapiBasePath);
                foreach (var directory in directories)
                {
                    var files = Directory.GetFiles(directory);

                    var MasterKeys = new List<MasterKey>();

                    foreach (var file in files)
                    {
                        if (!Regex.IsMatch(file, @"[0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12}"))
                        {
                            continue;
                        }

                        var masterKey = new MasterKey();
                        masterKey.FileName = Path.GetFileName(file);
                        masterKey.LastAccessed = File.GetLastAccessTime(file);
                        masterKey.LastModified = File.GetLastAccessTime(file);
                        MasterKeys.Add(masterKey);
                    }

                    yield return new DpapiMasterKeysDTO()
                    {
                        Folder = $"{directory}",
                        MasterKeys = MasterKeys
                    };
                }
            }

            WriteHost("\n  [*] Use the Mimikatz \"dpapi::masterkey\" module with appropriate arguments (/pvk or /rpc) to decrypt");
            WriteHost("  [*] You can also extract many DPAPI masterkeys from memory with the Mimikatz \"sekurlsa::dpapi\" module");
            WriteHost("  [*] You can also use SharpDPAPI for masterkey retrieval.");
        }

        internal class DpapiMasterKeysDTO : CommandDTOBase
        {
            public string Folder { get; set; }
            public List<MasterKey> MasterKeys { get; set; }
        }

        [CommandOutputType(typeof(DpapiMasterKeysDTO))]
        internal class DpapiMasterKeysFormatter : TextFormatterBase
        {
            public DpapiMasterKeysFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (DpapiMasterKeysDTO)result;

                WriteLine("  Folder : {0}\n", dto.Folder);
                WriteLine($"    LastAccessed              LastModified              FileName");
                WriteLine($"    ------------              ------------              --------");

                foreach(var masterkey in dto.MasterKeys)
                {
                    WriteLine("    {0,-22}    {1,-22}    {2}", masterkey.LastAccessed, masterkey.LastModified, masterkey.FileName);
                }

                WriteLine();
            }
        }
    }
}
#nullable enable