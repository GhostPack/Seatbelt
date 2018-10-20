using System;
using System.IO;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class MappedDrives :IProbe
    {
        public static string ProbeName => "MappedDrives";

        public string List()
        {
            var sb = new StringBuilder();

            try
            {
                sb.AppendProbeHeaderLine("Drive Information (via .NET)");
                
                sb.AppendLine($"  {"Drive",-10}   Mapped Location");
                
                // grab all drive letters
                foreach (var driveInfo in DriveInfo.GetDrives())
                {
                    // try to resolve each drive to a UNC mapped location
                    var path = Helpers.GetUNCPath(driveInfo.Name);

                    sb.AppendLine($"  {driveInfo.Name,-10} : {path}");
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
