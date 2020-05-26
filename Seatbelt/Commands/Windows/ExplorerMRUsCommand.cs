#nullable disable
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Seatbelt.Interop;


namespace Seatbelt.Commands.Windows
{
    internal class ExplorerMRUsCommand : CommandBase
    {
        public override string Command => "ExplorerMRUs";
        public override string Description => "Explorer most recently used files (last 7 days, argument == last X days)";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;


        public ExplorerMRUsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var lastDays = 7;

            // parses recent file shortcuts via COM
            if (args.Length == 1)
            {
                lastDays = int.Parse(args[0]);
            }
            else if (!Runtime.FilterResults)
            {
                lastDays = 30;
            }

            foreach (var file in EnumRecentExplorerFiles(lastDays).OrderByDescending(e => ((ExplorerRecentFilesDTO)e).LastAccessDate))
            {
                yield return file;
            }
        }

        private IEnumerable<CommandDTOBase> EnumRecentExplorerFiles(int lastDays)
        {
            var startTime = DateTime.Now.AddDays(-lastDays);
            object shellObj = null;

            try
            {
                // WshShell COM object GUID 
                var shell = Type.GetTypeFromCLSID(new Guid("F935DC22-1CF0-11d0-ADB9-00C04FD58A0B"));
                shellObj = Activator.CreateInstance(shell);

                var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
                var dirs = Directory.GetDirectories(userFolder);
                foreach (var userDir in dirs)
                {
                    if (userDir.EndsWith("Public") || userDir.EndsWith("Default") || userDir.EndsWith("Default User") ||
                        userDir.EndsWith("All Users"))
                    {
                        continue;
                    }

                    string userName = null;
                    try
                    {
                        userName = File.GetAccessControl($"{userDir}\\NTUSER.DAT")
                            .GetOwner(typeof(SecurityIdentifier))
                            .ToString();
                        userName = Advapi32.TranslateSid(userName);
                    }
                    catch
                    {
                        var parts = userDir.Split('\\');
                        userName = parts[parts.Length - 1];
                    }

                    var recentPath = $"{userDir}\\AppData\\Roaming\\Microsoft\\Windows\\Recent\\";

                    string[] recentFiles = null;

                    try
                    {
                        recentFiles = Directory.GetFiles(recentPath, "*.lnk", SearchOption.AllDirectories);
                    }
                    catch
                    {
                        continue;
                    }

                    if (recentFiles.Length == 0) continue;
                    foreach (var recentFile in recentFiles)
                    {
                        var lastAccessed = File.GetLastAccessTime(recentFile);

                        if (lastAccessed <= startTime)
                        {
                            continue;
                        }

                        // invoke the WshShell com object, creating a shortcut to then extract the TargetPath from
                        var shortcut = shellObj.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shellObj, new object[] { recentFile });
                        var targetPath = (string)shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, new object[] { });

                        if (targetPath.Trim() == "")
                        {
                            continue;
                        }

                        yield return new ExplorerRecentFilesDTO()
                        {
                            Application = "Explorer",
                            User = userName,
                            Target = targetPath,
                            LastAccessDate = lastAccessed
                        };
                    }
                }
            }
            finally
            {
                if (shellObj != null)
                {
                    Marshal.ReleaseComObject(shellObj);
                }
            }
        }
    }

    internal class ExplorerRecentFilesDTO : CommandDTOBase
    {
        public string Application { get; set; }
        public string Target { get; set; }
        public DateTime LastAccessDate { get; set; }
        public string User { get; set; }
    }

    [CommandOutputType(typeof(ExplorerRecentFilesDTO))]
    internal class ExplorerRecentFileTextFormatter : TextFormatterBase
    {
        public ExplorerRecentFileTextFormatter(ITextWriter sink) : base(sink)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ExplorerRecentFilesDTO)result;

            WriteLine("  {0}  {1}  {2}  {3}", dto.Application, dto.User, dto.LastAccessDate.ToString("yyyy-MM-dd"), dto.Target);
        }
    }
}
#nullable enable