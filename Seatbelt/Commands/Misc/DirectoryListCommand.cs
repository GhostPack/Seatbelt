#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    internal class DirectoryListCommand : CommandBase
    {
        public override string Command => "dir";

        public override string Description =>
            "Lists files/folders. By default, lists users' downloads, documents, and desktop folders (arguments == [directory] [depth] [regex] [boolIgnoreErrors]";

        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public DirectoryListCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            string directory = null;
            Regex regex = null;
            int depth;
            var ignoreErrors = false;

            WriteHost("  {0,-10} {1,-10} {2,-9} {3}\n", "LastAccess", "LastWrite", "Size", "Path");

            if (args.Length == 0)
            {
                directory = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
                regex = new Regex(@"^(?!.*desktop\.ini).*\\(Documents|Downloads|Desktop)\\", RegexOptions.IgnoreCase);
                depth = 2;
                ignoreErrors = true;
            }
            else if (args.Length == 1)
            {
                directory = args[0];
                depth = 0;
            }
            else if (args.Length == 2)
            {
                directory = args[0];
                depth = int.Parse(args[1]);
            }
            else if (args.Length == 3)
            {
                directory = args[0];
                depth = int.Parse(args[1]);
                regex = new Regex(args[2], RegexOptions.IgnoreCase);
            }
            else
            {
                directory = args[0];
                depth = int.Parse(args[1]);
                regex = new Regex(args[2], RegexOptions.IgnoreCase);
                ignoreErrors = true;
            }

            foreach (var file in ListDirectory(directory, regex, depth, ignoreErrors))
            {
                yield return file;
            }
        }

        private IEnumerable<DirectoryListDTO> ListDirectory(string path, Regex regex, int depth,
            bool ignoreErrors)
        {
            if (depth < 0)
            {
                yield break;
            }

            var dirList = new List<string>();
            string[] directories = null;
            try
            {
                directories = Directory.GetDirectories(path);
            }
            catch (Exception e)
            {
                if (!ignoreErrors)
                {
                    WriteError(e.ToString());
                }

                yield break;
            }

            foreach (var dir in directories)
            {
                dirList.Add(dir);
                if (regex != null && !regex.IsMatch(dir))
                {
                    continue;
                }

                if (!dir.EndsWith("\\"))
                {
                    yield return WriteOutput(dir + "\\", 0);
                }
                else
                {
                    yield return WriteOutput(dir, 0);
                }
            }



            string[] files = null;
            try
            {
                files = Directory.GetFiles(path);
            }
            catch (Exception e)
            {
                if (!ignoreErrors)
                {
                    throw e;
                }

                yield break;
            }

            foreach (var file in files)
            {
                if (regex != null && !regex.IsMatch(file))
                {
                    continue;
                }

                long size = 0;
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

            foreach (var dir in dirList)
            {
                foreach (var file in ListDirectory(dir, regex, (depth - 1), ignoreErrors))
                {
                    yield return file;
                }
            }
        }

        private DirectoryListDTO WriteOutput(string path, long size)
        {
            var lastAccess = Directory.GetLastAccessTime(path);
            var lastWrite = Directory.GetLastWriteTime(path);

            return new DirectoryListDTO()
            {
                LastAccess = lastAccess,
                LastWrite = lastWrite,
                Size = size,
                Path = path
            };
        }
    }

    internal class DirectoryListDTO : CommandDTOBase
    {
        public DateTime LastAccess { get; set; }
        public DateTime LastWrite { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
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
            WriteLine("  {0,-10} {1,-10} {2,-9} {3}", dto.LastWrite.ToString("yy-MM-dd"), dto.LastAccess.ToString("yy-MM-dd"), BytesToString(dto.Size), dto.Path);
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
#nullable enable