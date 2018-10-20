using System;
using System.IO;
using System.Text;

namespace Seatbelt.Probes.UserChecks
{
    public class InterestingFiles :IProbe
    {

        public static string ProbeName => "InterestingFiles";


        public string List()
        {
            var sb = new StringBuilder(16384);

            // returns files (w/ modification dates) that match the given pattern below
            var patterns = "*pass *;*diagram*;*.pdf;*.vsd;*.doc;*docx;*.xls;*.xlsx;*.kdbx;*.key;KeePass.config";
            string searchPath;

            if (Helpers.IsHighIntegrity())
            {
                sb.AppendProbeHeaderLine("Interesting Files (All Users)");
                searchPath = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
            }
            else
            {
                sb.AppendProbeHeaderLine("Interesting Files (Current User)");
                searchPath = Environment.GetEnvironmentVariable("USERPROFILE");
            }

            var filePaths = Helpers.FindFiles(searchPath, patterns);

            foreach (var filePath in filePaths)
                sb.Append(AppendFileInfo(filePath));

            return sb.ToString();
        }

        private static string AppendFileInfo(string filePath)
        {
            var lastAccessed = File.GetLastAccessTime(filePath);
            var lastModified = File.GetLastWriteTime(filePath);

            string toReturn = $"    File:         {filePath}\r\n";
            toReturn += $"        Accessed: {lastAccessed}\r\n";
            toReturn += $"        Modified: {lastModified}\r\n";

            return toReturn;
        }



    }
}
