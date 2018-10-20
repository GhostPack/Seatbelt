using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;


namespace Seatbelt.Probes.UserChecks
{
    public class CurrentDomainGroups :IProbe
    {
        public static string ProbeName => "CurrentDomainGroups";


        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("Current User's Groups");

            try
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();
                var groups = new List<string>();

                foreach (var group in windowsIdentity.Groups)
                {
                    try
                    {
                        groups.Add(group.Translate(typeof(NTAccount)).ToString());
                    }
                    catch { }
                }
                groups.Sort();
                foreach (var group in groups)
                {
                    sb.AppendLine($"  {group}");
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
