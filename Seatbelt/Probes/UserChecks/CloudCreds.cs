using System;
using System.IO;
using System.Text;

namespace Seatbelt.Probes.UserChecks
{
    public class CloudCreds :IProbe
    {
        public static string ProbeName => "CloudCreds";


        public string List()
        {
            var sb = new StringBuilder();

            // checks for various cloud credential files (AWS, Microsoft Azure, and Google Compute)
            // adapted from https://twitter.com/cmaddalena's SharpCloud project (https://github.com/chrismaddalena/SharpCloud/)

            try
            {

                if (Helpers.IsHighIntegrity())
                    RunForHighIntegrity(sb);
                else
                    RunForOtherIntegrity(sb);

            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

        private static void RunForHighIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Cloud Credentials (All Users)");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];

                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users")) continue;

                var awsKeyFile = String.Format("{0}\\.aws\\credentials", dir);
                AppendDetails(sb, "AWS key file exists", awsKeyFile);

                var computeCredsDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\credentials.db", dir);
                AppendDetails(sb, "Compute creds", computeCredsDb);

                var computeLegacyCreds = String.Format("{0}\\AppData\\Roaming\\gcloud\\legacy_credentials", dir);
                AppendDetails(sb, "Compute legacy creds", computeLegacyCreds);

                var computeAccessTokensDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\access_tokens.db", dir);
                AppendDetails(sb, "Compute access tokens", computeAccessTokensDb);

                var azureTokens = String.Format("{0}\\.azure\\accessTokens.json", dir);
                AppendDetails(sb, "Azure access tokens", azureTokens);

                var azureProfile = String.Format("{0}\\.azure\\azureProfile.json", dir);
                AppendDetails(sb, "Azure profile", azureProfile);
            }
        }

        private static void RunForOtherIntegrity(StringBuilder sb)
        {
            sb.AppendProbeHeaderLine("Checking for Cloud Credentials (Current User)");

            var awsKeyFile = String.Format("{0}\\.aws\\credentials", Environment.GetEnvironmentVariable("USERPROFILE"));
            AppendDetails(sb, "AWS key file exists", awsKeyFile);

            var computeCredsDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\credentials.db",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            AppendDetails(sb, "Compute creds", computeCredsDb);

            var computeLegacyCreds = String.Format("{0}\\AppData\\Roaming\\gcloud\\legacy_credentials",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            AppendDetails(sb, "Compute legacy creds", computeLegacyCreds);

            var computeAccessTokensDb = String.Format("{0}\\AppData\\Roaming\\gcloud\\access_tokens.db",
                Environment.GetEnvironmentVariable("USERPROFILE"));
            AppendDetails(sb, "Compute access tokens", computeAccessTokensDb);

            var azureTokens = String.Format("{0}\\.azure\\accessTokens.json", Environment.GetEnvironmentVariable("USERPROFILE"));
            AppendDetails(sb, "Azure access tokens", azureTokens);

            var azureProfile = String.Format("{0}\\.azure\\azureProfile.json", Environment.GetEnvironmentVariable("USERPROFILE"));
            AppendDetails(sb, "Azure profile", azureProfile);
        }


        private static void AppendDetails(StringBuilder sb, string itemName, string fileName)
        {
            if (!File.Exists(fileName)) return;

            var lastAccessed = File.GetLastAccessTime(fileName);
            var lastModified = File.GetLastWriteTime(fileName);
            var size = new FileInfo(fileName).Length;
            sb.AppendLine($"  [*] {itemName} at           : {fileName}");
            sb.AppendLine($"      Accessed                   : {lastAccessed}");
            sb.AppendLine($"      Modified                   : {lastModified}");
            sb.AppendLine($"      Size                       : {size}");
            sb.AppendLine();
        }


    }
}
