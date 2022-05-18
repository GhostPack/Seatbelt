#nullable disable
using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using System;

// Any command you create should not generate compiler warnings
namespace Seatbelt.Commands.Windows
{
    // Module to enumerate information from tools such as OneDrive
    // Stuart Morgan <stuart.morgan@mwrinfosecurity.com> @ukstufus
    class OneDriveSyncProvider
    {
        // Stores the mapping between a sync ID and mount point
        public Dictionary<string, Dictionary<string, string>> mpList = new Dictionary<string, Dictionary<string, string>>();
        // Stores the list of OneDrive accounts configured in the registry
        public Dictionary<string, Dictionary<string, string>> oneDriveList = new Dictionary<string, Dictionary<string, string>>();
        // Stores the mapping between the account and the mountpoint IDs
        public Dictionary<string, List<string>> AcctoMPMapping = new Dictionary<string, List<string>>();
        // Stores the 'used' scopeIDs (to identify orphans)
        public List<string> usedScopeIDs = new List<string>();
    }

    internal class CloudSyncProviderCommand : CommandBase
    {
        public override string Command => "CloudSyncProviders";
        public override string Description => "All configured Office 365 endpoints (tenants and teamsites) which are synchronised by OneDrive.";
        public override CommandGroup[] Group => new[] {CommandGroup.User}; 
        public override bool SupportRemote => true;                       
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
                    Boolean business = false;
                    Dictionary<string, string> account = new Dictionary<string, string>();
                    foreach (string x in new List<string> { "DisplayName", "Business", "ServiceEndpointUri", "SPOResourceId", "UserEmail", "UserFolder", "UserName", "WebServiceUrl" })
                    {
                        var result = ThisRunTime.GetStringValue(RegistryHive.Users, $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts\\{acc}", x);
                        if (!string.IsNullOrEmpty(result))
                            account[x] = result;
                        if (x == "Business")
                            business = (String.Compare(result,"1")==0) ? true : false;
                    }
                    var odMountPoints = ThisRunTime.GetValues(RegistryHive.Users, $"{sid}\\Software\\Microsoft\\OneDrive\\Accounts\\{acc}\\ScopeIdToMountPointPathCache");
                    List<string> ScopeIds = new List<string>();
                    if (business == true)
                    {
                        foreach (var mp in odMountPoints)
                        {
                            ScopeIds.Add(mp.Key);
                        }
                    } else
                    {
                        ScopeIds.Add(acc); // If its a personal account, OneDrive adds it as 'Personal' or the name of the account, not by the ScopeId itself. You can only have one personal account.
                    }
                    o.AcctoMPMapping[acc] = ScopeIds;
                    o.oneDriveList[acc] = account;
                    o.usedScopeIDs.AddRange(ScopeIds);
                }

                yield return new CloudSyncProviderDTO(
                    sid,
                    o
                    );

            }
        }

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

                WriteLine("  {0} :", dto.Sid);

                foreach (var item in dto.Odsp.oneDriveList)
                {

                    // If there are no parameters, move on
                    if (item.Value.Count == 0)
                        continue;

                    string accName = item.Key;
                    WriteLine("\r\n    {0} :", accName);

                    // These are the core parameters for each account
                    foreach (var subItem in item.Value)
                    {
                        WriteLine("      {0} : {1}", subItem.Key, subItem.Value);
                    }
                   
                    // Now display the mountpoints
                    foreach (string mp in dto.Odsp.AcctoMPMapping[accName])
                    {
                        WriteLine("");

                        if (!dto.Odsp.mpList.ContainsKey(mp))
                            continue;
                        foreach (var mpSub in dto.Odsp.mpList[mp])
                        {
                            if (mpSub.Key == "LastModifiedTime")
                            {
                                DateTime parsedDate;
                                DateTime.TryParse(mpSub.Value, out parsedDate);
                                string formattedDate = parsedDate.ToString("ddd dd MMM yyyy HH:mm:ss");
                                WriteLine("      | {0} : {1} ({2})", mpSub.Key, mpSub.Value, formattedDate);
                            }
                            else
                            {
                                WriteLine("      | {0} : {1}", mpSub.Key, mpSub.Value);
                            }
                        }
                    }
                }

                // Now look for 'Orphaned' accounts
                List<string> AllScopeIds = new List<string>(dto.Odsp.mpList.Keys);
                WriteLine("\r\n    Orphaned :");
                foreach (string scopeid in AllScopeIds)
                {
                    if (!dto.Odsp.usedScopeIDs.Contains(scopeid))
                    {
                        foreach (var mpSub in dto.Odsp.mpList[scopeid])
                        {
                            if (mpSub.Key == "LastModifiedTime")
                            {
                                DateTime parsedDate;
                                DateTime.TryParse(mpSub.Value, out parsedDate);
                                string formattedDate = parsedDate.ToString("ddd dd MMM yyyy HH:mm:ss");
                                WriteLine("      {0} : {1} ({2})", mpSub.Key, mpSub.Value, formattedDate);
                            }
                            else
                            {
                                WriteLine("      {0} : {1}", mpSub.Key, mpSub.Value);
                            }
                        }
                        WriteLine("");
                    }
                }
                WriteLine("");                            
            }
        }
    }
}