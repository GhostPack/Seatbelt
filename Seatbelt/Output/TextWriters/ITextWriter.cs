namespace Seatbelt.Output.TextWriters
{
    internal interface ITextWriter
    {
        void Write(string str);
        void WriteLine();
        void WriteLine(string str);
        void WriteLine(string format, params object?[] args);
        void FlushAndClose();
    }
}