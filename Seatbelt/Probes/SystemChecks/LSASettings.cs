using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Seatbelt.Probes.SystemChecks
{
    public class LSASettings : IProbe
    {
        public static string ProbeName => "LSASettings";

        public string List()
        {
            var regex = new Regex(@".*wdigest.*", RegexOptions.Compiled);

            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("LSA Settings");

            var settings = Helpers.GetRegValues("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Lsa");
            if ((settings != null) && (settings.Any()))
            {
                foreach (var kvp in settings)
                {
                    if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.String"))
                    {
                        var result = string.Join(",", (string[])kvp.Value);

                        sb.AppendLine($"  {kvp.Key,-30} : {result}");

                        if (kvp.Key == "Security Packages" && regex.Match(result).Success)
                        {
                            sb.AppendLine("    [*] Wdigest is enabled- plaintext password extraction is possible!");
                        }
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
