using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using System.Diagnostics;
using Seatbelt.Util;
using Microsoft.Win32;

namespace Seatbelt.Commands.Browser
{
    internal class ChromePresenceCommand : CommandBase
    {
        public override string Command => "ChromePresence";
        public override string Description => "Checks if interesting Google Chrome files exist";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Chrome };
        public override bool SupportRemote => false;

        public ChromePresenceCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            string chromeVersion = "";

            var chromePath = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe", "");
            if (chromePath != null)
            {
                chromeVersion = FileVersionInfo.GetVersionInfo(chromePath).ProductVersion;
            }

            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);
            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                var chromeBasePath = $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\";
                if (!Directory.Exists(chromeBasePath))
                {
                    continue;
                }

                var history = new DateTime();
                var cookies = new DateTime();
                var loginData = new DateTime();

                var userChromeHistoryPath = $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History";
                if (File.Exists(userChromeHistoryPath))
                {
                    history = File.GetLastWriteTime(userChromeHistoryPath);
                }

                var userChromeCookiesPath = $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies";
                if (File.Exists(userChromeCookiesPath))
                {
                    cookies = File.GetLastWriteTime(userChromeCookiesPath);
                }

                var userChromeLoginDataPath = $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data";
                if (File.Exists(userChromeLoginDataPath))
                {
                    loginData = File.GetLastWriteTime(userChromeLoginDataPath);
                }

                if (history != DateTime.MinValue || cookies != DateTime.MinValue || loginData != DateTime.MinValue)
                {
                    yield return new ChromePresenceDTO(
                        $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\",
                        history,
                        cookies,
                        loginData,
                        chromeVersion
                    );
                }
            }
        }

        internal class ChromePresenceDTO : CommandDTOBase
        {
            public ChromePresenceDTO(string folder, DateTime historyLastModified, DateTime cookiesLastModified, DateTime loginDataLastModified, string chromeVersion)
            {
                Folder = folder;
                HistoryLastModified = historyLastModified;
                CookiesLastModified = cookiesLastModified;
                LoginDataLastModified = loginDataLastModified;
                ChromeVersion = chromeVersion;
            }

            public string Folder { get; }
            public DateTime HistoryLastModified { get; }
            public DateTime CookiesLastModified { get; }
            public DateTime LoginDataLastModified { get; }
            public string ChromeVersion { get; }
        }

        [CommandOutputType(typeof(ChromePresenceDTO))]
        internal class ChromePresenceFormatter : TextFormatterBase
        {
            public ChromePresenceFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (ChromePresenceDTO)result;

                WriteLine("  {0}\n", dto.Folder);
                if (dto.HistoryLastModified != DateTime.MinValue)
                {
                    WriteLine("    'History'     ({0})  :  Run the 'ChromeHistory' command", dto.HistoryLastModified);
                }
                if (dto.CookiesLastModified != DateTime.MinValue)
                {
                    WriteLine("    'Cookies'     ({0})  :  Run SharpDPAPI/SharpChrome or the Mimikatz \"dpapi::chrome\" module", dto.CookiesLastModified);
                }
                if (dto.LoginDataLastModified != DateTime.MinValue)
                {
                    WriteLine("    'Login Data'  ({0})  :  Run SharpDPAPI/SharpChrome or the Mimikatz \"dpapi::chrome\" module", dto.LoginDataLastModified);
                }

                WriteLine("     Chrome Version                       :  {0}", dto.ChromeVersion);
                if (dto.ChromeVersion.StartsWith("8"))
                {
                    WriteLine("         Version is 80+, new DPAPI scheme must be used");
                }
            }
        }
    }
}
