#nullable disable   // Temporary - Need to fix nullable type issues
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


namespace Seatbelt.Commands.Windows
{
    class Module
    {
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
                        string companyName = null;

                        if (item.Path != null)
                        {
                            try
                            {
                                var myAssemblyName = AssemblyName.GetAssemblyName(item.Path);
                                isDotNet = true;
                            }
                            catch (FileNotFoundException)
                            {
                                // WriteHost("The file cannot be found.");
                            }
                            catch (BadImageFormatException exception)
                            {
                                if (Regex.IsMatch(exception.Message,
                                    ".*This assembly is built by a runtime newer than the currently loaded runtime and cannot be loaded.*",
                                    RegexOptions.IgnoreCase))
                                {
                                    isDotNet = true;
                                }
                            }
                            catch
                            {
                                // WriteHost("The assembly has already been loaded.");
                            }

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
                            if (string.IsNullOrEmpty(companyName) ||
                                (companyName != null &&
                                 Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                            {
                                continue;
                            }
                        }

                        var ProcessModules = new List<Module>();
                        if (enumerateModules)
                        {
                            try
                            {
                                var modules = item.Process.Modules;
                                foreach (ProcessModule module in modules)
                                {
                                    var ProcessModule = new Module()
                                    {
                                        ModuleName = module.ModuleName,
                                        ModuleFileName = module.FileVersionInfo.FileName,
                                        ModuleFileDescription = module.FileVersionInfo.FileDescription,
                                        ModuleOriginalFilename = module.FileVersionInfo.OriginalFilename,
                                        ModuleCompanyName = module.FileVersionInfo.CompanyName
                                    };
                                    ProcessModules.Add(ProcessModule);
                                }
                            }
                            catch
                            {
                                // eat it
                            }
                        }

                        yield return new ProcessesDTO()
                        {
                            ProcessName = item.Process.ProcessName,
                            CompanyName = companyName,
                            ProcessId = item.Process.Id,
                            Path = item.Path,
                            CommandLine = item.CommandLine,
                            IsDotNet = isDotNet,
                            Modules = ProcessModules
                        };
                    }
                }
            }
        }
    }

    internal class ProcessesDTO : CommandDTOBase
    {
        public string ProcessName { get; set; }
        public string CompanyName { get; set; }
        public int ProcessId { get; set; }
        public string Path { get; set; }
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
#nullable enable