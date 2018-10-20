using System;
using System.Collections.Generic;
using System.Linq;
using Seatbelt.IO;
using Seatbelt.ProbePresets;


namespace Seatbelt
{
    public static class ArgumentsParser
    {

        public static List<Action> Parse(string[] args, IOutputSink output, AvailableProbes probes)
        {

            // Get the arguments and lowercase them all so we can ignore the case going forward
            var arguments = args.Select(a => a.ToLowerInvariant()).Distinct().ToList();

            // the actions the user wants to execute
            var actionsRequested = new List<Action>();

            // look for the full, user, system presets and full - if present
            ParseForPresets(output, probes, arguments, actionsRequested);

            // look through the remaining arguments and add to the actions required
            foreach (var argument in arguments)
                actionsRequested.Add(() => output.Write(probes.RunProbe(argument)));

            return actionsRequested;

        }

        private static void ParseForPresets(IOutputSink output, AvailableProbes probes, List<string> arguments, List<Action> actionsRequested)
        {
            if (arguments.Contains("full"))
            {
                FilterResults.Filter = false;
                actionsRequested.Add(() => FullPreset.Run(output, probes));
                arguments.Remove("full");
            }

            if (arguments.Contains("all"))
            {
                actionsRequested.Add(() => AllPreset.Run(output, probes));
                arguments.Remove("all");
            }


            if (arguments.Contains("system"))
            {
                actionsRequested.Add(() => SystemPreset.Run(output, probes));
                arguments.Remove("system");
            }


            if (arguments.Contains("user"))
            {
                actionsRequested.Add(() => UserPreset.Run(output, probes));
                arguments.Remove("user");
            }
        }


        /// <summary>
        /// Look for the output choice (file or none (Console))
        /// Expected command line format is: ToFile "file name.txt"
        /// i.e. the filename is the next argument after the 'ToFile' keyword
        /// </summary>
        public static IOutputSink GetOutputTarget(string[] args)
        {

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLowerInvariant() == "tofile")
                {
                    // grab the file name
                    var filename = args[i + 1];

                    // empty out the parts of the array in case the filename conflicts somehow with the name of a probe
                    args[i] = string.Empty;
                    args[i + 1] = string.Empty;

                    return new FileOutput(filename);
                }
            }

            // if we get here then the file output wasn't selected to default to console
            return new ConsoleOutput();
        }
    }


}
