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
    internal class ChromiumPresenceCommand : CommandBase
    {
        public override string Command => "ChromiumPresence";
        public override string Description => "Checks if interesting Chrome/Edge/Brave/Opera files exist";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Chromium, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ChromiumPresenceCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            string chromeVersion = "";

            if (!ThisRunTime.ISRemote())
            {
                // TODO: translate the chrome path to a UNC path
                var chromePath = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe", "");

                if (chromePath != null)
                {
                    chromeVersion = FileVersionInfo.GetVersionInfo(chromePath).ProductVersion;
                }
            }

            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                string[] paths = {
                    "\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\",
                    "\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default\\",
                    "\\AppData\\Local\\BraveSoftware\\Brave-Browser\\User Data\\Default\\",
                    "\\AppData\\Roaming\\Opera Software\\Opera Stable\\"
                };

                foreach (string path in paths)
                {
                    var chromeBasePath = $"{dir}{path}";

                    if (!Directory.Exists(chromeBasePath))
                    {
                        continue;
                    }
                    
                    var history = new DateTime();
                    var cookies = new DateTime();
                    var loginData = new DateTime();

                    var userChromeHistoryPath = $"{chromeBasePath}History";
                    if (File.Exists(userChromeHistoryPath))
                    {
                        history = File.GetLastWriteTime(userChromeHistoryPath);
                    }

                    var userChromeCookiesPath = $"{chromeBasePath}Cookies";
                    if (File.Exists(userChromeCookiesPath))
                    {
                        cookies = File.GetLastWriteTime(userChromeCookiesPath);
                    }

                    var userChromeLoginDataPath = $"{chromeBasePath}LoginData";
                    if (File.Exists(userChromeLoginDataPath))
                    {
                        loginData = File.GetLastWriteTime(userChromeLoginDataPath);
                    }

                    if (history != DateTime.MinValue || cookies != DateTime.MinValue || loginData != DateTime.MinValue)
                    {
                        yield return new ChromiumPresenceDTO(
                            $"{chromeBasePath}",
                            history,
                            cookies,
                            loginData,
                            chromeVersion
                        );
                    }
                }
            }
        }

        internal class ChromiumPresenceDTO : CommandDTOBase
        {
            public ChromiumPresenceDTO(string folder, DateTime historyLastModified, DateTime cookiesLastModified, DateTime loginDataLastModified, string chromeVersion)
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

        [CommandOutputType(typeof(ChromiumPresenceDTO))]
        internal class ChromiumPresenceFormatter : TextFormatterBase
        {
            public ChromiumPresenceFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (ChromiumPresenceDTO)result;

                WriteLine("\r\n  {0}\n", dto.Folder);
                if (dto.HistoryLastModified != DateTime.MinValue)
                {
                    WriteLine("    'History'     ({0})  :  Run the 'ChromiumHistory' command", dto.HistoryLastModified);
                }
                if (dto.CookiesLastModified != DateTime.MinValue)
                {
                    WriteLine("    'Cookies'     ({0})  :  Run SharpDPAPI/SharpChrome or the Mimikatz \"dpapi::chrome\" module", dto.CookiesLastModified);
                }
                if (dto.LoginDataLastModified != DateTime.MinValue)
                {
                    WriteLine("    'Login Data'  ({0})  :  Run SharpDPAPI/SharpChrome or the Mimikatz \"dpapi::chrome\" module", dto.LoginDataLastModified);
                }

                if (dto.Folder.Contains("Google"))
                {
                    WriteLine("     Chrome Version                       :  {0}", dto.ChromeVersion);
                    if (dto.ChromeVersion.StartsWith("8"))
                    {
                        WriteLine("         Version is 80+, new DPAPI scheme must be used");
                    }
                }
            }
        }
    }
}
