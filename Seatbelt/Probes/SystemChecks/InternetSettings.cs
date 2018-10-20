using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class InternetSettings :IProbe
    {
        public static string ProbeName => "InternetSettings";

        public string List()
        {
            // lists user/system internet settings, including default proxy info

            var sb = new StringBuilder();
            
            var proxySettings = Helpers.GetRegValues("HKCU", "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");

            sb.AppendProbeHeaderLine("HKCU Internet Settings");

            if ((proxySettings != null) && (proxySettings.Count != 0))
            {
                foreach (var kvp in proxySettings)
                {
                    sb.AppendLine($"  {kvp.Key,30} : {kvp.Value}");
                }
            }

            var proxySettings2 = Helpers.GetRegValues("HKLM", "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");

            sb.AppendProbeHeaderLine("HKLM Internet Settings");

            if ((proxySettings2 != null) && (proxySettings2.Count != 0))
            {
                foreach (var kvp in proxySettings2)
                {
                    sb.AppendLine($"  {kvp.Key,30} : {kvp.Value}");
                }
            }

            return sb.ToString();
        }

    }
}
