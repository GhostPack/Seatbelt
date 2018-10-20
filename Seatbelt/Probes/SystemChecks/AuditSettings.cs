using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class AuditSettings : IProbe
    {
        public static string ProbeName => "AuditSettings";

        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("Audit Settings");

            var settings = Helpers.GetRegValues("HKLM", "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit");

            if ((settings != null) && (settings.Count != 0))
            {
                foreach (var kvp in settings)
                {
                    if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.String"))
                    {
                        var result = string.Join(",", (string[])kvp.Value);
                        sb.AppendLine($"  {kvp.Key,-30} : {result}");
                    }
                    else
                    {
                        sb.AppendLine($"  {kvp.Key,-30} : {kvp.Value}");
                    }
                }
            }

            return sb.ToString();
        }

    }
}
