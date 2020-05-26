using System;

namespace Seatbelt.Util
{
    internal static class ExtensionMethods
    {
        // From https://stackoverflow.com/questions/4108828/generic-extension-method-to-see-if-an-enum-contains-a-flag
        public static bool HasFlag(this Enum variable, Enum value)
        {
            if (variable == null)
                return false;

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Not as good as the .NET 4 version of this function, but should be good enough
            if (!Enum.IsDefined(variable.GetType(), value))
            {
                throw new ArgumentException(
                    $"Enumeration type mismatch.  The flag is of type '{value.GetType()}', was expecting '{variable.GetType()}'.");
            }

            var num = Convert.ToUInt64(value);
            return ((Convert.ToUInt64(variable) & num) == num);

        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static string TrimEnd(string input, string suffixToRemove)
        {
            if (input.EndsWith(suffixToRemove, StringComparison.OrdinalIgnoreCase))
            {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }
            
            return input;
        }
    }
}
