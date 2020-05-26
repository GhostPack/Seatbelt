using Microsoft.Win32;
using Seatbelt.Util;
using System;
using System.Collections.Generic;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Browser
{
    class TypedUrl
    {
        public TypedUrl(DateTime time, string url)
        {
            Time = time;
            Url = url;
        }
        public DateTime Time { get; }
        public string Url { get; }
    }

    internal class InternetExplorerTypedUrlsCommand : CommandBase
    {
        public override string Command => "IEUrls";
        public override string Description => "Internet Explorer typed URLs (last 7 days, argument == last X days)";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false; // TODO remote , though not sure how useful this would be

        public InternetExplorerTypedUrlsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists Internet explorer history (last 7 days by default)
            var lastDays = 7;

            if (!Runtime.FilterResults)
            {
                lastDays = 90;
            }

            if (args.Length >= 1)
            {
                if (!int.TryParse(args[0], out lastDays))
                {
                    throw new ArgumentException("Argument is not an integer");
                }
            }

            var startTime = DateTime.Now.AddDays(-lastDays);

            WriteHost($"Internet Explorer typed URLs for the last {lastDays} days\n");

            var SIDs = Registry.Users.GetSubKeyNames();
            foreach (var sid in SIDs)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                var settings = RegistryUtil.GetValues(RegistryHive.Users, $"{sid}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs");
                if ((settings == null) || (settings.Count <= 1))
                {
                    continue;
                }

                var URLs = new List<TypedUrl>();

                foreach (var kvp in settings)
                {
                    var timeBytes = RegistryUtil.GetBinaryValue(RegistryHive.Users, $"{sid}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime", kvp.Key.Trim());
                    if (timeBytes == null)
                        continue;

                    var timeLong = BitConverter.ToInt64(timeBytes, 0);
                    var urlTime = DateTime.FromFileTime(timeLong);
                    if (urlTime > startTime)
                    {
                        URLs.Add(new TypedUrl(
                            urlTime,
                            kvp.Value.ToString().Trim()
                            ));
                    }
                }

                yield return new InternetExplorerTypedURLsDTO(
                    sid,
                    URLs
                );
            }
        }

        internal class InternetExplorerTypedURLsDTO : CommandDTOBase
        {
            public InternetExplorerTypedURLsDTO(string sid, List<TypedUrl> urls)
            {
                Sid = sid;
                Urls = urls;    
            }
            public string Sid { get; }
            public List<TypedUrl> Urls { get; }
        }

        [CommandOutputType(typeof(InternetExplorerTypedURLsDTO))]
        internal class InternetExplorerTypedURLsFormatter : TextFormatterBase
        {
            public InternetExplorerTypedURLsFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (InternetExplorerTypedURLsDTO)result;

                WriteLine("\n  {0}", dto.Sid);

                foreach (var url in dto.Urls)
                {
                    WriteLine($"    {url.Time,-23} :  {url.Url}");
                }
                WriteLine();
            }
        }
    }
}
