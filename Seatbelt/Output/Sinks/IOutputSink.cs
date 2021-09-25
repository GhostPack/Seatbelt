using System;
using Seatbelt.Commands;

namespace Seatbelt.Output.Sinks
{
    internal interface IOutputSink : IDisposable
    {
        void WriteOutput(CommandDTOBase dto);
        void WriteHost(string message);
        void WriteVerbose(string message);
        void WriteWarning(string message);
        void WriteError(string message);
        string GetOutput();
    }
}
