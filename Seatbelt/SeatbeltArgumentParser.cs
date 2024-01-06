using System;
using System.Collections.Generic;
using System.Linq;

namespace Seatbelt
{
    class SeatbeltArgumentParser
    {
        private string[] Arguments { get; set; }
        public SeatbeltArgumentParser(string[] args)
        {
            Arguments = args;
        }

        public SeatbeltOptions Parse()
        {
            var originalArgs = Arguments;

            try
            {
                var quietMode = ParseAndRemoveSwitchArgument("-q");
                var filterResults = !ParseAndRemoveSwitchArgument("-Full");
                var randomizeOrder = ParseAndRemoveSwitchArgument("-RandomizeOrder");

                var commandGroups = ParseAndRemoveKeyValueArgument("-Group");
                var outputFile = ParseAndRemoveKeyValueArgument("-OutputFile");
                var computerName = ParseAndRemoveKeyValueArgument("-ComputerName");
                var userName = ParseAndRemoveKeyValueArgument("-Username");
                var password = ParseAndRemoveKeyValueArgument("-Password");

                var delayCommands = ParseAndRemoveKeyValueArgument("-DelayCommands");

                return new SeatbeltOptions(
                    Arguments.ToList(),      // Everything else that isn't parsed is interpreted as a command
                    commandGroups == null ? new List<string>() : commandGroups.Split(',').Select(g => g.Trim()).ToList(),
                    outputFile,
                    filterResults,
                    randomizeOrder,
                    quietMode,
                    delayCommands,
                    computerName,
                    userName,
                    password
                    );
            }
            finally
            {
                Arguments = originalArgs;
            }

        }

        private bool ParseAndRemoveSwitchArgument(string key)
        {
            if (Arguments.Contains(key, StringComparer.CurrentCultureIgnoreCase))
            {
                Arguments = Arguments.Where(c => c.ToLower() != key.ToLower()).ToArray();
                return true;
            }

            return false;
        }

        private string? ParseAndRemoveKeyValueArgument(string key)
        {
            var arg = Arguments.FirstOrDefault(
                c => c.ToLower().StartsWith($"{key.ToLower()}=")
            );

            if (string.IsNullOrEmpty(arg))
                return null;

            try
            {
                var value = arg.Substring(arg.IndexOf('=') + 1);
                Arguments = Arguments.Where(c => !c.ToLower().StartsWith(key.ToLower())).ToArray();
                return value;
            }
            catch (Exception e)
            {
                throw new Exception($"Error parsing password argument \"{key}\": {e}");
            }
        }
    }
}
