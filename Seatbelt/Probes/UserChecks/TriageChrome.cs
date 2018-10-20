using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Seatbelt.Probes.UserChecks
{
    public class TriageChrome : IProbe
    {

        public static string ProbeName => "TriageChrome";


        public string List()
        {
            var sb = new StringBuilder();

            try
            {
                if (Helpers.IsHighIntegrity())
                    RunForHighIntegrity(sb);
                else
                    RunForOtherIntegrity(sb);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Chrome (All Users)");
            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                      dir.EndsWith("All Users")))
                {
                    var userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", dir);
                    ParseChromeHistory(userChromeHistoryPath, userName, sb);

                    var userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", dir);
                    ParseChromeBookmarks(userChromeBookmarkPath, userName, sb);
                }
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Chrome (Current User)");

            var userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", Environment.GetEnvironmentVariable("USERPROFILE"));
            ParseChromeHistory(userChromeHistoryPath, Environment.GetEnvironmentVariable("USERNAME"), sb);

            var userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", Environment.GetEnvironmentVariable("USERPROFILE"));

            ParseChromeBookmarks(userChromeBookmarkPath, Environment.GetEnvironmentVariable("USERNAME"), sb);
        }

        private static void ParseChromeHistory(string path, string user, StringBuilder sb)
        {
            // parses a Chrome history file via regex
            if (!File.Exists(path)) return;

            sb.AppendLine($"    History ({user}):");
            sb.AppendLine();
            var historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

            try
            {
                using (var streamReader = new StreamReader(path))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        var m = historyRegex.Match(line);

                        if (m.Success)
                            sb.AppendLine($"      {m.Groups[0].ToString().Trim()}");

                    }
                }
            }
            catch (IOException ioException)
            {
                sb.AppendLine();
                sb.AppendLine("    [x] IO exception, history file likely in use (i.e. Browser is likely running): " + ioException.Message);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }
        }

        private static void ParseChromeBookmarks(string path, string user, StringBuilder sb)
        {
            // parses a Chrome bookmarks
            if (!File.Exists(path)) return;

            sb.AppendLine();
            sb.AppendLine($"    Bookmarks ({user}):");
            sb.AppendLine();

            try
            {
                var contents = File.ReadAllText(path);

                // reference: http://www.tomasvera.com/programming/using-javascriptserializer-to-parse-json-objects/
                var json = new JavaScriptSerializer();
                var deserialized = json.Deserialize<Dictionary<string, object>>(contents);
                var roots = (Dictionary<string, object>)deserialized["roots"];
                var bookmark_bar = (Dictionary<string, object>)roots["bookmark_bar"];
                var children = (ArrayList)bookmark_bar["children"];

                foreach (Dictionary<string, object> entry in children)
                {
                    sb.AppendLine($"      Name: {entry["name"].ToString().Trim()}");
                    entry.TryGetValue("url", out var url);
                    sb.AppendLine($"      Url:  {url?.ToString().Trim()}");
                    sb.AppendLine();
                }
            }
            catch (IOException exception)
            {
                sb.AppendLine();
                sb.AppendLine("    [x] IO exception, Bookmarks file likely in use (i.e. Chrome is likely running)." + exception.Message);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }
        }
    }
}
