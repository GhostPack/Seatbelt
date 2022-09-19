using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Seatbelt.Util;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Seatbelt.Interop;

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
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false; // other local Process" stuff prevents this

        public ProcessesCommand(Runtime runtime) : base(runtime)
        {
        }
        private int? GetProcessProtectionInfo(int ProcessId)
        {
            int pplValueString;
            IntPtr ProcessHandle = Kernel32.OpenProcess(Interop.Kernel32.ProcessAccess.QueryLimitedInformation, false, ProcessId);
            if (ProcessHandle == null)
            {
                WriteError($" [!] Could not get a handle to ProcessId " + ProcessId);
            }
            PsProtection ppl = new PsProtection();
            int returnlength;
            int status = Ntdll.NtQueryInformationProcess(ProcessHandle, PROCESSINFOCLASS.ProcessProtectionInformation, ref ppl, Marshal.SizeOf(ppl), out returnlength);
            if (status != 0)
            {
                WriteError($" [!] Could not get Process Protection Info for ProcessId " + ProcessId);
                var handleResult = Kernel32.CloseHandle(ProcessHandle);
                return null;
            }
            else
            {
                pplValueString = ((byte)ppl.Type | (byte)ppl.Audit | ((int)ppl.Signer) << 4);
                return pplValueString;
            }
        }
        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists currently running processes that don't have "Microsoft Corporation" as the company name in their file info
            //      or all processes if "-full" is passed

            var enumerateModules = args.Length == 1 && args[0].ToLower().Equals("modules");

            WriteHost(Runtime.FilterResults
                ? "Collecting Non Microsoft Processes (via WMI)\n"
                : "Collecting All Processes (via WMI)\n");

            var wmiQueryString = "SELECT ProcessId, ParentProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using var searcher = new ManagementObjectSearcher(wmiQueryString);
            using var results = searcher.Get();

            var query = from p in Process.GetProcesses()
                join mo in results.Cast<ManagementObject>()
                    on p.Id equals (int)(uint)mo["ProcessId"]
                select new
                {
                    Process = p,
                    ParentProcessId = (UInt32)mo["ParentProcessId"],
                    Path = (string)mo["ExecutablePath"],
                    CommandLine = (string)mo["CommandLine"],
                };
            
            foreach (var proc in query)
            {
                var isDotNet = false;
                string? companyName = null;
                string? description = null;
                string? version = null;
                int? ProtectionLevelinfo = null;

                if (!SecurityUtil.IsHighIntegrity() || proc.Process.Id == 0)
                {
                    ProtectionLevelinfo = null;
                }
                else
                {
                    ProtectionLevelinfo = GetProcessProtectionInfo(proc.Process.Id);
                }

                if (proc.Path != null)
                {
                    
                    isDotNet = FileUtil.IsDotNetAssembly(proc.Path);

                    try
                    {
                        var myFileVersionInfo = FileVersionInfo.GetVersionInfo(proc.Path);
                        companyName = myFileVersionInfo.CompanyName;
                        description = myFileVersionInfo.FileDescription;
                        version = myFileVersionInfo.FileVersion;
                    }
                    catch
                    {
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
                        var modules = proc.Process.Modules;
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
                    proc.Process.ProcessName,
                    proc.Process.Id,
                    (int)proc.ParentProcessId,
                    companyName,
                    description,
                    version,
                    proc.Path,
                    proc.CommandLine,
                    isDotNet,
                    processModules,
                    ProtectionLevelinfo
                );
            }
        }
    }

    internal class ProcessesDTO : CommandDTOBase
    {
        public ProcessesDTO(string processName, int processId, int parentProcessId, string? companyName, string? description, string? version, string? path, string commandLine, bool? isDotNet, List<Module> modules, int? ProtectionLevelinfo)
        {
            ProcessName = processName;
            ProcessId = processId;
            ParentProcessId = parentProcessId;
            CompanyName = companyName;
            Description = description;
            Version = version;
            Path = path;
            CommandLine = commandLine;
            IsDotNet = isDotNet;
            Modules = modules;
            ProcessProtectionLevelinfo = ProtectionLevelinfo;
        }
        public string ProcessName { get; set; }
        public string? CompanyName { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public int ProcessId { get; set; }
        public int ParentProcessId { get; set; }
        public string? Path { get; set; }
        public string CommandLine { get; set; }
        public bool? IsDotNet { get; set; }
        public List<Module> Modules { get; set; }
        public int? ProcessProtectionLevelinfo { get; set; }
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

            string? ProtectionLevelString;
            if (dto.ProcessProtectionLevelinfo == null)
            {
                ProtectionLevelString = null;
            }
            else
            {
                string pplValue = ((int)dto.ProcessProtectionLevelinfo).ToString("X");
                ProtectionValueName protectionLevel = (ProtectionValueName)Enum.Parse(typeof(ProtectionValueName), pplValue, true);
                string protectionValueName = protectionLevel.ToString();
                string pplValueHex = "(0x" + pplValue + ")";
                ProtectionLevelString = protectionValueName + pplValueHex;
            }

            WriteLine(" {0,-40} : {1}", "ProcessName", dto.ProcessName);
            WriteLine(" {0,-40} : {1}", "ProcessId", dto.ProcessId);
            WriteLine(" {0,-40} : {1}", "ParentProcessId", dto.ParentProcessId);
            WriteLine(" {0,-40} : {1}", "CompanyName", dto.CompanyName);
            WriteLine(" {0,-40} : {1}", "Description", dto.Description);
            WriteLine(" {0,-40} : {1}", "Version", dto.Version);
            WriteLine(" {0,-40} : {1}", "Path", dto.Path);
            WriteLine(" {0,-40} : {1}", "CommandLine", dto.CommandLine);
            WriteLine(" {0,-40} : {1}", "IsDotNet", dto.IsDotNet);
            WriteLine(" {0,-40} : {1}", "ProcessProtectionInformation", ProtectionLevelString);

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
