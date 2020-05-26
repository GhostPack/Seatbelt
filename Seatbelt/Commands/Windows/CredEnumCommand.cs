#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Seatbelt.Interop;
using System.ComponentModel;
using System.Text;


// this code was adapted by @djhohnstein from @peewpw's work at
//  https://github.com/peewpw/Invoke-WCMDump/blob/master/Invoke-WCMDump.ps1
//  which was originally based on https://github.com/spolnik/Simple.CredentialsManager

namespace Seatbelt.Commands.Windows
{
    internal class CredEnumCommand : CommandBase
    {
        public override string Command => "CredEnum";
        public override string Description => "Enumerates the current user's saved credentials using CredEnumerate()";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public CredEnumCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // from https://gist.github.com/meziantou/10311113#file-credentialmanager-cs-L83-L105
            var ret = Advapi32.CredEnumerate(null, 0, out var count, out var pCredentials);
            if (!ret)
            {
                var lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            for (var n = 0; n < count; n++)
            {
                var credentialPtr = Marshal.ReadIntPtr(pCredentials, n * Marshal.SizeOf(typeof(IntPtr)));
                var credential = (Advapi32.CREDENTIAL)Marshal.PtrToStructure(credentialPtr, typeof(Advapi32.CREDENTIAL));

                string password = null;
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    var passwordBytes = new byte[credential.CredentialBlobSize];
                    Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, credential.CredentialBlobSize);
                    var flags = Advapi32.IsTextUnicodeFlags.IS_TEXT_UNICODE_STATISTICS;

                    if (Advapi32.IsTextUnicode(passwordBytes, passwordBytes.Length, ref flags))
                    {
                        password = Encoding.Unicode.GetString(passwordBytes);
                    }
                    else
                    {
                        password = BitConverter.ToString(passwordBytes).Replace("-", " ");
                    }
                }

                yield return new CredEnumDTO(
                    credential.TargetName,
                    credential.Comment,
                    credential.UserName,
                    password,
                    credential.Type,
                    credential.Persist,
                    DateTime.FromFileTime(credential.LastWritten)
                );
            }

            Advapi32.CredFree(pCredentials);
        }

        internal class CredEnumDTO : CommandDTOBase
        {
            public CredEnumDTO(string target, string comment, string username, string password, Advapi32.CredentialType credentialType, Advapi32.PersistenceType persistenceType, DateTime lastWriteTime)
            {
                Target = target;
                Comment = comment;
                Username = username;
                Password = password;
                CredentialType = credentialType;
                PersistenceType = persistenceType;
                LastWriteTime = lastWriteTime;
            }
            public string Target { get; }
            public string Comment { get; }
            public string Username { get; }
            public string Password { get; }
            public Advapi32.CredentialType CredentialType { get; }
            public Advapi32.PersistenceType PersistenceType { get; }
            public DateTime LastWriteTime { get; }
        }

        [CommandOutputType(typeof(CredEnumDTO))]
        internal class WindowsVaultFormatter : TextFormatterBase
        {
            public WindowsVaultFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (CredEnumDTO)result;

                WriteLine($"  Target              : {dto.Target}");
                if (!String.IsNullOrEmpty(dto.Comment))
                {
                    WriteLine($"  Comment             : {dto.Comment}");
                }
                WriteLine($"  UserName            : {dto.Username}");
                WriteLine($"  Password            : {dto.Password}");
                WriteLine($"  CredentialType      : {dto.CredentialType}");
                WriteLine($"  PersistenceType     : {dto.PersistenceType}");
                WriteLine($"  LastWriteTime       : {dto.LastWriteTime}\r\n");
            }
        }
    }
}
#nullable enable