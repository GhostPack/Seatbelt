using Seatbelt.Commands;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Output.Formatters
{
    // If a command doesn't customize its output text, then this will be called
    internal class DefaultTextFormatter : TextFormatterBase
    {
        public DefaultTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            if (result == null)
            {
                return;
            }

            var type = result.GetType();

            foreach (var p in type.GetProperties())
            {
                if (p.PropertyType.IsArray)
                {
                    var value = (object[])p.GetValue(result, null);
                    Write($"  {p.Name,-30} : ");
                    for (var i = 0; i < value.Length; i++)
                    {
                        Write(value[i].ToString());
                        if (value.Length-1 != i)
                        {
                            Write(", ");
                        }
                    }
                    WriteLine();
                }
                else
                {
                    var value = p.GetValue(result, null);
                    WriteLine("  {0,-30} : {1}", p.Name, value);
                }
            }

            WriteLine();
        }
    }
}