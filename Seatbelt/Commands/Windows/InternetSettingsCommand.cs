#nullable disable
using Microsoft.Win32;
using Seatbelt.Util;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;


namespace Seatbelt.Commands.Windows
{
    internal class InternetSettingsCommand : CommandBase
    {
        public override string Command => "InternetSettings";
        public override string Description => "Internet settings including proxy configs";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO remote

        public InternetSettingsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            WriteHost(" Hive                               Key : Value\n");
            // lists user/system internet settings, including default proxy info
            var proxySettings = RegistryUtil.GetValues(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");

            if ((proxySettings != null) && (proxySettings.Count != 0))
            {
                foreach (var kvp in proxySettings)
                {
                    yield return new InternetSettingsDTO()
                    {
                        Hive = "HKCU",
                        Key = kvp.Key,
                        Value = kvp.Value.ToString()
                    };
                }
            }

            WriteHost();

            var proxySettings2 = RegistryUtil.GetValues(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");

            if ((proxySettings2 != null) && (proxySettings2.Count != 0))
            {
                foreach (var kvp in proxySettings2)
                {
                    yield return new InternetSettingsDTO()
                    {
                        Hive = "HKLM",
                        Key = kvp.Key,
                        Value = kvp.Value.ToString()
                    };
                }
            }
        }

        internal class InternetSettingsDTO : CommandDTOBase
        {
            public string Hive { get; set; }

            public string Key { get; set; }

            public string Value { get; set; }
        }

        [CommandOutputType(typeof(InternetSettingsDTO))]
        internal class InternetSettingsFormatter : TextFormatterBase
        {
            public InternetSettingsFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (InternetSettingsDTO)result;

                WriteLine(" {0}    {1,30} : {2}", dto.Hive, dto.Key, dto.Value);
            }
        }
    }
}
#nullable enable