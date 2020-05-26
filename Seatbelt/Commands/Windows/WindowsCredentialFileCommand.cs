#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    class CredentialFile
    {
        public string FileName { get; set; }

        public string Description { get; set; }

        public Guid GUIDMasterKey { get; set; }

        public DateTime LastAccessed { get; set; }

        public DateTime LastModified { get; set; }

        public long Size { get; set; }
    }

    internal class WindowsCredentialFileCommand : CommandBase
    {
        public override string Command => "WindowsCredentialFiles";
        public override string Description => "Windows credential DPAPI blobs";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public WindowsCredentialFileCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists any found files in Local\Microsoft\Credentials\*

            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                var userCredFilePath = $"{dir}\\AppData\\Local\\Microsoft\\Credentials\\";
                if (!Directory.Exists(userCredFilePath))
                {
                    continue;
                }

                var userFiles = Directory.GetFiles(userCredFilePath);
                if (userFiles.Length == 0)
                {
                    continue;
                }

                List<CredentialFile> UserCredentials = new List<CredentialFile>();

                foreach (var file in userFiles)
                {
                    var lastAccessed = File.GetLastAccessTime(file);
                    var lastModified = File.GetLastWriteTime(file);
                    var size = new FileInfo(file).Length;
                    var fileName = Path.GetFileName(file);
                    
                    // jankily parse the bytes to extract the credential type and master key GUID
                    // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                    var credentialArray = File.ReadAllBytes(file);
                    var guidMasterKeyArray = new byte[16];
                    Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                    var guidMasterKey = new Guid(guidMasterKeyArray);

                    var stringLenArray = new byte[16];
                    Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                    var descLen = BitConverter.ToInt32(stringLenArray, 0);

                    var descBytes = new byte[descLen-4];
                    Array.Copy(credentialArray, 60, descBytes, 0, descBytes.Length);

                    var desc = Encoding.Unicode.GetString(descBytes);

                    CredentialFile userCredential = new CredentialFile();
                    userCredential.FileName = Path.GetFileName(file);
                    userCredential.Description = desc;
                    userCredential.GUIDMasterKey = guidMasterKey;
                    userCredential.LastAccessed = File.GetLastAccessTime(file);
                    userCredential.LastModified = File.GetLastAccessTime(file);
                    userCredential.Size = size;
                    UserCredentials.Add(userCredential);
                }

                yield return new WindowsCredentialFileDTO()
                {
                    Folder = userCredFilePath,
                    CredentialFiles = UserCredentials
                };
            }

            var systemFolder = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\config\\systemprofile\\AppData\\Local\\Microsoft\\Credentials";

            if (Directory.Exists(systemFolder))
            {
                var files = Directory.GetFiles(systemFolder);

                if (files.Length != 0)
                {
                    List<CredentialFile> SystemCredentials = new List<CredentialFile>();

                    foreach (var file in files)
                    {
                        var lastAccessed = File.GetLastAccessTime(file);
                        var lastModified = File.GetLastWriteTime(file);
                        var size = new FileInfo(file).Length;
                        var fileName = Path.GetFileName(file);

                        // jankily parse the bytes to extract the credential type and master key GUID
                        // reference- https://github.com/gentilkiwi/mimikatz/blob/3d8be22fff9f7222f9590aa007629e18300cf643/modules/kull_m_dpapi.h#L24-L54
                        var credentialArray = File.ReadAllBytes(file);
                        var guidMasterKeyArray = new byte[16];
                        Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                        var guidMasterKey = new Guid(guidMasterKeyArray);

                        var stringLenArray = new byte[16];
                        Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                        var descLen = BitConverter.ToInt32(stringLenArray, 0);

                        var descBytes = new byte[descLen - 4];
                        Array.Copy(credentialArray, 60, descBytes, 0, descBytes.Length);

                        var desc = Encoding.Unicode.GetString(descBytes);

                        CredentialFile systemCredential = new CredentialFile();
                        systemCredential.FileName = Path.GetFileName(file);
                        systemCredential.Description = desc;
                        systemCredential.GUIDMasterKey = guidMasterKey;
                        systemCredential.LastAccessed = File.GetLastAccessTime(file);
                        systemCredential.LastModified = File.GetLastAccessTime(file);
                        systemCredential.Size = size;
                        SystemCredentials.Add(systemCredential);
                    }

                    yield return new WindowsCredentialFileDTO()
                    {
                        Folder = systemFolder,
                        CredentialFiles = SystemCredentials
                    };
                }
            }
        }

        internal class WindowsCredentialFileDTO : CommandDTOBase
        {
            public string Folder { get; set; }
            public List<CredentialFile> CredentialFiles { get; set; } = new List<CredentialFile>();
        }

        [CommandOutputType(typeof(WindowsCredentialFileDTO))]
        internal class WindowsCredentialFileFormatter : TextFormatterBase
        {
            public WindowsCredentialFileFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (WindowsCredentialFileDTO)result;

                WriteLine("  Folder : {0}\n", dto.Folder);

                foreach (CredentialFile credentialFile in dto.CredentialFiles)
                {
                    WriteLine("    FileName     : {0}", credentialFile.FileName);
                    WriteLine("    Description  : {0}", credentialFile.Description);
                    WriteLine("    MasterKey    : {0}", credentialFile.GUIDMasterKey);
                    WriteLine("    Accessed     : {0}", credentialFile.LastAccessed);
                    WriteLine("    Modified     : {0}", credentialFile.LastModified);
                    WriteLine("    Size         : {0}\n", credentialFile.Size);
                }

                WriteLine();
            }
        }
    }
}
#nullable enable