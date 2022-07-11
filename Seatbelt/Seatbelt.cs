using Seatbelt.Commands;
using Seatbelt.Output.Sinks;
using Seatbelt.Output.TextWriters;
using System;
using System.Linq;
using System.Text;

namespace Seatbelt
{
    public class Seatbelt : IDisposable
    {
        public bool FilterResults { get; }

        private readonly IOutputSink _outputSink;
        private readonly Runtime _runtime;
        private const string Version = "1.2.1";
        private SeatbeltOptions Options { get; set; }

        public Seatbelt(string[] args)
        {
            Options = (new SeatbeltArgumentParser(args)).Parse();

            _outputSink = OutputSinkFromArgs(Options.OutputFile);

            _runtime = new Runtime(
                _outputSink,
                Options.Commands,
                Options.CommandGroups,
                Options.FilterResults,
                Options.ComputerName,
                Options.UserName,
                Options.Password
                );
        }

        public string GetOutput()
        {
            return _outputSink.GetOutput();
        }

        private IOutputSink OutputSinkFromArgs(string? outputFileArg)
        {
            if (outputFileArg == null)
                return new TextOutputSink(new ConsoleTextWriter(), FilterResults);

            if (outputFileArg == string.Empty)
                throw new Exception("Invalid filename");


            if (outputFileArg.EndsWith(".json"))
            {
                return new JsonFileOutputSink(outputFileArg, FilterResults);
            }

            if (outputFileArg == "jsonstring")
            {
                return new JsonStringOutputSink(outputFileArg, FilterResults);
            }

            return new TextOutputSink(new FileTextWriter(outputFileArg), FilterResults);
        }

        public void Start()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            if (!Options.Commands.Any() && !Options.CommandGroups.Any())
            {
                PrintLogo();
                Usage();
                return;
            }

            if (!Options.QuietMode)
                PrintLogo();

            _runtime.Execute();

            watch.Stop();

            if (!Options.QuietMode)
            {
                _outputSink.WriteVerbose($"\n\n[*] Completed collection in {(watch.ElapsedMilliseconds / 1000.0)} seconds\n");
            }
        }

        public void PrintLogo()
        {
            _outputSink.WriteHost("\n\n                        %&&@@@&&                                                                                  ");
            _outputSink.WriteHost("                        &&&&&&&%%%,                       #&&@@@@@@%%%%%%###############%                         ");
            _outputSink.WriteHost("                        &%&   %&%%                        &////(((&%%%%%#%################//((((###%%%%%%%%%%%%%%%");
            _outputSink.WriteHost("%%%%%%%%%%%######%%%#%%####%  &%%**#                      @////(((&%%%%%%######################(((((((((((((((((((");
            _outputSink.WriteHost("#%#%%%%%%%#######%#%%#######  %&%,,,,,,,,,,,,,,,,         @////(((&%%%%%#%#####################(((((((((((((((((((");
            _outputSink.WriteHost("#%#%%%%%%#####%%#%#%%#######  %%%,,,,,,  ,,.   ,,         @////(((&%%%%%%%######################(#(((#(#((((((((((");
            _outputSink.WriteHost("#####%%%####################  &%%......  ...   ..         @////(((&%%%%%%%###############%######((#(#(####((((((((");
            _outputSink.WriteHost("#######%##########%#########  %%%......  ...   ..         @////(((&%%%%%#########################(#(#######((#####");
            _outputSink.WriteHost("###%##%%####################  &%%...............          @////(((&%%%%%%%%##############%#######(#########((#####");
            _outputSink.WriteHost("#####%######################  %%%..                       @////(((&%%%%%%%################                        ");
            _outputSink.WriteHost("                        &%&   %%%%%      Seatbelt         %////(((&%%%%%%%%#############*                         ");
            _outputSink.WriteHost($"                        &%%&&&%%%%%        v{Version}         ,(((&%%%%%%%%%%%%%%%%%,                                 ");
            _outputSink.WriteHost("                         #%%%%##,                                                                                 \n\n");
        }


        private void Usage()
        {
            // List all available commands
            _outputSink.WriteHost("Available commands (+ means remote usage is supported):\n");
            _runtime.AllCommands.ForEach(c =>
            {
                if (c.SupportRemote)
                {
                    _outputSink.WriteHost($"    + {c.Command,-22} - {c.Description}");
                }
                else
                {
                    _outputSink.WriteHost($"      {c.Command,-22} - {c.Description}");
                }
            });


            // List all command groupings
            var commandGroups = Enum.GetNames(typeof(CommandGroup)).ToArray();
            _outputSink.WriteHost("\n\nSeatbelt has the following command groups: " + string.Join(", ", commandGroups));
            _outputSink.WriteHost("\n    You can invoke command groups with         \"Seatbelt.exe <group>\"\n");
            _outputSink.WriteHost("\n    Or command groups except specific commands \"Seatbelt.exe <group> -Command\"\n");

            var sb = new StringBuilder();
            foreach (var group in commandGroups)
            {

                if (group == "All")
                {
                    sb.Append($"   \"Seatbelt.exe -group={group.ToLower()}\" runs all commands\n\n");
                    continue;
                }

                sb.Append($"   \"Seatbelt.exe -group={group.ToLower()}\" runs the following commands:\n\n        ");

                var groupCommands = _runtime.AllCommands
                        .Where(c => c.Group.Contains((CommandGroup)Enum.Parse(typeof(CommandGroup), group)))
                        .Select(c => c.Command)
                        .ToArray();

                for (var i = 0; i < groupCommands.Length; i++)
                {
                    sb.Append(groupCommands[i]);

                    if (i != groupCommands.Length - 1)
                    {
                        sb.Append(", ");
                    }

                    if (i % 4 == 0 && i != 0)
                    {
                        sb.Append("\n        ");
                    }
                }
                sb.AppendLine("\n");
            }

            _outputSink.WriteHost(sb.ToString());
            _outputSink.WriteHost("Examples:");
            _outputSink.WriteHost("    'Seatbelt.exe <Command> [Command2] ...' will run one or more specified checks only");
            _outputSink.WriteHost("    'Seatbelt.exe <Command> -full' will return complete results for a command without any filtering.");
            _outputSink.WriteHost("    'Seatbelt.exe \"<Command> [argument]\"' will pass an argument to a command that supports it (note the quotes).");
            _outputSink.WriteHost("    'Seatbelt.exe -group=all' will run ALL enumeration checks, can be combined with \"-full\".");
            _outputSink.WriteHost("    'Seatbelt.exe -group=all -AuditPolicies' will run all enumeration checks EXCEPT AuditPolicies, can be combined with \"-full\".");
            _outputSink.WriteHost("    'Seatbelt.exe <Command> -computername=COMPUTER.DOMAIN.COM [-username=DOMAIN\\USER -password=PASSWORD]' will run an applicable check remotely");
            _outputSink.WriteHost("    'Seatbelt.exe -group=remote -computername=COMPUTER.DOMAIN.COM [-username=DOMAIN\\USER -password=PASSWORD]' will run remote specific checks");
            _outputSink.WriteHost("    'Seatbelt.exe -group=system -outputfile=\"C:\\Temp\\out.txt\"' will run system checks and output to a .txt file.");
            _outputSink.WriteHost("    'Seatbelt.exe -group=user -q -outputfile=\"C:\\Temp\\out.json\"' will run in quiet mode with user checks and output to a .json file.");

        }

        public void Dispose()
        {
            _outputSink.Dispose();
        }
    }
}