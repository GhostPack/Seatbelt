using System;
using System.Diagnostics;


namespace Seatbelt
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                using var sb = (new Seatbelt(args));
                sb.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unhandled terminating exception: {e}");
            }
            finally
            {
                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
