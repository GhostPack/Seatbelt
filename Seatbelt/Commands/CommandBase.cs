using System;
using System.Collections.Generic;

namespace Seatbelt.Commands
{
    internal abstract class CommandBase
    {
        public abstract string Command { get; }
        public virtual string CommandVersion { get; set;  }
        public abstract string Description { get; }
        public abstract CommandGroup[] Group { get; }
        public abstract bool SupportRemote { get; }

        public Runtime Runtime { get; set; }

        protected CommandBase(Runtime runtime)
        {
            CommandVersion = "1.0";
            Runtime = runtime;
        }

        public abstract IEnumerable<CommandDTOBase?> Execute(string[] args);



        public void WriteOutput(CommandDTOBase dto) => throw new NotImplementedException();

        public void WriteVerbose(string message) => Runtime.OutputSink.WriteVerbose(message);

        public void WriteWarning(string message) => Runtime.OutputSink.WriteWarning(message);

        public void WriteError(string message) => Runtime.OutputSink.WriteError(message);

        public void WriteHost(string format = "", params object[] args) => Runtime.OutputSink.WriteHost(string.Format(format, args));
    }
}
