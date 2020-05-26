using Seatbelt.Commands;
using Seatbelt.Output.TextWriters;

// Individual commands can impelement this this class if they want to format textual output in a certain way
namespace Seatbelt.Output.Formatters
{
    internal abstract class TextFormatterBase
    {
        private readonly ITextWriter _textWriter;

        protected TextFormatterBase(ITextWriter sink)
        {
            _textWriter = sink;
        }

        // Children implement this method to customize the command's string output
        public abstract void FormatResult(CommandBase? command, CommandDTOBase results, bool filterResults);

        protected void Write(string str) => _textWriter.Write(str);
        protected void WriteLine() => _textWriter.WriteLine();
        protected void WriteLine(string str) => _textWriter.WriteLine(str);
        protected void WriteLine(string format, object? arg0) => _textWriter.WriteLine(format, arg0);
        protected void WriteLine(string format, object? arg0, object? arg1) => _textWriter.WriteLine(format, arg0, arg1);
        protected void WriteLine(string format, object arg0, object arg1, object arg2) => _textWriter.WriteLine(format, arg0, arg1, arg2);
        protected void WriteLine(string format, params object?[] args) => _textWriter.WriteLine(format, args);
    }
}
