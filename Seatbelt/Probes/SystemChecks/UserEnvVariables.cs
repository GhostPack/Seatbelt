using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class UserEnvVariables :IProbe
    {
        public static string ProbeName => "UserEnvVariables";


        public string List()
        {
            var sb = new StringBuilder();

            try
            {
                // dumps out current user environment variables
                sb.AppendProbeHeaderLine("User Environment Variables");

                var srt = new SortedDictionary<string, string>();

                foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    srt.Add((string)env.Key, (string)env.Value);
                }

                foreach (var kvp in srt)
                {
                    sb.AppendLine($"  {kvp.Key,-35} : {kvp.Value}");
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

