using System;

namespace Seatbelt.Output.TextWriters
{
    internal interface ITextWriter : IDisposable
    {
        void Write(string str);
        void WriteLine();
        void WriteLine(string str);
        void WriteLine(string format, params object?[] args);
    }
}