using System;
using System.Collections.Generic;
using Microsoft.Win32;


namespace Seatbelt.Commands.Windows
{
    internal class LastShutdownCommand : CommandBase
    {
        public override string Command => "LastShutdown";
        public override string Description => "Returns the DateTime of the last system shutdown (via the registry).";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public LastShutdownCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var shutdownBytes = ThisRunTime.GetBinaryValue(RegistryHive.LocalMachine, "SYSTEM\\ControlSet001\\Control\\Windows", "ShutdownTime");
            if (shutdownBytes != null)
            {
                var shutdownInt = BitConverter.ToInt64(shutdownBytes, 0);
                var shutdownTime = DateTime.FromFileTime(shutdownInt);

                yield return new LastShutdownDTO()
                {
                    LastShutdown = shutdownTime
                };
            }
        }
    }

    internal class LastShutdownDTO : CommandDTOBase
    {
        public DateTime LastShutdown { get; set; }
    }
}
