using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using static Seatbelt.Interop.Advapi32;
using static Seatbelt.Interop.Secur32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    // TODO: Most privileges are disabled by default.  Better to move this to SharpUp?
    internal class TokenPrivilegesCommand : CommandBase
    {
        public override string Command => "TokenPrivileges";
        public override string Description => "Currently enabled token privileges (e.g. SeDebugPrivilege/etc.)";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false;

        public TokenPrivilegesCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // Returns all privileges that the current process/user possesses
            // adapted from https://stackoverflow.com/questions/4349743/setting-size-of-token-privileges-luid-and-attributes-array-returned-by-gettokeni

            WriteHost("Current Token's Privileges\n");

            var TokenInfLength = 0;
            var ThisHandle = WindowsIdentity.GetCurrent().Token;
            GetTokenInformation(ThisHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, TokenInfLength, out TokenInfLength);
            var TokenInformation = Marshal.AllocHGlobal(TokenInfLength);
            if (GetTokenInformation(WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenPrivileges, TokenInformation, TokenInfLength, out TokenInfLength))
            {
                var ThisPrivilegeSet = (TOKEN_PRIVILEGES)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_PRIVILEGES));
                for (var index = 0; index < ThisPrivilegeSet.PrivilegeCount; index++)
                {
                    var laa = ThisPrivilegeSet.Privileges[index];
                    var StrBuilder = new System.Text.StringBuilder();
                    var luidNameLen = 0;
                    var luidPointer = Marshal.AllocHGlobal(Marshal.SizeOf(laa.Luid));
                    Marshal.StructureToPtr(laa.Luid, luidPointer, true);
                    LookupPrivilegeName(null, luidPointer, null, ref luidNameLen);
                    StrBuilder.EnsureCapacity(luidNameLen + 1);
                    if (LookupPrivilegeName(null, luidPointer, StrBuilder, ref luidNameLen))
                    {
                        var strPrivilege = StrBuilder.ToString();
                        var strAttributes = String.Format("{0}", (LuidAttributes)laa.Attributes);
                        Marshal.FreeHGlobal(luidPointer);

                        yield return new TokenPrivilegesDTO(
                            strPrivilege,
                            strAttributes
                        );
                    }
                    
                }
            }
        }
    }

    internal class TokenPrivilegesDTO : CommandDTOBase
    {
        public TokenPrivilegesDTO(string privilege, string attributes)
        {
            Privilege = privilege;
            Attributes = attributes;
        }

        public string Privilege { get; set; }
        public string Attributes { get; set; }
    }

    [CommandOutputType(typeof(TokenPrivilegesDTO))]
    internal class TokenPrivilegesTextFormatter : TextFormatterBase
    {
        public TokenPrivilegesTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (TokenPrivilegesDTO)result;
 
            WriteLine("  {0,43}:  {1}", dto.Privilege, dto.Attributes);
        }
    }
}
