using System;
using System.Collections.Generic;
using System.IO;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Browser
{
    internal class InternetExplorerFavoritesCommand : CommandBase
    {
        public override string Command => "IEFavorites";
        public override string Description => "Internet Explorer favorites";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;


        public InternetExplorerFavoritesCommand(Runtime runtime) : base(runtime)
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

                var userFavoritesPath = $"{dir}\\Favorites\\";
                if (!Directory.Exists(userFavoritesPath))
                {
                    continue;
                }

                var bookmarkPaths = Directory.GetFiles(userFavoritesPath, "*.url", SearchOption.AllDirectories);
                if (bookmarkPaths.Length == 0)
                {
                    continue;
                }

                var favorites = new List<string>();

                foreach (var bookmarkPath in bookmarkPaths)
                {
                    using var rdr = new StreamReader(bookmarkPath);
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

                    favorites.Add(url.Trim());
                }

                yield return new InternetExplorerFavoritesDTO(
                    userName,
                    favorites
                );
            }
        }

        internal class InternetExplorerFavoritesDTO : CommandDTOBase
        {
            public InternetExplorerFavoritesDTO(string userName, List<string> favorites)
            {
                UserName = userName;
                Favorites = favorites;
            }
            public string UserName { get; }
            public List<string> Favorites { get; }
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