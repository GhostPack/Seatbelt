using System.IO;

namespace Seatbelt.IO
{
    public class FileOutput : IOutputSink
    {
        private readonly StreamWriter _stream;


        public FileOutput(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            _stream = File.CreateText(fileName);
            _stream.AutoFlush = true;
        }


        public void WriteLine(string text)
            => _stream.WriteLine(text);

        public void Write(string text)
            => _stream.Write(text);

        public void FlushAndClose()
        {
            _stream.Flush();
            _stream.Dispose();
        }

    }
}