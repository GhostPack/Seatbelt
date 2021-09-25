#nullable disable
using System.Collections.Generic;
using System.Management;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using System.Collections.Specialized;


namespace Seatbelt.Commands.Windows
{
    internal class WMICommand : CommandBase
    {
        public override string Command => "WMI";
        public override string Description => "Runs a specified WMI query";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public WMICommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            string wmiQuery = "Select * from Win32_ComputerSystem";
            string wmiNamespace = @"root\cimv2";

            if (args.Length == 1)
            {
                wmiQuery = args[0];
            }
            else if (args.Length == 2)
            {
                wmiNamespace = args[0];
                wmiQuery = args[1];
            }

            List<OrderedDictionary> results = new List<OrderedDictionary>();

            using (var searcher = ThisRunTime.GetManagementObjectSearcher(wmiNamespace, wmiQuery))
            {
                using (var items = searcher.Get())
                {
                    foreach (ManagementObject result in items)
                    {
                        OrderedDictionary properties = new OrderedDictionary();

                        foreach (var prop in result.Properties) {
                            properties.Add(prop.Name, prop.Value);
                        }

                        results.Add(properties);
                    }
                }
            }

            yield return new WMIDTO(
                results
            );
        }
    }

    internal class WMIDTO : CommandDTOBase
    {
        public WMIDTO(List<OrderedDictionary> results)
        {
            QueryResults = results;
        }
        public List<OrderedDictionary> QueryResults { get; }
    }

    [CommandOutputType(typeof(WMIDTO))]
    internal class WMIFormatter : TextFormatterBase
    {
        public WMIFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (WMIDTO)result;
            foreach (OrderedDictionary resultEntry in dto.QueryResults) {

                var enumerator = resultEntry.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if ((enumerator.Value != null) && enumerator.Value.GetType().Name == "String[]")
                    {
                        WriteLine("  {0}:", enumerator.Key);
                        foreach (var s in (string[])enumerator.Value)
                        {
                            WriteLine("      {0}", s);
                        }
                    }
                    // TODO: additional array type unrolling at some point as needed
                    else
                    {
                        WriteLine("  {0,-30}:  {1}", enumerator.Key, enumerator.Value);
                    }
                }
                WriteLine();
            }
        }
    }
}
#nullable enable