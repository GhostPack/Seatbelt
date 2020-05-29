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
        protected void WriteLine(string format, params object?[] args) => _textWriter.WriteLine(format, args);
    }
}
