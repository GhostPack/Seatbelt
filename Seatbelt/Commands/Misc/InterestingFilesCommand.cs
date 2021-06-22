using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                string? owner = null;
                string? sddl = null;
                try
                {
                    sddl = info.GetAccessControl(System.Security.AccessControl.AccessControlSections.All).GetSecurityDescriptorSddlForm(System.Security.AccessControl.AccessControlSections.All);
                    owner = File.GetAccessControl(file).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                }
                catch { }

                yield return new InterestingFileDTO(
                    $"{file}",
                    info.Length,
                    info.CreationTime,
                    info.LastAccessTime,
                    info.LastWriteTime,
                    sddl,
                    owner
                );
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
                string[]? files = null;
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

        internal class InterestingFileDTO : CommandDTOBase
        {
            public InterestingFileDTO(string path, long size, DateTime dateCreated, DateTime dateAccessed, DateTime dateModified, string? sddl, string? fileOwner)
            {
                Path = path;
                Size = size;
                DateCreated = dateCreated;
                DateAccessed = dateAccessed;
                DateModified = dateModified;
                Sddl = sddl;
                FileOwner = fileOwner;
            }
            public string Path { get; }
            public long Size { get; }
            public DateTime DateCreated { get; }
            public DateTime DateAccessed { get; }
            public DateTime DateModified { get; }
            public string? Sddl { get; }
            public string? FileOwner { get; }
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