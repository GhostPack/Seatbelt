using System.Text;

namespace Seatbelt.IO
{
    public interface IOutputSink
    {

        void WriteLine(string text);

        void Write(string text);

        void FlushAndClose();
    }
}