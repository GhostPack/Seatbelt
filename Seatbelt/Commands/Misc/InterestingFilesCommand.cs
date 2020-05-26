#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands
{
    internal class InterestingFilesCommand : CommandBase
    {
        public override string Command => "InterestingFiles";
        public override string Description => "\"Interesting\" files matching various patterns in the user's folder. Note: takes non-trivial time.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false;

        public InterestingFilesCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // TODO: accept patterns on the command line

            // returns files (w/ modification dates) that match the given pattern below
            var patterns = new string[]{
                // Wildcards
                "*pass*",
                "*diagram*",
                "*_rsa*",       // SSH keys

                // Extensions
                "*.doc",
                "*.docx",
                "*.pem",
                "*.pdf",
                "*.pfx",        // Certificate (Code signing, SSL, etc.)
                "*.p12",        // Certificate (Code signing, SSL, etc.) - Mac/Firefox
                "*.ppt",
                "*.pptx",
                "*.vsd",        // Visio Diagrams
                "*.xls",
                "*.xlsx",
                "*.kdb",        // KeePass database
                "*.kdbx",       // KeePass database
                "*.key",

                // Specific file names
                "KeePass.config"
            };

            var searchPath = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var files = FindFiles(searchPath, string.Join(";", patterns));

            WriteHost("\nAccessed      Modified      Path");
            WriteHost("----------    ----------    -----");

            foreach (var file in files)
            {
                var info = new FileInfo(file);

                var owner = "";
                var SDDL = "";
                try
                {
                    SDDL = info.GetAccessControl(System.Security.AccessControl.AccessControlSections.All).GetSecurityDescriptorSddlForm(System.Security.AccessControl.AccessControlSections.All);
                    owner = File.GetAccessControl(file).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                }
                catch { }

                yield return new InterestingFileDTO()
                {
                    Path = $"{file}",
                    FileOwner = owner,
                    Size = info.Length,
                    DateCreated = info.CreationTime,
                    DateAccessed = info.LastAccessTime,
                    DateModified = info.LastWriteTime,
                    Sddl = SDDL
                };
            }
        }

        public static List<string> FindFiles(string path, string patterns)
        {
            // finds files matching one or more patterns under a given path, recursive
            // adapted from http://csharphelper.com/blog/2015/06/find-files-that-match-multiple-patterns-in-c/
            //      pattern: "*pass*;*.png;"

            var files = new List<string>();
            try
            {
                var filesUnfiltered = GetFiles(path).ToList();

                // search every pattern in this directory's files
                foreach (var pattern in patterns.Split(';'))
                {
                    files.AddRange(filesUnfiltered.Where(f => f.Contains(pattern.Trim('*'))));
                }

                //// go recurse in all sub-directories
                //foreach (var directory in Directory.GetDirectories(path))
                //    files.AddRange(FindFiles(directory, patterns));
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
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception)
                {
                    // Eat it
                }

                if (files == null) continue;
                foreach (var f in files)
                {
                    yield return f;
                }
            }
        }

        public static IEnumerable<string> Split(string text, int partLength, StringBuilder sb)
        {
            if (text == null) { sb.AppendLine("[ERROR] Split() - singleLineString"); }
            if (partLength < 1) { sb.AppendLine("[ERROR] Split() - 'columns' must be greater than 0."); }

            var partCount = Math.Ceiling((double)text.Length / partLength);
            if (partCount < 2)
            {
                yield return text;
            }

            for (var i = 0; i < partCount; i++)
            {
                var index = i * partLength;
                var lengthLeft = Math.Min(partLength, text.Length - index);
                var line = text.Substring(index, lengthLeft);
                yield return line;
            }
        }

        internal class InterestingFileDTO : CommandDTOBase
        {
            public string Path { get; set; }
            public string FileOwner { get; set; }
            public long Size { get; set; }
            public DateTime DateCreated { get; set; }
            public DateTime DateAccessed { get; set; }
            public DateTime DateModified { get; set; }
            public string Sddl { get; set; }
        }

        [CommandOutputType(typeof(InterestingFileDTO))]
        internal class InterestingFileFormatter : TextFormatterBase
        {
            public InterestingFileFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (InterestingFileDTO)result;

                WriteLine($"{dto.DateAccessed:yyyy-MM-dd}    {dto.DateModified:yyyy-MM-dd}    {dto.Path}");
            }
        }
    }
}
#nullable enable