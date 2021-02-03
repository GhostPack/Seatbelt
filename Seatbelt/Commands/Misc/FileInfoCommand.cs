using Seatbelt.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.AccessControl;

namespace Seatbelt.Commands.Windows
{
    // TODO: Include filetype custom properties (e.g. Author and last-saved by fields for Word docs)
    internal class FileInfoCommand : CommandBase
    {
        public override string Command => "FileInfo";
        public override string Description => "Information about a file (version information, timestamps, basic PE info, etc. argument(s) == file path(s)";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false;
        public FileInfoCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (args.Length == 0)
            {
                var WinDir = Environment.GetEnvironmentVariable("WINDIR");
                if (!Runtime.FilterResults)
                {
                    // Oct. 2018 - File list partially taken from https://github.com/rasta-mouse/Watson/tree/04f029aa35c360a8a49c7b162d51604253996eb6/Watson/SuspectFiles
                    args = new string[]
                    {
                        $"{WinDir}\\system32\\drivers\\afd.sys",
                        $"{WinDir}\\system32\\coremessaging.dll",
                        $"{WinDir}\\system32\\dssvc.dll",
                        $"{WinDir}\\system32\\gdiplus.dll",
                        $"{WinDir}\\system32\\gpprefcl.dll",
                        $"{WinDir}\\system32\\drivers\\mrxdav.sys",
                        $"{WinDir}\\system32\\ntoskrnl.exe",
                        $"{WinDir}\\system32\\pcadm.dll",
                        $"{WinDir}\\system32\\rpcrt4.dll",
                        $"{WinDir}\\system32\\schedsvc.dll",
                        $"{WinDir}\\system32\\seclogon.dll",
                        $"{WinDir}\\system32\\win32k.sys",
                        $"{WinDir}\\system32\\win32kfull.sys",
                        $"{WinDir}\\system32\\winload.exe",
                        $"{WinDir}\\system32\\winsrv.dll",
                    };
                }
                else
                {
                    args = new string[]
                    {
                        $"{WinDir}\\system32\\ntoskrnl.exe"
                    };
                }
            }

            foreach (var file in args)
            {
                if (File.Exists(file) && (File.GetAttributes(file) & FileAttributes.Directory) != FileAttributes.Directory) // If file is not a directory
                {
                    FileVersionInfo versionInfo;
                    FileInfo fileInfo;
                    FileSecurity security;

                    try
                    {
                        versionInfo = FileVersionInfo.GetVersionInfo(file);
                        fileInfo = new FileInfo(file);
                        security = File.GetAccessControl(file);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FileNotFoundException || ex is SystemException)
                        {
                            WriteError($"  [!] Could not locate {file}\n");
                        }
                        else if (ex is SecurityException || ex is UnauthorizedAccessException)
                        {
                            WriteError($"  [!] Insufficient privileges to access {file}\n");
                        }
                        else if (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                        {
                            WriteError($"  [!] Path \"{file}\" is an invalid format\n");
                        }
                        else
                        {
                            WriteError($"  [!] Error accessing {file}\n");
                        }
                        continue;
                    }

                    if (versionInfo != null && fileInfo != null && security != null)  // TODO: Account for cases when any of these aren't null 
                    {
                        var isDotNet = FileUtil.IsDotNetAssembly(file);

                        yield return new FileInfoDTO(
                            versionInfo.Comments,
                            versionInfo.CompanyName,
                            versionInfo.FileDescription,
                            versionInfo.FileName,
                            versionInfo.FileVersion,
                            versionInfo.InternalName,
                            versionInfo.IsDebug,
                            isDotNet,
                            versionInfo.IsPatched,
                            versionInfo.IsPreRelease,
                            versionInfo.IsPrivateBuild,
                            versionInfo.IsSpecialBuild,
                            versionInfo.Language,
                            versionInfo.LegalCopyright,
                            versionInfo.LegalTrademarks,
                            versionInfo.OriginalFilename,
                            versionInfo.PrivateBuild,
                            versionInfo.ProductName,
                            versionInfo.ProductVersion,
                            versionInfo.SpecialBuild,
                            fileInfo.Attributes,
                            fileInfo.CreationTimeUtc,
                            fileInfo.LastAccessTimeUtc,
                            fileInfo.LastWriteTimeUtc,
                            fileInfo.Length,
                            security.GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner)
                        );
                    }
                }

                else // Target is a directory
                {
                    DirectorySecurity directorySecurity;

                    try
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(file);
                        directorySecurity = directoryInfo.GetAccessControl();
                    }
                    catch (Exception ex)
                    {
                        if (ex is FileNotFoundException || ex is SystemException)
                        {
                            WriteError($"  [!] Could not locate {file}\n");
                        }
                        else if (ex is SecurityException || ex is UnauthorizedAccessException)
                        {
                            WriteError($"  [!] Insufficient privileges to access {file}\n");
                        }
                        else if (ex is ArgumentException || ex is PathTooLongException)
                        {
                            WriteError($"  [!] Path \"{file}\" is an invalid format\n");
                        }
                        else
                        {
                            WriteError($"  [!] Error accessing {file}\n");
                        }
                        continue;
                    }

                    if (directorySecurity != null)  // TODO: Account for cases when any of these aren't null 
                    {

                        yield return new DirectoryInfoDTO(
                            FileAttributes.Directory,
                            Directory.GetCreationTimeUtc(file),
                            Directory.GetLastAccessTimeUtc(file),
                            Directory.GetLastWriteTimeUtc(file),
                            directorySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner)
                        );
                    }
                }
            }
        }
    }

    internal class FileInfoDTO : CommandDTOBase
    {
        public FileInfoDTO(string comments, string companyName, string fileDescription, string fileName, string fileVersion, string internalName, bool isDebug, bool isDotNet, bool isPatched, bool isPreRelease, bool isPrivateBuild, bool isSpecialBuild, string language, string legalCopyright, string legalTrademarks, string originalFilename, string privateBuild, string productName, string productVersion, string specialBuild, FileAttributes attributes, DateTime creationTimeUtc, DateTime lastAccessTimeUtc, DateTime lastWriteTimeUtc, long length, string sddl)
        {
            Comments = comments;
            CompanyName = companyName;
            FileDescription = fileDescription;
            FileName = fileName;
            FileVersion = fileVersion;
            InternalName = internalName;
            IsDebug = isDebug;
            IsDotNet = isDotNet;
            IsPatched = isPatched;
            IsPreRelease = isPreRelease;
            IsPrivateBuild = isPrivateBuild;
            IsSpecialBuild = isSpecialBuild;
            Language = language;
            LegalCopyright = legalCopyright;
            LegalTrademarks = legalTrademarks;
            OriginalFilename = originalFilename;
            PrivateBuild = privateBuild;
            ProductName = productName;
            ProductVersion = productVersion;
            SpecialBuild = specialBuild;
            Attributes = attributes;
            CreationTimeUtc = creationTimeUtc;
            LastAccessTimeUtc = lastAccessTimeUtc;
            LastWriteTimeUtc = lastWriteTimeUtc;
            Length = length;
            SDDL = sddl;
        }

        public string Comments { get; }
        public string CompanyName { get; }
        public string FileDescription { get; }
        public string FileName { get; }
        public string FileVersion { get; }
        public string InternalName { get; }
        public bool IsDebug { get; }
        public bool IsDotNet { get; }
        public bool IsPatched { get; }
        public bool IsPreRelease { get; }
        public bool IsPrivateBuild { get; }
        public bool IsSpecialBuild { get; }
        public string Language { get; }
        public string LegalCopyright { get; }
        public string LegalTrademarks { get; }
        public string OriginalFilename { get; }
        public string PrivateBuild { get; }
        public string ProductName { get; }
        public string ProductVersion { get; }
        public string SpecialBuild { get; }
        public FileAttributes Attributes { get; }
        public DateTime CreationTimeUtc { get; }
        public DateTime LastAccessTimeUtc { get; }
        public DateTime LastWriteTimeUtc { get; }
        public long Length { get; }
        public string SDDL { get; }
}

    internal class DirectoryInfoDTO : CommandDTOBase
    {
        public DirectoryInfoDTO(FileAttributes attributes, DateTime creationTimeUtc, DateTime lastAccessTimeUtc, DateTime lastWriteTimeUtc, string sddl)
        {
            Attributes = attributes;
            CreationTimeUtc = creationTimeUtc;
            LastAccessTimeUtc = lastAccessTimeUtc;
            LastWriteTimeUtc = lastWriteTimeUtc;
            SDDL = sddl;
        }

        public FileAttributes Attributes { get; }
        public DateTime CreationTimeUtc { get; }
        public DateTime LastAccessTimeUtc { get; }
        public DateTime LastWriteTimeUtc { get; }
        public string SDDL { get; }
    }
}