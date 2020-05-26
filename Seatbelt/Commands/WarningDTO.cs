
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    class WarningDTO : CommandDTOBase
    {
        public WarningDTO(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    [CommandOutputType(typeof(WarningDTO))]
    internal class WarningTextFormatter : TextFormatterBase
    {
        public WarningTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase dto, bool filterResults)
        {
            WriteLine("WARNING: " + ((WarningDTO)dto).Message);
        }
    }
}
