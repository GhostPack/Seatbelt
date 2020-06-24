using System;

namespace Seatbelt
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                (new Seatbelt(args)).Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unhandled terminating exception: {e}");
            }
        }
    }
}
