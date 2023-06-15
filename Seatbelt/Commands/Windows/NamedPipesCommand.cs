#nullable disable
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using System.Security.AccessControl;
using static Seatbelt.Interop.Kernel32;
using System.IO;


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
                FileSecurity security;
                var sddl = "";
                int iProcessId = 0;
                string svProcessName = "";
                string svProcessPath = "";
                System.IntPtr hPipe = System.IntPtr.Zero;
                bool bvRet = false;

                // Try to identify ProcessID and ProcessName
                try
                {
                    //Get a handle to the pipe
                    hPipe = CreateFile(
                        System.String.Format("\\\\.\\pipe\\{0}", namedPipe), // The name of the file or device to be created or opened.
                        FileAccess.Read, // The requested access to the file or device.
                        FileShare.None, // The requested sharing mode of the file or device.
                        System.IntPtr.Zero, // Optional. A pointer to a SECURITY_ATTRIBUTES structure.
                        FileMode.Open, // An action to take on a file or device that exists or does not exist.
                        FileAttributes.Normal, // The file or device attributes and flags.
                        System.IntPtr.Zero); // Optional. A valid handle to a template file with the GENERIC_READ access right.


                    if (hPipe.ToInt64() != -1) //verify CreateFile did not return "INVALID_HANDLE_VALUE"
                    { 

                        //Retrieve the ProcessID registered for the pipe.
                        bvRet = GetNamedPipeServerProcessId(
                            hPipe, // A handle to an instance of a named pipe.
                            out iProcessId); // The process identifier.

                        //If GetNamedPipeServerProcessId was successful, get the process name for the returned ProcessID
                        if (bvRet)
                        {
                            var svProcess = System.Diagnostics.Process.GetProcessById(iProcessId);
                            svProcessName = svProcess.ProcessName;
                            svProcessPath = svProcess.MainModule.FileName;
                        }
                        else
                        {
                            //GetNamedPipeServerProcessId was unsuccessful
                            svProcessName = "Unk";
                        }

                        //Close the pipe handle
                        CloseHandle(hPipe);
                    }
                    else
                    {
                        //CreateFile returned "INVALID_HANDLE_VALUE" or 0xffffffff.
                        svProcessName = "Unk";
                    }
                }
                catch
                {
                    //Catch the exception. ProcessName is set to Unk.
                    svProcessName = "Unk";
                }

                try
                {
                    security = File.GetAccessControl(System.String.Format("\\\\.\\pipe\\{0}", namedPipe));
                    sddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                }
                catch
                {
                    sddl = "ERROR";
                }

                yield return new NamedPipesDTO()
                {
                    Name = namedPipe,
                    Sddl = sddl,
                    //SecurityDescriptor = null

                    ServerProcessName = svProcessName,
                    ServerProcessPID = iProcessId,
                    ServerProcessPath = svProcessPath
                };
            }
        }
    }

    internal class NamedPipesDTO : CommandDTOBase
    {
        public string Name { get; set; }

        public string Sddl { get; set; }

        public string ServerProcessName { get; set; }

        public int ServerProcessPID { get; set; }

        public string ServerProcessPath { get; set; }

        // public RawSecurityDescriptor SecurityDescriptor { get; set; }
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
            WriteLine("    Server Process Id   : {0}", dto.ServerProcessPID.ToString());
            if (!string.IsNullOrEmpty(dto.ServerProcessPath))
            {
                WriteLine("    Server Process Name : {0}", dto.ServerProcessName);
            }
            if (!string.IsNullOrEmpty(dto.ServerProcessPath))
            {
                WriteLine("    Server Process Path : {0}", dto.ServerProcessPath);
            }
            if (!dto.Sddl.Equals("ERROR"))
            {
                //WriteLine("    Owner   : {0}", dto.SecurityDescriptor.Owner);
                //foreach (CommonAce rule in dto.SecurityDescriptor.DiscretionaryAcl)
                //{
                //    WriteLine("        {0} :", rule.SecurityIdentifier);
                //    WriteLine("              {0} : {1}", rule.AceType, (GenericAceMask)rule.AccessMask);
                //}
                WriteLine("    Pipe SDDL           : {0}", dto.Sddl);
            }
        }
    }
}
#nullable enable