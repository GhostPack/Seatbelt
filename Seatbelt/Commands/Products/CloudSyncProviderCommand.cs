#if DEBUG
#nullable disable
using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;

// Any command you create should not generate compiler warnings
namespace Seatbelt.Commands.Windows
{
    // Module to enumerate information from tools such as OneDrive
    // Stuart Morgan <stuart.morgan@mwrinfosecurity.com> @ukstufus
    class OneDriveSyncProvider
    {
        // Stores the mapping between a sync ID and mount point
        public Dictionary<string, Dictionary<string, string>> mpList = new Dictionary<string, Dictionary<string, string>>();
        // Stores the list of OneDrive accouts configured in the registry
        public Dictionary<string, Dictionary<string, string>> oneDriveList = new Dictionary<string, Dictionary<string, string>>();
        // Stores the mapping between the account and the mountpoint IDs
        public Dictionary<string, List<string>> AcctoMPMapping = new Dictionary<string, List<string>>();
    }

    internal class CloudSyncProviderCommand : CommandBase
    {
        public override string Command => "CloudSyncProvider";
        public override string Description => "Lists ";
        public override CommandGroup[] Group => new[] {CommandGroup.User};              // either CommandGroup.System, CommandGroup.User, or CommandGroup.Misc
        public override bool SupportRemote => false;                             // set to true if you want to signal that your module supports remote operations
        public Runtime ThisRunTime;

        public CloudSyncProviderCommand(Runtime runtime) : base(runtime)
        {
            // use a constructor of this type if you want to support remote operations
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {

            // Get all of the user SIDs (so will cover all users if run as an admin or has access to other user's reg keys)
            var SIDs = ThisRunTime.GetUserSIDs();
            foreach (var sid in SIDs)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes")) // Disregard anything that isn't a user
                    continue;

                OneDriveSyncProvider o = new OneDriveSyncProvider();

                // Now get each of the IDs (they aren't GUIDs but are an identity value for the specific library to sync)
                var subKeys = ThisRunTime.GetSubkeyNames(RegistryHive.Users, $"{sid}\\Software\\SyncEngines\\Providers\\OneDrive");
                if (subKeys == null)
                    continue;

                // Now go through each of them, get the metadata and stick it in the 'provider' dict. It'll get cross referenced later.
                foreach (string rname in subKeys)
                {
                    Dictionary<string, string> provider = new Dictionary<string, string>();
                    foreach (string x in new List<string> { "LibraryType", "LastModifiedTime", "MountPoint", "UrlNamespace" })
                    {
                        var result = ThisRunTime.GetStringValue(RegistryHive.Users, $"{sid}\\Software\\SyncEngines\\Providers\\OneDrive\\{rname}", x);
                        if (!string.IsNullOrEmpty(result))
                            provider[x] = result;
                    }
                    o.mpList[rname] = provider;
                }

                var odAccounts = ThisRunTime.GetSubkeyNames(RegistryHive.Users, $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts");
                if (odAccounts == null)
                    continue;
             
                foreach (string acc in odAccounts)
                {
                    Dictionary<string, string> account = new Dictionary<string, string>();
                    foreach (string x in new List<string> { "DisplayName", "Business", "ServiceEndpointUri", "SPOResourceId", "UserEmail", "UserFolder", "UserName" })
                    {
                        var result = ThisRunTime.GetStringValue(RegistryHive.Users, $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts\\{acc}", x);
                        if (!string.IsNullOrEmpty(result))
                            account[x] = result;
                    }
                    var odMountPoints = ThisRunTime.GetValues(RegistryHive.Users, $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts\\{acc}\\ScopeIdToMountPointPathCache");
                    foreach (var mp in odMountPoints)
                    {
                        o.AcctoMPMapping[acc].Add(mp.Key);
                    }
                    o.oneDriveList[acc] = account;
                    
                }

                yield return new CloudSyncProviderDTO(
                    sid,
                    o
                    );

            }
            /*
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
                yield return new CloudSyncProviderDTO(
                    provider,
                    providerPath
                );
            }
            */
        }

        // This is the output data transfer object (DTO).
        // Properties in this class should only have getters or private setters, and should be initialized in the constructor.
        // Some of the existing commands are migrating to this format (in case you see ones that do not conform).
        internal class CloudSyncProviderDTO : CommandDTOBase
        {
            public CloudSyncProviderDTO(string sid, OneDriveSyncProvider odsp)
            {
                Sid = sid;
                Odsp = odsp;
            }
            public string Sid { get; }
            public OneDriveSyncProvider Odsp { get; }
        }


        // This is optional.
        // If you want to format the output in a particular way, implement it here.
        // A good example is .\Seatbelt\Commands\Windows\NtlmSettingsCommand.cs
        // If this class does not exist, Seatbelt will use the DefaultTextFormatter class
        [CommandOutputType(typeof(CloudSyncProviderDTO))]
        internal class CloudSyncProviderFormatter : TextFormatterBase
        {
            public CloudSyncProviderFormatter(ITextWriter writer) : base(writer)
            {
               
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (CloudSyncProviderDTO) result;

                WriteLine("  User: {0}", dto.Sid);

                foreach (var item in dto.Odsp.oneDriveList)
                {
                    string accName = item.Key;
                    WriteLine("   {0} :", accName);

                    // These are the core parameters for each account
                    foreach (var subItem in item.Value)
                    {
                        WriteLine("      {0} : {1}", subItem.Key, subItem.Value);
                    }

                    // Now display the mountpoints
                    foreach (string mp in dto.Odsp.AcctoMPMapping[accName])
                    {
                        foreach (var mpSub in dto.Odsp.mpList[mp])
                        {
                            WriteLine("           {0} : {1}", mpSub.Key, mpSub.Value);
                        }
                    }
                }
                // use the following function here if you want to write out to the cmdline. This data will not be serialized.                
            }
        }
    }
}
#endif