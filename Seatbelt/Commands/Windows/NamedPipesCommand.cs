#nullable disable
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using System.Security.AccessControl;
using static Seatbelt.Interop.Kernel32;
using System.IO;
using Seatbelt.Interop;
using System;

namespace Seatbelt.Commands.Windows
{
    internal class NamedPipesCommand : CommandBase
    {
        public override string Command => "NamedPipes";
        public override string Description => "Named pipe names, any readable ACL information and associated process information.";
        public override CommandGroup[] Group => new[] {CommandGroup.System};

        public override bool SupportRemote => false; // almost certainly not possible

        public NamedPipesCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists named pipes
            // reference - https://stackoverflow.com/questions/25109491/how-can-i-get-a-list-of-all-open-named-pipes-in-windows-and-avoiding-possible-ex/25126943#25126943
            var namedPipes = new List<string>();
            WIN32_FIND_DATA lpFindFileData;

            var ptr = FindFirstFile(@"\\.\pipe\*", out lpFindFileData);
            namedPipes.Add(lpFindFileData.cFileName);
            while (FindNextFile(ptr, out lpFindFileData))
            {
                namedPipes.Add(lpFindFileData.cFileName);
            }
            FindClose(ptr);

            namedPipes.Sort();

            foreach (var namedPipe in namedPipes)
            {
                string? svProcessPath = null;
                int? svProcessId = null;
                string? svProcessName = null;
                int? svSessionId = null;
                IntPtr hPipe = IntPtr.Zero;

                // Try to identify ProcessID and ProcessName
                try
                {
                    hPipe = CreateFile(
                        $"\\\\.\\pipe\\{namedPipe}",
                        FileAccess.Read,
                        FileShare.None,
                        IntPtr.Zero,
                        FileMode.Open,
                        FileAttributes.Normal,
                        IntPtr.Zero);


                    if (hPipe.ToInt64() != Win32Error.InvalidHandle)
                    {
                        bool bvRet = GetNamedPipeServerProcessId(
                            hPipe,
                            out int pipeServerPid);

                        try
                        {
                            if (bvRet)
                            {
                                var svProcess = System.Diagnostics.Process.GetProcessById(pipeServerPid);

                                svProcessId = pipeServerPid;
                                svProcessName = svProcess.ProcessName;
                                svProcessPath = svProcess.MainModule.FileName;
                            }
                        }
                        catch { }

                        bvRet = GetNamedPipeServerSessionId(
                            hPipe,
                            out int pipeServerSessionId);

                        if (bvRet)
                        {
                            svSessionId = pipeServerSessionId;
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    if (hPipe != IntPtr.Zero && hPipe.ToInt64() != Win32Error.InvalidHandle)
                    {
                        CloseHandle(hPipe);
                    }
                }

                string? sddl = GetSddl("\\\\.\\pipe\\{0}");


                yield return new NamedPipesDTO()
                {
                    Name = namedPipe,
                    Sddl = sddl,
                    ServerProcessName = svProcessName,
                    ServerProcessPID = svProcessId,
                    ServerProcessPath = svProcessPath,
                    ServerSessionId = svSessionId,
                };
            }
        }

        private string? GetSddl(string namedPipe)
        {
            try
            {
                var security = File.GetAccessControl($"\\\\.\\pipe\\{namedPipe}");
                var sddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                return sddl;
            }
            catch
            {
                return null;
            }
        }
    }

    internal class NamedPipesDTO : CommandDTOBase
    {
        public string Name { get; set; }
        public string? Sddl { get; set; }
        public string? ServerProcessName { get; set; }
        public int? ServerProcessPID { get; set; }
        public string? ServerProcessPath { get; set; }
        public int? ServerSessionId { get; internal set; }
    }

    [CommandOutputType(typeof(NamedPipesDTO))]
    internal class NamedPipesFormatter : TextFormatterBase
    {
        public NamedPipesFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (NamedPipesDTO)result;

            WriteLine("\n{0}", dto.Name);

            if (dto.ServerProcessPID != null)
            {
                WriteLine($"    Server Process Id   : {dto.ServerProcessPID}");
            }

            if (dto.ServerSessionId != null)
            {
                WriteLine($"    Server Session Id   : {dto.ServerSessionId}");
            }

            if (!string.IsNullOrEmpty(dto.ServerProcessPath))
            {
                WriteLine($"    Server Process Name : {dto.ServerProcessName}");
            }

            if (!string.IsNullOrEmpty(dto.ServerProcessPath))
            {
                WriteLine($"    Server Process Path : {dto.ServerProcessPath}");
            }

            if (!string.IsNullOrEmpty(dto.Sddl))
            {
                WriteLine($"    Pipe SDDL           : {dto.Sddl}");
            }
        }
    }
}
#nullable enable