using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static Seatbelt.Interop.User32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    internal class IdleTimeCommand : CommandBase
    {
        public override string Command => "IdleTime";
        public override string Description => "Returns the number of seconds since the current user's last input.";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false; // not possible


        public IdleTimeCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var lastInputInfo = new LastInputInfo();
            lastInputInfo.Size = (uint)Marshal.SizeOf(lastInputInfo);

            if (GetLastInputInfo(ref lastInputInfo))
            {
                var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                yield return new IdleTimeDTO(
                    currentUser,
                    ((uint) Environment.TickCount - lastInputInfo.Time)
                );
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    internal class IdleTimeDTO : CommandDTOBase
    {
        public IdleTimeDTO(string currentUser, uint milliseconds)
        {
            CurrentUser = currentUser;
            Milliseconds = milliseconds;
        }
        public string CurrentUser { get; }

        public uint Milliseconds { get; }
    }

    [CommandOutputType(typeof(IdleTimeDTO))]
    internal class IdleTimeFormatter : TextFormatterBase
    {
        public IdleTimeFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (IdleTimeDTO)result;

            var t = TimeSpan.FromMilliseconds(dto.Milliseconds);
            var idleTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                        t.Hours,
                        t.Minutes,
                        t.Seconds,
                        t.Milliseconds);

            WriteLine($"  CurrentUser : {dto.CurrentUser}");
            WriteLine($"  Idletime    : {idleTime} ({dto.Milliseconds} milliseconds)\n");
        }
    }
}
