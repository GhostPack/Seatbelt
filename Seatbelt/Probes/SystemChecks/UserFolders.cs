using System;
using System.IO;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class UserFolders : IProbe
    {
        public static string ProbeName => "UserFolders";


        public string List()
        {
            var sb = new StringBuilder(4096);


            // lists the folders in C:\Users\, showing users who have logged onto the system
            try
            {
                sb.AppendProbeHeaderLine("User Folders");

                var userPath = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";

                sb.AppendLine($"  {"Folder",-35}   Last Modified Time");

                foreach (var dir in Directory.GetDirectories(userPath))
                {
                    if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                    {
                        sb.AppendLine($"  {dir,-35} : {Directory.GetLastWriteTime(dir)}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();

        }
    }
}
