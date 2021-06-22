using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;


namespace Seatbelt.Commands
{
    class Download
    {
        public string? TeamID { get; set; }
        public string? UserID { get; set; }
        public string? DownloadPath { get; set; }
        public DateTime? StartTime { get; set; }
    }

    internal class SlackDownloadsCommand : CommandBase
    {
        public override string Command => "SlackDownloads";
        public override string Description => "Parses any found 'slack-downloads' files";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.Slack };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public SlackDownloadsCommand(Runtime runtime) : base(runtime)
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

                var userSlackDownloadsPath = $"{dir}\\AppData\\Roaming\\Slack\\storage\\slack-downloads";

                // parses a Slack downloads file
                if (File.Exists(userSlackDownloadsPath))
                {
                    var Downloads = new List<Download>();

                    try
                    {
                        var contents = File.ReadAllText(userSlackDownloadsPath);

                        // reference: http://www.tomasvera.com/programming/using-javascriptserializer-to-parse-json-objects/
                        var json = new JavaScriptSerializer();
                        var deserialized = json.Deserialize<Dictionary<string, object>>(contents);

                        foreach (var w in deserialized)
                        {
                            var dls = (Dictionary<string, object>)w.Value;
                            foreach (var x in dls)
                            {
                                var dl = (Dictionary<string, object>)x.Value;
                                var download = new Download();
                                if (dl.ContainsKey("teamId"))
                                {
                                    download.TeamID = $"{dl["teamId"]}";
                                }
                                if (dl.ContainsKey("userId"))
                                {
                                    download.UserID = $"{dl["userId"]}";
                                }
                                if (dl.ContainsKey("downloadPath"))
                                {
                                    download.DownloadPath = $"{dl["downloadPath"]}";
                                }
                                if (dl.ContainsKey("startTime"))
                                {
                                    try
                                    {
                                        download.StartTime = MiscUtil.UnixEpochToDateTime(long.Parse($"{dl["startTime"]}"));
                                    }
                                    catch
                                    {
                                    }
                                }
                                Downloads.Add(download);
                            }
                        }
                    }
                    catch (IOException exception)
                    {
                        WriteError(exception.ToString());
                    }
                    catch (Exception exception)
                    {
                        WriteError(exception.ToString());
                    }

                    yield return new SlackDownloadsDTO(
                        userName,
                        Downloads
                    );
                }
            }
        }

        internal class SlackDownloadsDTO : CommandDTOBase
        {
            public SlackDownloadsDTO(string userName, List<Download> downloads)
            {
                UserName = userName;
                Downloads = downloads;  
            }
            public string UserName { get; }
            public List<Download> Downloads { get; }
        }

        [CommandOutputType(typeof(SlackDownloadsDTO))]
        internal class SlackDownloadsFormatter : TextFormatterBase
        {
            public SlackDownloadsFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (SlackDownloadsDTO)result;

                WriteLine($"  Downloads ({dto.UserName}):\n");

                foreach (var download in dto.Downloads)
                {
                    WriteLine($"    TeamID       : {download.TeamID}");
                    WriteLine($"    UserId       : {download.UserID}");
                    WriteLine($"    DownloadPath : {download.DownloadPath}");
                    WriteLine($"    StartTime    : {download.StartTime}\n");
                }
                WriteLine();
            }
        }
    }
}