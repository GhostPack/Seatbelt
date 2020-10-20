using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;


namespace Seatbelt.Commands
{
    enum AuditType
    {
        Success = 1,
        Failure = 2,
        SuccessAndFailure = 3
    }

    class AuditEntry
    {
        public AuditEntry(string target, string subcategory, string subcategoryGuid, AuditType auditType)
        {
            Target = target;
            Subcategory = subcategory;
            SubcategoryGUID = subcategoryGuid;
            AuditType = auditType;
        }
        public string Target { get; }
        public string Subcategory { get; }
        public string SubcategoryGUID { get; }
        public AuditType AuditType { get; }
    }

    internal class AuditPoliciesCommand : CommandBase
    {
        public override string Command => "AuditPolicies";
        public override string Description => "Enumerates classic and advanced audit policy settings";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // TODO : remote conversion, need to implement searching for remote files

        // reference - https://github.com/trustedsec/HoneyBadger/blob/master/modules/post/windows/gather/ts_get_policyinfo.rb

        public AuditPoliciesCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var searchPath = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\GroupPolicy\\DataStore\\0\\sysvol\\";
            var sysnativeSearchPath = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\Sysnative\\GroupPolicy\\DataStore\\0\\sysvol\\";
            var files = FindFiles(searchPath, "audit.csv");
            // var sysnativeFiles = FindFiles(sysnativeSearchPath, "audit.csv");  // TODO: Need to implement parsing of this
            var classicFiles = FindFiles(searchPath, "GptTmpl.inf");

            foreach (var classicFilePath in classicFiles)
            {
                var result = ParseGPOPath(classicFilePath);
                var domain = result[0];
                var gpo = result[1];

                //ParseClassicPolicy
                var sections = IniFileHelper.ReadSections(classicFilePath);

                if (!sections.Contains("Event Audit"))
                    continue;

                var settings = ParseClassicPolicy(classicFilePath);

                yield return new AuditPolicyGPO(
                    classicFilePath,
                    domain,
                    gpo,
                    "classic",
                    settings
                );
            }

            foreach (var filePath in files)
            {
                var result = ParseGPOPath(filePath);
                var domain = result[0];
                var gpo = result[1];

                var settings = ParseAdvancedPolicy(filePath);

                yield return new AuditPolicyGPO(
                    filePath,
                    domain,
                    gpo,
                    "advanced",
                    settings
                );
            }
        }

        public string[] ParseGPOPath(string path)
        {
            // returns an array of the domain and GPO GUID from an audit.csv (or GptTmpl.inf) path

            var searchPath = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\GroupPolicy\\DataStore\\0\\sysvol\\";
            var sysnativeSearchPath = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\Sysnative\\GroupPolicy\\DataStore\\0\\sysvol\\";

            if (Regex.IsMatch(path, "System32"))
            {
                var rest = path.Substring(searchPath.Length, path.Length - searchPath.Length);
                var parts = rest.Split('\\');
                string[] result = { parts[0], parts[2] };
                return result;
            }
            else
            {
                var rest = path.Substring(sysnativeSearchPath.Length, path.Length - sysnativeSearchPath.Length);
                var parts = rest.Split('\\');
                string[] result = { parts[0], parts[2] };
                return result;
            }
        }

        public List<AuditEntry> ParseClassicPolicy(string path)
        {
            // parses a "classic" auditing policy (GptTmpl.inf), returning a list of AuditEntries

            var results = new List<AuditEntry>();

            var settings = IniFileHelper.ReadKeyValuePairs("Event Audit", path);
            foreach (var setting in settings)
            {
                var parts = setting.Split('=');

                var result = new AuditEntry(
                    "",
                    parts[0],
                    "",
                    (AuditType)Int32.Parse(parts[1])
                );

                results.Add(result);
            }

            return results;
        }

        public List<AuditEntry> ParseAdvancedPolicy(string path)
        {
            // parses a "advanced" auditing policy (audit.csv), returning a list of AuditEntries

            var results = new List<AuditEntry>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (values[0].Equals("Machine Name")) // skip the header
                        continue;

                    // CSV  lines:
                    // Machine Name,Policy Target,Subcategory,Subcategory GUID,Inclusion Setting,Exclusion Setting,Setting Value

                    string
                        target = values[1],
                        subcategory = values[2],
                        subcategoryGuid = values[3];
                    var auditType = (AuditType)int.Parse(values[6]);

                    results.Add(new AuditEntry(
                        target,
                        subcategory,
                        subcategoryGuid,
                        auditType
                    ));
                }
            }

            return results;
        }

        public static List<string> FindFiles(string path, string pattern)
        {
            // finds files matching one or more patterns under a given path, recursive
            // adapted from http://csharphelper.com/blog/2015/06/find-files-that-match-multiple-patterns-in-c/
            //      pattern: "*pass*;*.png;"

            var files = new List<string>();
            try
            {
                var filesUnfiltered = GetFiles(path).ToList();

                files.AddRange(filesUnfiltered.Where(f => f.Contains(pattern.Trim('*'))));
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }

            return files;
        }

        // FROM: https://stackoverflow.com/a/929418
        private static IEnumerable<string> GetFiles(string path)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (var subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception)
                {
                    // Eat it
                }
                string[]? files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception)
                {
                    // Eat it
                }

                if(files == null)
                    continue;
                ;
                foreach (var f in files)
                {
                    yield return f;
                }
            }
        }

        internal class AuditPolicyGPO : CommandDTOBase
        {
            public AuditPolicyGPO(string path, string domain, string gpo, string type, List<AuditEntry> settings)
            {
                Path = path;
                Domain = domain;
                GPO = gpo;
                Type = type;
                Settings = settings;
            }
            public string Path { get; }
            public string Domain { get; }
            public string GPO { get; }
            public string Type { get; }
            public List<AuditEntry> Settings { get; }
        }

        [CommandOutputType(typeof(AuditPolicyGPO))]
        internal class AuditPolicyormatter : TextFormatterBase
        {
            public AuditPolicyormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (AuditPolicyGPO)result;

                //WriteLine(" {0,-40} : {1}", "File", dto.Path);
                WriteLine(" {0,-40} : {1}", "Domain", dto.Domain);
                WriteLine(" {0,-40} : {1}", "GPO", dto.GPO);
                WriteLine(" {0,-40} : {1}", "Type", dto.Type);
                foreach (var entry in dto.Settings)
                {
                    WriteLine(" {0,40} : {1}", entry.Subcategory, entry.AuditType);
                }
                WriteLine();
            }
        }
    }
}