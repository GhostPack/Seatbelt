using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    public class DirectoryQuery
    {
        public DirectoryQuery(string path, int depth)
        {
            Path = path;
            Depth = depth;
        }
        public string Path { get; }
        public int Depth { get; }
    }
    internal class DirectoryListCommand : CommandBase
    {
        public override string Command => "dir";

        public override string Description =>
            "Lists files/folders. By default, lists users' downloads, documents, and desktop folders (arguments == [directory] [maxDepth] [regex] [boolIgnoreErrors]";

        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;
        private Stack<DirectoryQuery> _dirList = new Stack<DirectoryQuery>();

        public DirectoryListCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            string directory;
            Regex? regex = null;
            int maxDepth;
            var ignoreErrors = false;

            WriteHost("  {0,-10} {1,-10} {2,-9} {3}\n", "LastAccess", "LastWrite", "Size", "Path");

            if (args.Length == 0)
            {
                directory = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
                regex = new Regex(@"^(?!.*desktop\.ini).*\\(Documents|Downloads|Desktop)\\", RegexOptions.IgnoreCase);
                maxDepth = 2;
                ignoreErrors = true;
            }
            else if (args.Length == 1)
            {
                directory = args[0];
                maxDepth = 0;
            }
            else if (args.Length == 2)
            {
                directory = args[0];
                maxDepth = int.Parse(args[1]);
            }
            else if (args.Length == 3)
            {
                directory = args[0];
                maxDepth = int.Parse(args[1]);
                regex = new Regex(args[2], RegexOptions.IgnoreCase);
            }
            else
            {
                directory = args[0];
                maxDepth = int.Parse(args[1]);
                regex = new Regex(args[2], RegexOptions.IgnoreCase);
                ignoreErrors = true;
            }

            directory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory));
            if (!directory.EndsWith(@"\")) directory += @"\";

            _dirList.Push(new DirectoryQuery(directory, 0));

            while (_dirList.Any())
            {
                var query = _dirList.Pop();

                foreach (var dto in GetDirectories(regex, ignoreErrors, query, maxDepth))
                    yield return dto;

                foreach (var dto in GetFiles(regex, ignoreErrors, query))
                    yield return dto;
            }
        }
        
        private IEnumerable<DirectoryListDTO> GetFiles(Regex? regex, bool ignoreErrors, DirectoryQuery query)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(query.Path);
            }
            catch (Exception e)
            {
                files = new string[] { };
                if (!ignoreErrors)
                {
                    WriteError(e.ToString());
                }
            }

            foreach (var file in files)
            {
                if (regex != null && !regex.IsMatch(file) && !regex.IsMatch(query.Path))
                {
                    continue;
                }

                long? size = null;
                try
                {
                    var info = new FileInfo(file);
                    size = info.Length;
                }
                catch
                {
                }

                yield return WriteOutput(file, size);
            }
        }

        private IEnumerable<DirectoryListDTO> GetDirectories(Regex? regex, bool ignoreErrors, DirectoryQuery query, int maxDepth)
        {
            string[] directories;
            try
            {
                directories = Directory.GetDirectories(query.Path);
            }
            catch (Exception e)
            {
                directories = new string[] { };
                if (!ignoreErrors)
                {
                    WriteError(e.ToString());
                }
            }

            foreach (var dir in directories)
            {
                var matchesIncludeFilter = regex == null || regex.IsMatch(dir);

                if(query.Depth+1 <= maxDepth) _dirList.Push(new DirectoryQuery(dir, query.Depth + 1));

                if (!matchesIncludeFilter) continue;

                if (!dir.EndsWith("\\"))
                {
                    yield return WriteOutput(dir + "\\", 0);
                }
                else
                {
                    yield return WriteOutput(dir, 0);
                }
            }
        }

        private DirectoryListDTO WriteOutput(string path, long? size)
        {
            DateTime? lastAccess = null;
            DateTime? lastWrite = null;

            try
            {
                lastAccess = Directory.GetLastAccessTime(path);
                lastWrite = Directory.GetLastWriteTime(path);
            }
            catch
            {
            }

            return new DirectoryListDTO(
                lastAccess,
                lastWrite,
                size,
                path
            );
        }
    }

    internal class DirectoryListDTO : CommandDTOBase
    {
        public DirectoryListDTO(DateTime? lastAccess, DateTime? lastWrite, long? size, string path)
        {
            LastAccess = lastAccess;
            LastWrite = lastWrite;
            Size = size;
            Path = path;
        }
        public DateTime? LastAccess { get; }
        public DateTime? LastWrite { get; }
        public long? Size { get; }
        public string Path { get; }
    }

    [CommandOutputType(typeof(DirectoryListDTO))]
    internal class DirectoryListTextFormatter : TextFormatterBase
    {
        public DirectoryListTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (DirectoryListDTO)result;
            WriteLine("  {0,-10} {1,-10} {2,-9} {3}", 
                dto.LastWrite?.ToString("yy-MM-dd"), 
                dto.LastAccess?.ToString("yy-MM-dd"),
                dto.Size == null ? "???" : BytesToString(dto.Size.Value), 
                dto.Path);
        }

        private string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num) + suf[place];
        }
    }
}