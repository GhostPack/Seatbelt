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

    internal class ChromiumBookmarksCommand : CommandBase
    {
        public override string Command => "ChromiumBookmarks";
        public override string Description => "Parses any found Chrome/Edge/Brave/Opera bookmark files";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc, CommandGroup.Chromium };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ChromiumBookmarksCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var dirs = ThisRunTime.GetDirectories("\\Users\\");

            foreach (var dir in dirs)
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
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
                    // TODO: Account for other profiles
                    var userChromeBookmarkPath = $"{dir}{path}Bookmarks";

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

                    yield return new ChromiumBookmarksDTO(
                        userName,
                        userChromeBookmarkPath,
                        bookmarks
                    );
                }
            }
        }

        internal class ChromiumBookmarksDTO : CommandDTOBase
        {
            public ChromiumBookmarksDTO(string userName, string filePath, List<Bookmark> bookmarks)
            {
                UserName = userName;
                FilePath = filePath;
                Bookmarks = bookmarks;
            }
            public string UserName { get; }
            public string FilePath { get; }
            public List<Bookmark> Bookmarks { get; }
        }

        [CommandOutputType(typeof(ChromiumBookmarksDTO))]
        internal class ChromiumBookmarksFormatter : TextFormatterBase
        {
            public ChromiumBookmarksFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (ChromiumBookmarksDTO)result;

                if (dto.Bookmarks.Count > 0)
                {
                    WriteLine($"Bookmarks ({dto.FilePath}):\n");

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
}
