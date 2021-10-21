using System;
using System.IO;
using System.Web.Script.Serialization;
using Seatbelt.Commands;

namespace Seatbelt.Output.Sinks
{
    // Any sinks that output text to a location should inherit from this class
    internal class JsonFileOutputSink : IOutputSink
    {
        private StreamWriter _stream;
        private JavaScriptSerializer _json = new JavaScriptSerializer();

        public JsonFileOutputSink(string file, bool filterResults)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            _stream = File.CreateText(file);
            _stream.AutoFlush = true;
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

            var obj = new
            {
                Type = dtoType.ToString(),
                Data = dto
            };

            string jsonStr;
            try
            {
                jsonStr = _json.Serialize(obj);
            }
            catch(Exception e)
            {
                jsonStr = _json.Serialize(new
                {
                    Type = typeof(ErrorDTO).ToString(),
                    Data = _json.Serialize(new ErrorDTO(e.ToString()))
                });
            }

            _stream.WriteLine(jsonStr);
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
            _stream.Dispose();
        }
    }
}
