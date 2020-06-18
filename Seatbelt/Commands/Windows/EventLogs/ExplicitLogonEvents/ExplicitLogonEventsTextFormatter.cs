using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using System.Collections.Generic;

namespace Seatbelt.Commands.Windows.EventLogs.ExplicitLogonEvents
{
    [CommandOutputType(typeof(ExplicitLogonEventsDTO))]
    internal class ExplicitLogonEventsTextFormatter : TextFormatterBase
    {
        private readonly Dictionary<string, List<string>> events = new Dictionary<string, List<string>>();

        public ExplicitLogonEventsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ExplicitLogonEventsDTO)result;
            
            var targetUser = dto.TargetDomain + "\\" + dto.TargetUser;
            var subjectUser = dto.SubjectDomain + "\\" + dto.SubjectUser;
            var uniqueCredKey = $"{targetUser},{dto.Process},{subjectUser},{dto.IpAddress}";

            WriteLine($"{dto.TimeCreatedUtc?.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")},{uniqueCredKey}");

            //if (events.TryGetValue(uniqueCredKey, out _) == false)
            //{
            //    events[uniqueCredKey] = new List<string>
            //        {
            //            dto.TimeCreated.ToString()
            //        };
            //}
            //else
            //{
            //    events[uniqueCredKey].Add(dto.TimeCreated.ToString());
            //}


            //foreach (string key in events.Keys)
            //{
            //    WriteLine("\n\n  --- " + key + " ---");
            //    var dates = events[key].ToArray();
            //    for (int i = 0; i < dates.Length; i++)
            //    {
            //        if (i % 4 == 0)
            //        {
            //            Write("\n  ");
            //        }

            //        Write(dates[i].PadRight(24));
            //    }

            //    Write("\n");


                //WriteLine("\n\n  --- " + key + " ---");
                //var dates = events[key].ToArray();

                //for (var i = 0; i < dates.Length; i++)
                //{
                //    if (i % 4 == 0)
                //    {
                //        WriteHost("\n  ");
                //    }

                //    WriteHost(dates[i]);

                //    if (i != dates.Length - 1)
                //    {
                //        WriteHost(", ");
                //    }
                //}

                //WriteLine();
            //}
        }
    }
}
