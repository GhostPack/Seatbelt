#nullable disable
using System;
using System.Collections.Generic;
using Seatbelt.Util;
using Microsoft.Win32;

namespace Seatbelt.Commands.Windows
{
    enum NetworkCategory
    {
        PUBLIC = 0,
        HOME = 1,
        WORK = 2
    }

    // ref - https://social.technet.microsoft.com/Forums/windows/en-US/b0e13a16-51a6-4aca-8d44-c85e097f882b/nametype-in-nla-information-for-a-network-profile
    enum NetworkType
    {
        WIRED = 6,
        VPN = 23,
        WIRELESS = 25,
        MOBILE_BROADBAND = 243
    }

    internal class NetworkProfilesCommand : CommandBase
    {
        public override string Command => "NetworkProfiles";
        public override string Description => "Windows network profiles";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public NetworkProfilesCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (!SecurityUtil.IsHighIntegrity() && !ThisRunTime.ISRemote())
            {
                WriteError("Unable to collect. Must be an administrator.");
                yield break;
            }

            var profileGUIDs = ThisRunTime.GetSubkeyNames(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\");
            foreach (var profileGUID in profileGUIDs)
            {
                var ProfileName = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{profileGUID}", "ProfileName");

                var Description = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{profileGUID}", "Description");

                var NetworkCategory = (NetworkCategory)ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{profileGUID}", "Category");

                var NetworkType = (NetworkType)ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{profileGUID}", "NameType");

                var Managed = ThisRunTime.GetDwordValue(RegistryHive.LocalMachine, $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{profileGUID}", "Managed");

                var dateCreatedBytes = ThisRunTime.GetBinaryValue(RegistryHive.LocalMachine, $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{profileGUID}", "DateCreated");
                var DateCreated = ConvertBinaryDateTime(dateCreatedBytes);

                var dateLastConnectedBytes = ThisRunTime.GetBinaryValue(RegistryHive.LocalMachine, $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{profileGUID}", "DateCreated");
                var DateLastConnected = ConvertBinaryDateTime(dateLastConnectedBytes);

                yield return new NetworkProfilesDTO()
                {
                    ProfileName = ProfileName,
                    Description = Description,
                    NetworkCategory = NetworkCategory,
                    NetworkType = NetworkType,
                    Managed = Managed,
                    DateCreated = DateCreated,
                    DateLastConnected = DateLastConnected,
                };
            }
        }

        public DateTime ConvertBinaryDateTime(byte[] bytes)
        {
            // helper that does some stupid Microsoft format conversion

            if (bytes == null || bytes.Length == 0)
            {
                return new DateTime();
            }

            // ...yes, I know this conversion is stupid
            //  little endian, bytes transformed to text hex and then converted to an int
            //  ref-  http://cfed-ttf.blogspot.com/2009/08/decoding-datecreated-and.html
            var year = Convert.ToInt32($"{bytes[1]:X2}{bytes[0]:X2}", 16);
            var month = Convert.ToInt32($"{bytes[3]:X2}{bytes[2]:X2}", 16);
            var weekday = Convert.ToInt32($"{bytes[5]:X2}{bytes[4]:X2}", 16);
            var day = Convert.ToInt32($"{bytes[7]:X2}{bytes[6]:X2}", 16);
            var hour = Convert.ToInt32($"{bytes[9]:X2}{bytes[8]:X2}", 16);
            var min = Convert.ToInt32($"{bytes[11]:X2}{bytes[10]:X2}", 16);
            var sec = Convert.ToInt32($"{bytes[13]:X2}{bytes[12]:X2}", 16);

            return new DateTime(year, month, day, hour, min, sec);
        }

        internal class NetworkProfilesDTO : CommandDTOBase
        {
            public string ProfileName { get; set; }
            public string Description { get; set; }
            public NetworkCategory NetworkCategory { get; set; }
            public NetworkType NetworkType { get; set; }
            public object Managed { get; set; }
            public DateTime DateCreated { get; set; }
            public DateTime DateLastConnected { get; set; }
        }
    }
}
#nullable enable