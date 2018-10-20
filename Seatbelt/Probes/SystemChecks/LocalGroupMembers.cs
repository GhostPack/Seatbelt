using System;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class LocalGroupMembers : IProbe
    {
        public static string ProbeName => "LocalGroupMembers";
        
        public string List()
        {
            var sb = new StringBuilder();

            // adapted from https://stackoverflow.com/questions/33935825/pinvoke-netlocalgroupgetmembers-runs-into-fatalexecutionengineerror/33939889#33939889

            try
            {

                sb.AppendProbeHeaderLine("Local Group Memberships");

                // localization for @cnotin ;)
                string[] groupsSIDs = {
                    "S-1-5-32-544", // Administrators
                    "S-1-5-32-555", // RDP
                    "S-1-5-32-562", // COM
                    "S-1-5-32-580" // Remote Management
                };

                foreach (var sid in groupsSIDs)
                {
                    var groupNameFull = Helpers.TranslateSid(sid);
                    if (string.IsNullOrEmpty(groupNameFull))
                    {
                        // e.g. "S-1-5-32-580" for "Remote Management Users" can be missing on older versions of Windows
                        sb.AppendLine($"  [X] Cannot find SID translation for '{sid}'");
                        continue;
                    }

                    var groupName = groupNameFull.Substring(groupNameFull.IndexOf('\\') + 1);

                    sb.AppendLine($"  * {groupName} *").AppendLine();

                    var members = Helpers.GetLocalGroupMembers(groupName, sb);
                    if (members != null)
                    {
                        foreach (var member in members)
                        {
                            sb.AppendLine($"    {member}");
                        }
                    }

                    sb.AppendLine();
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
