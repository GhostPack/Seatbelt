using System;
using System.Text;

namespace Seatbelt.Output.TextWriters
{
    internal class ConsoleTextWriter : ITextWriter
    {
        public ConsoleTextWriter() => Console.OutputEncoding = Encoding.UTF8;
        public void Write(string str)
            => Console.Write(str);

        public void WriteLine()
            => Console.WriteLine();

        public void WriteLine(string str)
            => Console.WriteLine(str);

        public void WriteLine(string format, params object?[] args) => Console.WriteLine(format, args);

        public void Dispose()
        {
        }
    }
}
