#nullable disable
using System.Linq;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

namespace Seatbelt.Commands
{
    internal class HuntLolbasCommand : CommandBase
    {
        public override string Command => "HuntLolbas";
        public override string Description => "Locates Living Off The Land Binaries and Scripts (LOLBAS) on the system. Note: takes non-trivial time.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };              
        public override bool SupportRemote => false;

        // Contains all lolbas found from https://lolbas-project.github.io/
        // Obtained on 8/15/20
        private static readonly HashSet<string> Lolbas = new HashSet<string>(){
            "Advpack.dll", "Appvlp.exe", "At.exe",
            "Atbroker.exe", "Bash.exe", "Bginfo.exe",
            "Bitsadmin.exe", "CL_Invocation.ps1", "CL_Mutexverifiers.ps1",
            "Cdb.exe", "Certutil.exe", "Cmd.exe",
            "Cmdkey.exe", "Cmstp.exe", "Comsvcs.dll",
            "Control.exe", "Csc.exe", "Cscript.exe",
            "Desktopimgdownldr.exe", "Devtoolslauncher.exe", "Dfsvc.exe",
            "Diskshadow.exe", "Dnscmd.exe", "Dotnet.exe",
            "Dxcap.exe", "Esentutl.exe", "Eventvwr.exe",
            "Excel.exe", "Expand.exe", "Extexport.exe",
            "Extrac32.exe", "Findstr.exe", "Forfiles.exe",
            "Ftp.exe", "GfxDownloadWrapper.exe", "Gpscript.exe",
            "Hh.exe", "Ie4uinit.exe", "Ieadvpack.dll",
            "Ieaframe.dll", "Ieexec.exe", "Ilasm.exe",
            "Infdefaultinstall.exe", "Installutil.exe", "Jsc.exe",
            "Makecab.exe", "Manage-bde.wsf", "Mavinject.exe",
            "Mftrace.exe", "Microsoft.Workflow.Compiler.exe", "Mmc.exe",
            "Msbuild.exe", "Msconfig.exe", "Msdeploy.exe",
            "Msdt.exe", "Mshta.exe", "Mshtml.dll",
            "Msiexec.exe", "Netsh.exe", "Odbcconf.exe",
            "Pcalua.exe", "Pcwrun.exe", "Pcwutl.dll",
            "Pester.bat", "Powerpnt.exe", "Presentationhost.exe",
            "Print.exe", "Psr.exe", "Pubprn.vbs",
            "Rasautou.exe", "Reg.exe", "Regasm.exe",
            "Regedit.exe", "Regini.exe", "Register-cimprovider.exe",
            "Regsvcs.exe", "Regsvr32.exe", "Replace.exe",
            "Rpcping.exe", "Rundll32.exe", "Runonce.exe",
            "Runscripthelper.exe", "SQLToolsPS.exe", "Sc.exe",
            "Schtasks.exe", "Scriptrunner.exe", "Setupapi.dll",
            "Shdocvw.dll", "Shell32.dll", "Slmgr.vbs",
            "Sqldumper.exe", "Sqlps.exe", "Squirrel.exe",
            "SyncAppvPublishingServer.exe", "Syncappvpublishingserver.vbs", "Syssetup.dll",
            "Tracker.exe", "Tttracer.exe", "Update.exe",
            "Url.dll", "Verclsid.exe", "Wab.exe",
            "Winword.exe", "Wmic.exe", "Wscript.exe",
            "Wsl.exe", "Wsreset.exe", "Xwizard.exe",
            "Zipfldr.dll", "csi.exe", "dnx.exe",
            "msxsl.exe", "ntdsutil.exe", "rcsi.exe",
            "te.exe", "vbc.exe", "vsjitdebugger.exe",
            "winrm.vbs", };

        public HuntLolbasCommand(Runtime runtime) : base(runtime)
        {  
        }

        // FROM: https://stackoverflow.com/questions/172544/ignore-folders-files-when-directory-getfiles-is-denied-access
        // Reursively search for files with extensions of bat, exe, dll, ps1, and vbs from a top level root directory
        private static List<string> GetAllFilesFromFolder(string root, bool searchSubfolders)
        {
            Queue<string> folders = new Queue<string>();
            List<string> files = new List<string>();
            folders.Enqueue(root);
            while (folders.Count != 0)
            {
                string currentFolder = folders.Dequeue();
                try
                {
                    // FROM: https://stackoverflow.com/questions/8443524/using-directory-getfiles-with-a-regex-in-c
                    // If using .NET Framework 4+ can replace Directory.GetFiles with Directory.EnumerateFiles that should be faster
                    // See here for more info https://stackoverflow.com/questions/17756042/improve-the-performance-for-enumerating-files-and-folders-using-net
                    string[] filesInCurrent = System.IO.Directory.GetFiles(currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly).Where(path=> path.EndsWith(".bat") ||
                                                                                                                        path.EndsWith(".exe") || path.EndsWith(".dll") || path.EndsWith(".ps1") || path.EndsWith(".vbs")).ToArray();
                    files.AddRange(filesInCurrent);
                }
                catch
                {
                }
                try
                {
                    if (searchSubfolders)
                    {
                        string[] foldersInCurrent = System.IO.Directory.GetDirectories(currentFolder, "*.*", System.IO.SearchOption.TopDirectoryOnly);
                        foreach (string _current in foldersInCurrent)
                        {
                            folders.Enqueue(_current);
                        }
                    }
                }
                catch
                {
                }
            }
            return files;
        }

       
        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            List<string> paths = GetAllFilesFromFolder(@"C:\", true);  

            // For each path that was found check if the path is in the hashset and sort the result by path names 
            IEnumerable<string> query = paths.Where(path => Lolbas.Contains(path.Substring(path.LastIndexOf("\\") + 1))).OrderBy(path => path.Substring(path.LastIndexOf("\\") + 1));
          
            foreach(string path in query)
            {
                yield return new HuntLolbasDTO() { Path = path };
            }

            // This is only the LOLBAS with a .wsf extension so we can simply just check if the file exists
            string manageBDE = @"C:\Windows\System32\manage-bde.wsf";

            if (System.IO.File.Exists(manageBDE))
            {
                yield return new HuntLolbasDTO() { Path = manageBDE };
            }

            WriteVerbose($"Found: {query.Count()} LOLBAS");
            WriteHost("\nTo see how to use the LOLBAS that were found go to https://lolbas-project.github.io/");
        }

        internal class HuntLolbasDTO : CommandDTOBase
        {
            public string Path { get; set; }
        }

        [CommandOutputType(typeof(HuntLolbasDTO))]
        internal class HuntLolbasFormatter : TextFormatterBase
        {
            public HuntLolbasFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (HuntLolbasDTO)result;
                WriteLine($"Path: {dto.Path}");
            }
            
        }
    }
}
#nullable enable