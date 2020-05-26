using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    internal class ErrorDTO : CommandDTOBase
    {
        public ErrorDTO(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    [CommandOutputType(typeof(ErrorDTO))]
    internal class ErrorTextFormatter : TextFormatterBase
    {
        public ErrorTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ErrorDTO)result;
            WriteLine("ERROR: " + dto.Message);
        }
    }
}
