using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Seatbelt.Commands;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Output.Sinks
{
    // Any sinks that output text to a location should inherit from this class
    internal class TextOutputSink : IOutputSink
    {
        private readonly Dictionary<Type, TextFormatterBase> _customSinks = new Dictionary<Type, TextFormatterBase>();
        private readonly TextFormatterBase _defaultTextSink;

        private readonly ITextWriter _textWriter;
        private readonly bool _filterResults;

        public TextOutputSink(ITextWriter writer, bool filterResults)
        {
            _textWriter = writer;
            _filterResults = filterResults;

            // If a command doesn't customize its output, the default text outputter will be used
            _defaultTextSink = new DefaultTextFormatter(_textWriter);
            InitializeCustomTextFormatters();
        }

        private static Assembly MyAssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            return System.Reflection.Assembly.GetExecutingAssembly();
        }

        private void InitializeCustomTextFormatters()
        {
            var currentAssembly = Assembly.GetExecutingAssembly();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(MyAssemblyResolveEventHandler);

            foreach (var formatter in currentAssembly.GetTypes().Where(t => typeof(TextFormatterBase).IsAssignableFrom(t)))
            {
                var attributes = Attribute.GetCustomAttributes(formatter);

                foreach (var t in attributes)
                {
                    if (!(t is CommandOutputTypeAttribute)) continue;

                    var outputTypeAttr = (CommandOutputTypeAttribute) t;

                    if (_customSinks.ContainsKey(outputTypeAttr.Type))
                    {
                        throw new Exception($"Custom sink {outputTypeAttr.Type} already assigned to {_customSinks[outputTypeAttr.Type]}. Could not associate DTO with the another formatter({formatter})");
                    }

                    _customSinks.Add(outputTypeAttr.Type, (TextFormatterBase)Activator.CreateInstance(formatter, new object[] { _textWriter }));
                    break;
                }
            }

        }

        public void WriteOutput(CommandDTOBase dto)
        { 
            //var obj = dtoCollection?.FirstOrDefault();
            if (dto == null)
            {
                return;
            }

            // If the dto has a custom output sink, use it.  Otherwise, use the default output sink
            var dtoType = dto.GetType();
            if (_customSinks.ContainsKey(dtoType))
            {
                _customSinks[dtoType].FormatResult(null, dto, _filterResults);
            }
            else
            {
                _defaultTextSink.FormatResult(null, dto, _filterResults);
            }
        }

        public void WriteVerbose(string message) => WriteOutput(new VerboseDTO(message));

        public void WriteWarning(string message) => WriteOutput(new WarningDTO(message));

        public void WriteError(string message) => WriteOutput(new ErrorDTO(message));

        public void WriteHost(string message) => WriteOutput(new HostDTO(message));
        public string GetOutput()
        {
            return "";
        }
        public void Dispose()
        {
            _textWriter.Dispose();
        }
    }
}
