using Seatbelt.Commands;

namespace Seatbelt.Output.Sinks
{
    internal interface IOutputSink
    {
        void WriteOutput(CommandDTOBase dto);
        void WriteHost(string message);
        void WriteVerbose(string message);
        void WriteWarning(string message);
        void WriteError(string message);
    }
}
