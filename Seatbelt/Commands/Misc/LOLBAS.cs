using System.Linq;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

namespace Seatbelt.Commands
{
    internal class LolbasCommand : CommandBase
    {
        public override string Command => "LOLBAS";
        public override string Description => "Locates Living Off The Land Binaries and Scripts (LOLBAS) on the system. Note: takes non-trivial time.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };              
        public override bool SupportRemote => false;

        // Contains all lolbas found from https://lolbas-project.github.io/
        // Obtained on 8/15/20
        private static readonly HashSet<string> Lolbas = new HashSet<string>(){
            "advpack.dll", "appvlp.exe", "at.exe", 
            "atbroker.exe", "bash.exe", "bginfo.exe", 
            "bitsadmin.exe", "cl_invocation.ps1", "cl_mutexverifiers.ps1", 
            "cdb.exe", "certutil.exe", "cmd.exe", 
            "cmdkey.exe", "cmstp.exe", "comsvcs.dll", 
            "control.exe", "csc.exe", "cscript.exe", 
            "desktopimgdownldr.exe", "devtoolslauncher.exe", "dfsvc.exe", 
            "diskshadow.exe", "dnscmd.exe", "dotnet.exe", 
            "dxcap.exe", "esentutl.exe", "eventvwr.exe", 
            "excel.exe", "expand.exe", "extexport.exe", 
            "extrac32.exe", "findstr.exe", "forfiles.exe", 
            "ftp.exe", "gfxdownloadwrapper.exe", "gpscript.exe", 
            "hh.exe", "ie4uinit.exe", "ieadvpack.dll", 
            "ieaframe.dll", "ieexec.exe", "ilasm.exe", 
            "infdefaultinstall.exe", "installutil.exe", "jsc.exe", 
            "makecab.exe", "manage-bde.wsf", "mavinject.exe", 
            "mftrace.exe", "microsoft.workflow.compiler.exe", "mmc.exe", 
            "msbuild.exe", "msconfig.exe", "msdeploy.exe", 
            "msdt.exe", "mshta.exe", "mshtml.dll", 
            "msiexec.exe", "netsh.exe", "odbcconf.exe", 
            "pcalua.exe", "pcwrun.exe", "pcwutl.dll", 
            "pester.bat", "powerpnt.exe", "presentationhost.exe", 
            "print.exe", "psr.exe", "pubprn.vbs", 
            "rasautou.exe", "reg.exe", "regasm.exe", 
            "regedit.exe", "regini.exe", "register-cimprovider.exe", 
            "regsvcs.exe", "regsvr32.exe", "replace.exe", 
            "rpcping.exe", "rundll32.exe", "runonce.exe", 
            "runscripthelper.exe", "sqltoolsps.exe", "sc.exe", 
            "schtasks.exe", "scriptrunner.exe", "setupapi.dll", 
            "shdocvw.dll", "shell32.dll", "slmgr.vbs", 
            "sqldumper.exe", "sqlps.exe", "squirrel.exe", 
            "syncappvpublishingserver.exe", "syncappvpublishingserver.vbs", "syssetup.dll", 
            "tracker.exe", "tttracer.exe", "update.exe", 
            "url.dll", "verclsid.exe", "wab.exe", 
            "winword.exe", "wmic.exe", "wscript.exe", 
            "wsl.exe", "wsreset.exe", "xwizard.exe", 
            "zipfldr.dll", "csi.exe", "dnx.exe", 
            "msxsl.exe", "ntdsutil.exe", "rcsi.exe", 
            "te.exe", "vbc.exe", "vsjitdebugger.exe", 
            "winrm.vbs", };

        public LolbasCommand(Runtime runtime) : base(runtime)
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
            IEnumerable<string> query = paths.Where(path => Lolbas.Contains(path.Substring(path.LastIndexOf("\\") + 1).ToLower())).OrderBy(path => path.Substring(path.LastIndexOf("\\") + 1));
          
            foreach(string path in query)
            {
                yield return new LolbasDTO(path);
            }

            // This is only the LOLBAS with a .wsf extension so we can simply just check if the file exists
            string manageBDE = @"C:\Windows\System32\manage-bde.wsf";

            if (System.IO.File.Exists(manageBDE))
            {
                yield return new LolbasDTO(manageBDE);
            }

            WriteVerbose($"Found: {query.Count()} LOLBAS");
            WriteHost("\nTo see how to use the LOLBAS that were found go to https://lolbas-project.github.io/");
        }

        internal class LolbasDTO : CommandDTOBase
        {
            public LolbasDTO(string path)
            {
                Path = path;
            }
            public string Path { get; set; }
        }

        [CommandOutputType(typeof(LolbasDTO))]
        internal class LolbasFormatter : TextFormatterBase
        {
            public LolbasFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (LolbasDTO)result;
                WriteLine($"Path: {dto.Path}");
            }
            
        }
    }
}