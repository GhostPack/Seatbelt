using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    internal class HostDTO : CommandDTOBase
    {
        public HostDTO(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    [CommandOutputType(typeof(HostDTO))]
    internal class HostTextFormatter : TextFormatterBase
    {
        public HostTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (HostDTO)result;
            WriteLine(dto.Message);
        }
    }
}
