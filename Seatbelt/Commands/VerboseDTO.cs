
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    class VerboseDTO : CommandDTOBase
    {
        public VerboseDTO(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    [CommandOutputType(typeof(VerboseDTO))]
    internal class VerboseTextFormatter : TextFormatterBase
    {
        public VerboseTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase dto, bool filterResults)
        {
            //WriteLine("VERBOSE: " + ((VerboseDTO)dto).Message);
            WriteLine(((VerboseDTO)dto).Message);
        }
    }
}
