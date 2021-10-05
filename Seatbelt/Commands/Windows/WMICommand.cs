using System;
using System.Collections;
using System.Collections.Generic;
using System.Management;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using System.Collections.Specialized;
using System.Linq;
using System.Text;


namespace Seatbelt.Commands.Windows
{
    internal class WMICommand : CommandBase
    {
        public override string Command => "WMI";
        public override string Description => "Runs a specified WMI query";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public WMICommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var wmiQuery = "Select * from Win32_ComputerSystem";
            var wmiNamespace = @"root\cimv2";

            if (args.Length == 1)
            {
                wmiQuery = args[0];
            }
            else if (args.Length == 2)
            {
                wmiNamespace = args[0];
                wmiQuery = args[1];
            }

            var results = new List<OrderedDictionary>();

            using (var searcher = ThisRunTime.GetManagementObjectSearcher(wmiNamespace, wmiQuery))
            {
                using var items = searcher.Get();

                foreach (ManagementObject result in items)
                {
                    var properties = new OrderedDictionary();

                    foreach (var prop in result.Properties)
                    {
                        properties.Add(prop.Name, prop.Value);
                    }

                    results.Add(properties);
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
            foreach (var resultEntry in dto.QueryResults)
            {
                var enumerator = resultEntry.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    var value = enumerator.Value;
                    if (value == null)
                    {
                        continue;
                    }

                    var valueType = value.GetType();
                    var valueName = enumerator.Key?.ToString();

                    if (valueType.IsArray)
                    {
                        WriteArrayValue(valueType, valueName, value);
                    }
                    else
                    {
                        WriteLine("  {0,-30}: {1}", valueName, value);
                    }
                }
                WriteLine();
            }
        }

        private void WriteArrayValue(Type valueType, string? valueName, object value)
        {
            var elemType = valueType.GetElementType();

            var name = $"{valueName}({valueType.Name})";

            if (elemType == typeof(string))
            {
                WriteLine($"  {name,-30}:");
                foreach (var s in (string[]) value)
                {
                    WriteLine($"      {s}");
                }
            }
            else
            {
                IEnumerable<string> s = ((IEnumerable) value).Cast<object>()
                    .Select(x => x.ToString())
                    .ToArray();

                var v = string.Join(",", (string[]) s);

                WriteLine($"  {name,-30}: {v}");
            }
        }
    }
}