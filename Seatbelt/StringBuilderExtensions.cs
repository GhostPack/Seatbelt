using System;
using System.Text;

namespace Seatbelt
{
    public static class StringBuilderExtensions
    {

        public static void AppendProbeHeaderLine(this StringBuilder stringBuilder, string headerText) 
            => stringBuilder.AppendLine($"\r\n\r\n=== {headerText} ===\r\n");

        public static void AppendSubHeaderLine(this StringBuilder stringBuilder, string headerText)
        => stringBuilder.AppendLine($"  {headerText}:\r\n");

        public static void AppendExceptionLine(this StringBuilder stringBuilder, Exception ex)
        => stringBuilder.AppendLine($"  [X] Exception: {ex.Message}");

    }
}
