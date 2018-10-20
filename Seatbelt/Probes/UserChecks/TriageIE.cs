using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;


namespace Seatbelt.Probes.UserChecks
{
    public class TriageIE: IProbe
    {

        public static string ProbeName => "TriageIE";


        public string List()
    {
        var sb = new StringBuilder();

        // lists Internet explorer history (last 7 days by default) and favorites
        var lastDays = 7;

        if (!FilterResults.Filter)
            lastDays = 90;

        var startTime = DateTime.Now.AddDays(-lastDays);

        try
        {
            if (Helpers.IsHighIntegrity())
                RunForHighIntegrity(sb, startTime, lastDays);
            else
                RunForOtherIntegrity(sb, startTime, lastDays);
        }
        catch (Exception ex)
        {
            sb.AppendExceptionLine(ex);
        }

        return sb.ToString();
    }

    private static void RunForHighIntegrity(StringBuilder sb, DateTime startTime, int lastDays)
    {
        sb.AppendProbeHeaderLine($"Internet Explorer (All Users) Last {lastDays} Days");

        var SIDs = Registry.Users.GetSubKeyNames();
        foreach (var SID in SIDs)
        {
            if (!SID.StartsWith("S-1-5") || SID.EndsWith("_Classes")) continue;

            var settings = Helpers.GetRegValues("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs", SID));

            if ((settings == null) || (settings.Count <= 1)) continue;

            sb.AppendLine();
            sb.AppendLine($"  History ({SID}):");

            foreach (var kvp in settings)
            {
                var timeBytes = Helpers.GetRegValueBytes("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime", SID), kvp.Key.Trim());
                if (timeBytes == null) continue;

                var timeLong = (long) (BitConverter.ToInt64(timeBytes, 0));
                var urlTime = DateTime.FromFileTime(timeLong);
                if (urlTime > startTime)
                    sb.AppendLine($"    {urlTime,-23} :  {kvp.Value.ToString().Trim()}");
            }
        }

        var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
        var dirs = Directory.GetDirectories(userFolder);

        foreach (var dir in dirs)
        {
            var parts = dir.Split('\\');
            var userName = parts[parts.Length - 1];

            if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")) continue;

            var userIEBookmarkPath = String.Format("{0}\\Favorites\\", dir);

            if (!Directory.Exists(userIEBookmarkPath)) continue;

            var bookmarkPaths = Directory.GetFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories);

            if (bookmarkPaths.Length == 0) continue;

            sb.AppendLine();
            sb.AppendLine($"  Favorites ({userName}):");

            foreach (var bookmarkPath in bookmarkPaths)
            {
                using (var rdr = new StreamReader(bookmarkPath))
                {
                    string line;
                    var url = "";
                    while ((line = rdr.ReadLine()) != null)
                    {
                        if (line.StartsWith("URL=", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (line.Length > 4)
                                url = line.Substring(4);
                            break;
                        }
                    }

                    sb.AppendLine($"    {url.Trim()}");
                }
            }
        }
    }

    private static void RunForOtherIntegrity(StringBuilder sb, DateTime startTime, int lastDays)
    {
        sb.AppendProbeHeaderLine($"Internet Explorer (Current User) Last {lastDays} Days");

        sb.AppendLine("  History:");
        var settings = Helpers.GetRegValues("HKCU", "SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs");

        if ((settings != null) && (settings.Any()))
        {
            foreach (var kvp in settings)
            {
                var timeBytes = Helpers.GetRegValueBytes("HKCU", "SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime",
                    kvp.Key.Trim());
                if (timeBytes == null) continue;

                var timeLong = (long) (BitConverter.ToInt64(timeBytes, 0));
                var urlTime = DateTime.FromFileTime(timeLong);
                if (urlTime > startTime)
                {
                    sb.AppendLine($"    {urlTime,-23} :  {kvp.Value.ToString().Trim()}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("  Favorites:");

        var userIEBookmarkPath = String.Format("{0}\\Favorites\\", Environment.GetEnvironmentVariable("USERPROFILE"));

        var bookmarkPaths = Directory.GetFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories);

        foreach (var bookmarkPath in bookmarkPaths)
        {
            using (var rdr = new StreamReader(bookmarkPath))
            {
                string line;
                var url = "";
                while ((line = rdr.ReadLine()) != null)
                {
                    if (line.StartsWith("URL=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (line.Length > 4)
                            url = line.Substring(4);
                        break;
                    }
                }

                sb.AppendLine($"    {url.Trim()}");
            }
        }
    }
    }
}
