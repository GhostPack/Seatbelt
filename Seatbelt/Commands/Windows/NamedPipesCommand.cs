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
        public override string Description => "Named pipe names and any readable ACL information.";
        public override CommandGroup[] Group => new[] {CommandGroup.System};

        public override bool SupportRemote => false; // might be true? unsure...

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
                try
                {
                    security = File.GetAccessControl(System.String.Format("\\\\.\\pipe\\{0}", namedPipe));
                    sddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                }
                catch
                {
                    sddl = "ERROR";
                }

                if (!System.String.IsNullOrEmpty(sddl) && !sddl.Equals("ERROR"))
                {
                    yield return new NamedPipesDTO()
                    {
                        Name = namedPipe,
                        Sddl = sddl
                        //SecurityDescriptor = new RawSecurityDescriptor(sddl)
                    };
                }
                else
                {
                    yield return new NamedPipesDTO()
                    {
                        Name = namedPipe,
                        Sddl = sddl
                        //SecurityDescriptor = null
                    };
                }
            }
        }
    }

    internal class NamedPipesDTO : CommandDTOBase
    {
        public string Name { get; set; }

        public string Sddl { get; set; }

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

            WriteLine("  \\\\.\\pipe\\{0}", dto.Name);
            if (!dto.Sddl.Equals("ERROR"))
            {
                //WriteLine("    Owner   : {0}", dto.SecurityDescriptor.Owner);
                //foreach (CommonAce rule in dto.SecurityDescriptor.DiscretionaryAcl)
                //{
                //    WriteLine("        {0} :", rule.SecurityIdentifier);
                //    WriteLine("              {0} : {1}", rule.AceType, (GenericAceMask)rule.AccessMask);
                //}
                WriteLine("    SDDL         : {0}", dto.Sddl);
            }
        }
    }
}
#nullable enable