using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class CredentialFileInfo
    {
        public CredentialFileInfo(string fileName, string description, Guid guidMasterKey, DateTime lastAccessed, DateTime lastModified, long size)
        {
            FileName = fileName;
            Description = description;
            GuidMasterKey = guidMasterKey;
            LastAccessed = lastAccessed;
            LastModified = lastModified;
            Size = size;    
        }

        public string FileName { get; set; }

        public string Description { get; set; }

        public Guid GuidMasterKey { get; set; }

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
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";

            var credentialFolders = new List<string>()
            {
                $"{systemRoot}\\System32\\config\\systemprofile\\AppData\\Local\\Microsoft\\Credentials",
                $"{systemRoot}\\System32\\config\\systemprofile\\AppData\\Roaming\\Microsoft\\Credentials",
                $"{systemRoot}\\ServiceProfiles\\LocalService\\AppData\\Local\\Microsoft\\Credentials",
                $"{systemRoot}\\ServiceProfiles\\LocalService\\AppData\\Roaming\\Microsoft\\Credentials",
                $"{systemRoot}\\ServiceProfiles\\NetworkService\\AppData\\Local\\Microsoft\\Credentials",
                $"{systemRoot}\\ServiceProfiles\\NetworkService\\AppData\\Roaming\\Microsoft\\Credentials"
            };

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                credentialFolders.Add($"{dir}\\AppData\\Local\\Microsoft\\Credentials\\");
                credentialFolders.Add($"{dir}\\AppData\\Roaming\\Microsoft\\Credentials\\");
            };

            foreach (var credPath in credentialFolders)
            {
                foreach (var commandDtoBase in GetCredentialsFromDirectory(credPath))
                    yield return commandDtoBase;
            }
        }

        private IEnumerable<CommandDTOBase> GetCredentialsFromDirectory(string credPath)
        {
            if (!Directory.Exists(credPath))
                yield break;

            var userFiles = Directory.GetFiles(credPath);
            if (userFiles.Length == 0)
                yield break;

            var userCredentials = userFiles.Select(CredentialFile).ToList();

            yield return new WindowsCredentialFileDTO(credPath, userCredentials);
        }

        private CredentialFileInfo CredentialFile(string file)
        {
            var size = new FileInfo(file).Length;

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

            var cred = new CredentialFileInfo(
                Path.GetFileName(file),
                desc,
                guidMasterKey,
                File.GetLastAccessTime(file),
                File.GetLastAccessTime(file),
                size
            );

            return cred;
        }
    }

    internal class WindowsCredentialFileDTO : CommandDTOBase
    {
        public WindowsCredentialFileDTO(string folder, List<CredentialFileInfo> credentials)
        {
            Folder = folder;
            CredentialInfo = credentials;
        }
        public string Folder { get; set; }
        public List<CredentialFileInfo> CredentialInfo { get; }
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

            foreach (var credentialFile in dto.CredentialInfo)
            {
                WriteLine("    FileName     : {0}", credentialFile.FileName);
                WriteLine("    Description  : {0}", credentialFile.Description);
                WriteLine("    MasterKey    : {0}", credentialFile.GuidMasterKey);
                WriteLine("    Accessed     : {0}", credentialFile.LastAccessed);
                WriteLine("    Modified     : {0}", credentialFile.LastModified);
                WriteLine("    Size         : {0}\n", credentialFile.Size);
            }

            WriteLine();
        }
    }
}
