using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Browser
{
    // TODO: Listing bookmarks does not account for bookmark folders. It lists folders, but not nested bookmarks/folders inside another folder )
    internal class Bookmark
    {
        public Bookmark(string name, string url)
        {
            Name = name;
            Url = url;
        }
        public string Name { get; }
        public string Url { get; }
    }

    internal class ChromeBookmarksCommand : CommandBase
    {
        public override string Command => "ChromeBookmarks";
        public override string Description => "Parses any found Chrome bookmark files";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc, CommandGroup.Chrome };
        public override bool SupportRemote => false;

        public ChromeBookmarksCommand(Runtime runtime) : base(runtime)
        {
        }
        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);
            foreach (var dir in dirs)
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                // TODO: Account for other profiles
                var userChromeBookmarkPath = $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks";

                // parses a Chrome bookmarks file
                if (!File.Exists(userChromeBookmarkPath))
                    continue;

                var bookmarks = new List<Bookmark>();

                try
                {
                    var contents = File.ReadAllText(userChromeBookmarkPath);

                    // reference: http://www.tomasvera.com/programming/using-javascriptserializer-to-parse-json-objects/
                    var json = new JavaScriptSerializer();
                    var deserialized = json.Deserialize<Dictionary<string, object>>(contents);
                    var roots = (Dictionary<string, object>)deserialized["roots"];
                    var bookmarkBar = (Dictionary<string, object>)roots["bookmark_bar"];
                    var children = (ArrayList)bookmarkBar["children"];

                    foreach (Dictionary<string, object> entry in children)
                    {
                        var bookmark = new Bookmark(
                            $"{entry["name"].ToString().Trim()}",
                            entry.ContainsKey("url") ? $"{entry["url"]}" : "(Bookmark Folder?)"
                            );

                        bookmarks.Add(bookmark);
                    }
                }
                catch (Exception exception)
                {
                    WriteError(exception.ToString());
                }

                yield return new ChromeBookmarksDTO(
                    userName,
                    bookmarks
                );
            }
        }

        internal class ChromeBookmarksDTO : CommandDTOBase
        {
            public ChromeBookmarksDTO(string userName, List<Bookmark> bookmarks)
            {
                UserName = userName;
                Bookmarks = bookmarks;
            }
            public string UserName { get; }
            public List<Bookmark> Bookmarks { get; }
        }

        [CommandOutputType(typeof(ChromeBookmarksDTO))]
        internal class ChromeBookmarksFormatter : TextFormatterBase
        {
            public ChromeBookmarksFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (ChromeBookmarksDTO)result;

                WriteLine($"Bookmarks ({dto.UserName}):\n");

                foreach (var bookmark in dto.Bookmarks)
                {
                    WriteLine($"    Name : {bookmark.Name}");
                    WriteLine($"    URL  : {bookmark.Url}\n");
                }
                WriteLine();
            }
        }
    }
}
