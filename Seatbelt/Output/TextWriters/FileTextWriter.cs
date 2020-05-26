using System;
using System.IO;

namespace Seatbelt.Output.TextWriters
{
    internal class FileTextWriter : ITextWriter, IDisposable
    {
        private readonly StreamWriter _stream;

        public FileTextWriter(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            _stream = File.CreateText(fileName);
            _stream.AutoFlush = true;
        }

        

        public void Write(string str)
            => _stream.Write(str);

        public void WriteLine()
            => _stream.WriteLine();

        public void WriteLine(string str) 
            => _stream.WriteLine(str);

        public void WriteLine(string format, params object?[] args) 
            => _stream.WriteLine(format, args);

        public void FlushAndClose()
        {
            _stream.Flush();
            _stream.Dispose();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
