using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class WEFSettings :IProbe
    {

        public static string ProbeName => "WEFSettings";


        public string List()
        {
            var sb = new StringBuilder();
            sb.AppendProbeHeaderLine("WEF Settings");

            var settings = Helpers.GetRegValues("HKLM", "Software\\Policies\\Microsoft\\Windows\\EventLog\\EventForwarding\\SubscriptionManager");
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
