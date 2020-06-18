using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text.RegularExpressions;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Seatbelt.Util;


namespace Seatbelt.Commands.Windows
{
    class Module
    {
        public Module(string moduleName, string moduleFileName, string moduleFileDescription, string moduleOriginalFilename, string moduleCompanyName)
        {
            ModuleName = moduleName;
            ModuleFileName = moduleFileName;
            ModuleFileDescription = moduleFileDescription;
            ModuleOriginalFilename = moduleOriginalFilename;
            ModuleCompanyName = moduleCompanyName;  
        }
        public string ModuleName { get; set; }
        public string ModuleFileName { get; set; }
        public string ModuleFileDescription { get; set; }
        public string ModuleOriginalFilename { get; set; }
        public string ModuleCompanyName { get; set; }
    }

    internal class ProcessesCommand : CommandBase
    {
        public override string Command => "Processes";
        public override string Description => "Running processes with file info company names that don't contain 'Microsoft', \"-full\" enumerates all processes";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // other local Process" stuff prevents this

        public ProcessesCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists currently running processes that don't have "Microsoft Corporation" as the company name in their file info
            //      or all processes if "-full" is passed

            var enumerateModules = false;
            if (args.Length == 1 && args[0].ToLower().Equals("modules"))
            {
                enumerateModules = true;
            }

            WriteHost(Runtime.FilterResults
                ? "Collecting Non Microsoft Processes (via WMI)\n"
                : "Collecting All Processes (via WMI)\n");

            var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (var results = searcher.Get())
                {
                    var query = from p in Process.GetProcesses()
                                join mo in results.Cast<ManagementObject>()
                                    on p.Id equals (int)(uint)mo["ProcessId"]
                                select new
                                {
                                    Process = p,
                                    Path = (string)mo["ExecutablePath"],
                                    CommandLine = (string)mo["CommandLine"],
                                };
                    foreach (var item in query)
                    {
                        var isDotNet = false;
                        string? companyName = null;

                        if (item.Path != null)
                        {
                            isDotNet = FileUtil.IsDotNetAssembly(item.Path);

                            try
                            {
                                var myFileVersionInfo = FileVersionInfo.GetVersionInfo(item.Path);
                                companyName = myFileVersionInfo.CompanyName;
                            } catch
                            {
                                companyName = null;
                            }
                        }

                        if (Runtime.FilterResults)
                        {
                            if (companyName == null || string.IsNullOrEmpty(companyName.Trim()) ||
                                (companyName != null &&
                                 Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                            {
                                continue;
                            }
                        }

                        var processModules = new List<Module>();
                        if (enumerateModules)
                        {
                            try
                            {
                                var modules = item.Process.Modules;
                                foreach (ProcessModule module in modules)
                                {
                                    var ProcessModule = new Module(
                                        module.ModuleName,
                                        module.FileVersionInfo.FileName,
                                        module.FileVersionInfo.FileDescription,
                                        module.FileVersionInfo.OriginalFilename,
                                        module.FileVersionInfo.CompanyName
                                    );
                                    processModules.Add(ProcessModule);
                                }
                            }
                            catch
                            {
                                // eat it
                            }
                        }

                        yield return new ProcessesDTO(
                            item.Process.ProcessName,
                            companyName,
                            item.Process.Id,
                            item.Path,
                            item.CommandLine,
                            isDotNet,
                            processModules
                        );
                    }
                }
            }
        }
    }

    internal class ProcessesDTO : CommandDTOBase
    {
        public ProcessesDTO(string processName, string? companyName, int processId, string? path, string commandLine, bool? isDotNet, List<Module> modules)
        {
            ProcessName = processName;
            CompanyName = companyName;
            ProcessId = processId;
            Path = path;
            CommandLine = commandLine;
            IsDotNet = isDotNet;
            Modules = modules;  
        }
        public string ProcessName { get; set; }
        public string? CompanyName { get; set; }
        public int ProcessId { get; set; }
        public string? Path { get; set; }
        public string CommandLine { get; set; }
        public bool? IsDotNet { get; set; }
        public List<Module> Modules { get; set; }
    }

    [CommandOutputType(typeof(ProcessesDTO))]
    internal class ProcessFormatter : TextFormatterBase
    {
        public ProcessFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ProcessesDTO)result;

            WriteLine(" {0,-40} : {1}", "ProcessName", dto.ProcessName);
            WriteLine(" {0,-40} : {1}", "CompanyName", dto.CompanyName);
            WriteLine(" {0,-40} : {1}", "ProcessId", dto.ProcessId);
            WriteLine(" {0,-40} : {1}", "Path", dto.Path);
            WriteLine(" {0,-40} : {1}", "CommandLine", dto.CommandLine);
            WriteLine(" {0,-40} : {1}", "IsDotNet", dto.IsDotNet);

            if (dto.Modules.Count != 0)
            {
                WriteLine(" {0,-40} :", "Modules");
                foreach (var module in dto.Modules)
                {
                    if (!filterResults || String.IsNullOrEmpty(module.ModuleCompanyName) || !Regex.IsMatch(module.ModuleCompanyName, @"^Microsoft.*", RegexOptions.IgnoreCase))
                    {
                        WriteLine(" {0,40} : {1}", "Name", module.ModuleName);
                        WriteLine(" {0,40} : {1}", "CompanyName", module.ModuleCompanyName);
                        WriteLine(" {0,40} : {1}", "FileName", module.ModuleFileName);
                        WriteLine(" {0,40} : {1}", "OriginalFileName", module.ModuleOriginalFilename);
                        WriteLine(" {0,40} : {1}\n", "FileDescription", module.ModuleFileDescription);
                    }
                }
            }
            WriteLine();
        }
    }
}
