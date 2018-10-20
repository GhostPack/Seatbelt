using System;

namespace Seatbelt.IO
{
    public class ConsoleOutput : IOutputSink
    {

        public void WriteLine(string text)
            => Console.WriteLine(text);

        public void Write(string text)
            => Console.Write(text);

        public void FlushAndClose()
        {
            // NOP
        }
    }
}
