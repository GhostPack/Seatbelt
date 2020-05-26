#if DEBUG
using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

// Any command you create should not generate compiler warnings
namespace Seatbelt.Commands.Windows
{
    // Replace all instances of "TEMPLATE" with the command name you're building
    internal class TEMPLATECommand : CommandBase
    {
        public override string Command => "TEMPLATE";
        public override string Description => "Description for your command";
        public override CommandGroup[] Group => new[] {CommandGroup.User};              // either CommandGroup.System, CommandGroup.User, or CommandGroup.Misc
        public override bool SupportRemote => true;                             // set to true if you want to signal that your module supports remote operations
        public Runtime ThisRunTime;

        public TEMPLATECommand(Runtime runtime) : base(runtime)
        {
            // use a constructor of this type if you want to support remote operations
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // .\Seatbelt\Runtime.cs contains a number of helper WMI/Registry functions that lets you implicitly perform enumeration locally or remotely.
            //      GetManagementObjectSearcher(string nameSpace, string query)     ==> easy WMI namespace searching. See DNSCacheCommand.cs
            //      GetSubkeyNames(RegistryHive hive, string path)                  ==> registry subkey enumeration via WMI StdRegProv. See PuttySessions.cs
            //      GetStringValue(RegistryHive hive, string path, string value)    ==> retrieve a string registry value via WMI StdRegProv. See PuttySessions.cs
            //      GetDwordValue(RegistryHive hive, string path, string value)     ==> retrieve an uint registry value via WMI StdRegProv. See NtlmSettingsCommand.cs
            //      GetBinaryValue(RegistryHive hive, string path, string value)    ==> retrieve an binary registry value via WMI StdRegProv. See SysmonCommand.cs
            //      GetValues(RegistryHive hive, string path)                       ==> retrieve the values under a path. See PuttyHostKeys.cs.
            //      GetUserSIDs()                                                   ==> return all user SIDs under HKU. See PuttyHostKeys.cs.

            var providers = ThisRunTime.GetSubkeyNames(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\AMSI\Providers");
            if(providers == null)
                yield break;       // Exit the function and don't return anything

            foreach (var provider in providers)
            {
                var providerPath = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"SOFTWARE\\Classes\\CLSID\\{provider}\\InprocServer32", "");

                // Avoid writing output inside this function.
                // If you want to format your output in a special way, use a text formatter class (see below)
                // You _can_ using the following function, however it's not recommended and will be going away in the future.
                // If you do, this data will not be serialized.
                // WriteHost("OUTPUT");

                // yield your DTO objects. If you need to yield a _collection_ of multiple objects, set one of the DTO properties to be a List or something similar.
                yield return new TEMPLATEDTO(
                    provider,
                    providerPath
                );
            }
        }

        // This is the output data transfer object (DTO).
        // Properties in this class should only have getters or private setters, and should be initialized in the constructor.
        // Some of the existing commands are migrating to this format (in case you see ones that do not conform).
        internal class TEMPLATEDTO : CommandDTOBase
        {
            public TEMPLATEDTO(string property, string? propertyPath)
            {
                Property = property;
                PropertyPath = propertyPath;
            }
            public string Property { get; }
            public string? PropertyPath { get; }
        }


        // This is optional.
        // If you want to format the output in a particular way, implement it here.
        // A good example is .\Seatbelt\Commands\Windows\NtlmSettingsCommand.cs
        // If this class does not exist, Seatbelt will use the DefaultTextFormatter class
        [CommandOutputType(typeof(TEMPLATEDTO))]
        internal class TEMPLATEFormatter : TextFormatterBase
        {
            public TEMPLATEFormatter(ITextWriter writer) : base(writer)
            {
                // nothing goes here
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                // use the following function here if you want to write out to the cmdline. This data will not be serialized.
                WriteLine("OUTPUT");
            }
        }
    }
}
#endif