#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Browser
{
    // TODO: Bookmarks get collected, but Bookmark folders are not collected
    //          ^ what does this mean exactly? - @harmj0y
    internal class InternetExplorerFavoritesCommand : CommandBase
    {
        public override string Command => "IEFavorites";
        public override string Description => "Internet Explorer favorites";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;


        public InternetExplorerFavoritesCommand(Runtime runtime) : base(runtime)
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

                var userIEBookmarkPath = $"{dir}\\Favorites\\";
                if (!Directory.Exists(userIEBookmarkPath))
                {
                    continue;
                }

                var bookmarkPaths = Directory.GetFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories);
                if (bookmarkPaths.Length == 0)
                {
                    continue;
                }

                var Favorites = new List<string>();

                foreach (var bookmarkPath in bookmarkPaths)
                {
                    using (var rdr = new StreamReader(bookmarkPath))
                    {
                        string line;
                        var url = "";

                        while ((line = rdr.ReadLine()) != null)
                        {
                            if (!line.StartsWith("URL=", StringComparison.InvariantCultureIgnoreCase))
                            {
                                continue;
                            }

                            if (line.Length > 4)
                            {
                                url = line.Substring(4);
                            }

                            break;
                        }

                        Favorites.Add(url.Trim());
                    }
                }

                yield return new InternetExplorerFavoritesDTO()
                {
                    UserName = userName,
                    Favorites = Favorites
                };
            }
        }

        internal class InternetExplorerFavoritesDTO : CommandDTOBase
        {
            public string UserName { get; set; }
            public List<string> Favorites { get; set; }
        }

        [CommandOutputType(typeof(InternetExplorerFavoritesDTO))]
        internal class InternetExplorerFavoritesFormatter : TextFormatterBase
        {
            public InternetExplorerFavoritesFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (InternetExplorerFavoritesDTO)result;

                WriteLine($"Favorites ({dto.UserName}):\n");

                foreach (var favorite in dto.Favorites)
                {
                    WriteLine($"  {favorite}");
                }
                WriteLine();
            }
        }
    }
}
#nullable enable