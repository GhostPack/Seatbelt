using System;
using System.IO;
using System.Text;

namespace Seatbelt.Probes.UserChecks
{
    public class CredFiles : IProbe
    {
        public static string ProbeName => "CredFiles";

        public string List()
        {
            var sb = new StringBuilder(8192);

            try
            {
                if (Helpers.IsHighIntegrity())
                {
                    RunForHighIntegrity(sb);
                }
                else
                {
                    RunForOtherIntegrity(sb);
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Credential Files (All Users)");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
            var found = false;

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];

                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users")) continue;

                var userCredFilePath = String.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\", dir);
                if (!Directory.Exists(userCredFilePath)) continue;

                var systemFiles = Directory.GetFiles(userCredFilePath);
                if ((systemFiles != null) && (systemFiles.Length != 0))
                {
                    sb.AppendLine();
                    sb.AppendLine($"    Folder       : {userCredFilePath}");
                    sb.AppendLine();

                    foreach (var file in systemFiles)
                    {
                        var lastAccessed = File.GetLastAccessTime(file);
                        var lastModified = File.GetLastWriteTime(file);
                        var size = new FileInfo(file).Length;
                        var fileName = Path.GetFileName(file);
                        found = true;

                        sb.AppendLine($"    CredFile     : {fileName}");

                        var credentialArray = File.ReadAllBytes(file);
                        var guidMasterKeyArray = new byte[16];
                        Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                        var guidMasterKey = new Guid(guidMasterKeyArray);

                        var stringLenArray = new byte[16];
                        Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                        var descLen = BitConverter.ToInt32(stringLenArray, 0);

                        var descBytes = new byte[descLen];
                        Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                        var desc = Encoding.Unicode.GetString(descBytes).TrimEnd('\0');
                        sb.AppendLine($"    Description  : {desc}");
                        sb.AppendLine($"    MasterKey    : {guidMasterKey.ToString()}");
                        sb.AppendLine($"    Accessed     : {lastAccessed}");
                        sb.AppendLine($"    Modified     : {lastModified}");
                        sb.AppendLine($"    Size         : {size}\r\n");
                        sb.AppendLine();
                    }
                }
            }

            var systemFolder = String.Format("{0}\\System32\\config\\systemprofile\\AppData\\Local\\Microsoft\\Credentials", Environment.GetEnvironmentVariable("SystemRoot"));
            var files = Directory.GetFiles(systemFolder);

            if ((files != null) && (files.Length != 0))
            {
                sb.AppendLine();
                sb.AppendLine($"    Folder       : {systemFolder}");
                sb.AppendLine();


                foreach (var file in files)
                {
                    var lastAccessed = File.GetLastAccessTime(file);
                    var lastModified = File.GetLastWriteTime(file);
                    var size = new FileInfo(file).Length;
                    var fileName = Path.GetFileName(file);
                    found = true;

                    sb.AppendLine($"    CredFile     : {fileName}");

                    var credentialArray = File.ReadAllBytes(file);
                    var guidMasterKeyArray = new byte[16];
                    Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                    var guidMasterKey = new Guid(guidMasterKeyArray);

                    var stringLenArray = new byte[16];
                    Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                    var descLen = BitConverter.ToInt32(stringLenArray, 0);

                    var descBytes = new byte[descLen];
                    Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                    var desc = Encoding.Unicode.GetString(descBytes).TrimEnd('\0');
                    sb.AppendLine($"    Description  : {desc}");
                    sb.AppendLine($"    MasterKey    : {guidMasterKey.ToString()}");
                    sb.AppendLine($"    Accessed     : {lastAccessed}");
                    sb.AppendLine($"    Modified     : {lastModified}");
                    sb.AppendLine($"    Size         : {size}");
                    sb.AppendLine();
                }
            }

            if (found)
            {
                sb.AppendLine("  [*] Use the Mimikatz \"dpapi::cred\" module with appropriate /masterkey to decrypt");
                sb.AppendLine("  [*] You can extract many DPAPI masterkeys from memory with the Mimikatz \"sekurlsa::dpapi\" module");
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Credential Files (Current User)");

            var userName = Environment.GetEnvironmentVariable("USERNAME");
            var userCredFilePath = String.Format("{0}\\AppData\\Local\\Microsoft\\Credentials\\",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            var found = false;

            if (Directory.Exists(userCredFilePath))
            {
                var files = Directory.GetFiles(userCredFilePath);
                sb.AppendLine($"    Folder       : {userCredFilePath}");
                sb.AppendLine();

                foreach (var file in files)
                {
                    var lastAccessed = File.GetLastAccessTime(file);
                    var lastModified = File.GetLastWriteTime(file);
                    var size = new FileInfo(file).Length;
                    var fileName = Path.GetFileName(file);
                    found = true;

                    sb.AppendLine($"    CredFile     : {fileName}");

                    var credentialArray = File.ReadAllBytes(file);
                    var guidMasterKeyArray = new byte[16];
                    Array.Copy(credentialArray, 36, guidMasterKeyArray, 0, 16);
                    var guidMasterKey = new Guid(guidMasterKeyArray);

                    var stringLenArray = new byte[16];
                    Array.Copy(credentialArray, 56, stringLenArray, 0, 4);
                    var descLen = BitConverter.ToInt32(stringLenArray, 0);

                    var descBytes = new byte[descLen];
                    Array.Copy(credentialArray, 60, descBytes, 0, descLen - 4);

                    var desc = Encoding.Unicode.GetString(descBytes).TrimEnd('\0');
                    sb.AppendLine($"    Description  : {desc}");
                    sb.AppendLine($"    MasterKey    : {guidMasterKey.ToString()}");
                    sb.AppendLine($"    Accessed     : {lastAccessed}");
                    sb.AppendLine($"    Modified     : {lastModified}");
                    sb.AppendLine($"    Size         : {size}");
                    sb.AppendLine();
                }
            }

            if (found)
            {
                sb.AppendLine("  [*] Use the Mimikatz \"dpapi::cred\" module with appropriate /masterkey to decrypt");
            }
        }
    }
}
