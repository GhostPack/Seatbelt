#nullable disable
using Microsoft.Win32;
using Seatbelt.Util;
using System;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Seatbelt.Interop;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Seatbelt.Commands.Windows
{
    class OfficeMRU
    {
        public string Product { get; set; }

        public string Type { get; set; }

        public string FileName { get; set; }
    }

    internal class OfficeMRUsCommand : CommandBase
    {
        public override string Command => "OfficeMRUs";
        public override string Description => "Office most recently used file list (last 7 days)";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false; // TODO but a bit more complicated

        public OfficeMRUsCommand(Runtime runtime) : base(runtime)
        {
        }


        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var lastDays = 7;

            // parses recent file shortcuts via COM
            if (args.Length == 1)
            {
                lastDays = int.Parse(args[0]);
            }
            else if (!Runtime.FilterResults)
            {
                lastDays = 30;
            }

            WriteHost("Enumerating Office most recently used files for the last {0} days", lastDays);
            WriteHost("\n  {0,-8}  {1,-23}  {2,-12}  {3}", "App", "User", "LastAccess", "FileName");
            WriteHost("  {0,-8}  {1,-23}  {2,-12}  {3}", "---", "----", "----------", "--------");

            foreach (var file in EnumRecentOfficeFiles(lastDays).OrderByDescending(e => ((OfficeRecentFilesDTO)e).LastAccessDate))
            {
                yield return file;
            }
        }

        private IEnumerable<CommandDTOBase> EnumRecentOfficeFiles(int lastDays)
        {
            foreach (var sid in Registry.Users.GetSubKeyNames())
            {
                if (!sid.StartsWith("S-1") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                string userName = null;
                try
                {
                    userName = Advapi32.TranslateSid(sid);
                }
                catch
                {
                    userName = sid;
                }
                
                var officeVersion = 
                    RegistryUtil.GetSubkeyNames(RegistryHive.Users, $"{sid}\\Software\\Microsoft\\Office")
                    ?.Where(k => float.TryParse(k, NumberStyles.AllowDecimalPoint, new CultureInfo("en-GB"), out _));

                if (officeVersion is null)
                    continue;

                foreach (var version in officeVersion)
                {
                    foreach (OfficeRecentFilesDTO mru in GetMRUsFromVersionKey($"{sid}\\Software\\Microsoft\\Office\\{version}"))
                    {
                        if (mru.LastAccessDate <= DateTime.Now.AddDays(-lastDays)) continue;

                        mru.User = userName;
                        yield return mru;
                    }
                }
            }
        }

        private IEnumerable<CommandDTOBase> GetMRUsFromVersionKey(string officeVersionSubkeyPath)
        {
            var officeApplications = RegistryUtil.GetSubkeyNames(RegistryHive.Users, officeVersionSubkeyPath);
            if (officeApplications == null)
            {
                yield break;
            }

            foreach (var app in officeApplications)
            {
                // 1) HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\<OFFICE APP>\File MRU
                foreach (var mru in GetMRUsValues($"{officeVersionSubkeyPath}\\{app}\\File MRU"))
                {
                    yield return mru;
                }

                // 2) HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Word\User MRU\ADAL_B7C22499E768F03875FA6C268E771D1493149B23934326A96F6CDFEEEE7F68DA72\File MRU
                // or HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Word\User MRU\LiveId_CC4B824314B318B42E93BE93C46A61575D25608BBACDEEEA1D2919BCC2CF51FF\File MRU

                var logonAapps = RegistryUtil.GetSubkeyNames(RegistryHive.Users, $"{officeVersionSubkeyPath}\\{app}\\User MRU");
                if (logonAapps == null)
                    continue;

                foreach (var logonApp in logonAapps)
                {
                    foreach (var mru in GetMRUsValues($"{officeVersionSubkeyPath}\\{app}\\User MRU\\{logonApp}\\File MRU"))
                    {
                        ((OfficeRecentFilesDTO)mru).Application = app;
                        yield return mru;
                    }
                }
            }
        }

        private IEnumerable<CommandDTOBase> GetMRUsValues(string fileMRUKeyPath)
        {
            var values = RegistryUtil.GetValues(RegistryHive.Users, fileMRUKeyPath);
            if (values == null) yield break;
            foreach (string mru in values.Values)
            {
                var m = ParseMruString(mru);
                if (m != null)
                {
                    yield return m;
                }
            }
        }


        private OfficeRecentFilesDTO? ParseMruString(string mru)
        {
            var matches = Regex.Matches(mru, "\\[[a-zA-Z0-9]+?\\]\\[T([a-zA-Z0-9]+?)\\](\\[[a-zA-Z0-9]+?\\])?\\*(.+)");
            if (matches.Count == 0)
            {
                return null;
            }

            long timestamp = 0;
            var dateHexString = matches[0].Groups[1].Value;
            var filename = matches[0].Groups[matches[0].Groups.Count - 1].Value;

            try
            {
                timestamp = long.Parse(dateHexString, NumberStyles.HexNumber);
            }
            catch
            {
                WriteError($"Could not parse MRU timestamp. Parsed timestamp: {dateHexString} MRU value: {mru}");
            }

            return new OfficeRecentFilesDTO()
            {
                Application = "Office",
                LastAccessDate = DateTime.FromFileTimeUtc(timestamp),
                User = null,
                Target = filename
            };
        }

        internal class OfficeRecentFilesDTO : CommandDTOBase
        {
            public string Application { get; set; }
            public string Target { get; set; }
            public DateTime LastAccessDate { get; set; }
            public string User { get; set; }
        }

        [CommandOutputType(typeof(OfficeRecentFilesDTO))]
        internal class OfficeMRUsCommandFormatter : TextFormatterBase
        {
            public OfficeMRUsCommandFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (OfficeRecentFilesDTO)result;

                WriteLine("  {0,-8}  {1,-23}  {2,-12}  {3}", dto.Application, dto.User, dto.LastAccessDate.ToString("yyyy-MM-dd"), dto.Target);
            }
        }
    }
}
#nullable enable