using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class PowerShellSettings :IProbe
    {

        public static string ProbeName => "PowerShellSettings";


        public string List()
        {
            var sb = new StringBuilder();

            sb.AppendProbeHeaderLine("PowerShell Settings");

            GetTranscriptionSettings(sb);

            GetModuleLoggingFunctions(sb);

            GetScriptBlockSettings(sb);

            return sb.ToString();
        }

        private static void GetTranscriptionSettings(StringBuilder sb)
        {
            var powerShellVersion2 = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\PowerShell\\1\\PowerShellEngine", "PowerShellVersion");
            sb.AppendLine($"  {"PowerShell v2 Version",-30} : {powerShellVersion2}");

            var powerShellVersion5 = Helpers.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\PowerShell\\3\\PowerShellEngine", "PowerShellVersion");
            sb.AppendLine($"  {"PowerShell v5 Version",-30} : {powerShellVersion5}");

            var transcriptionSettings = Helpers.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\Transcription");
            sb.AppendLine("\r\n  Transcription Settings:\r\n");

            if ((transcriptionSettings != null) && (transcriptionSettings.Count != 0))
            {
                foreach (var kvp in transcriptionSettings)
                {
                    sb.AppendLine($"  {kvp.Key,30} : {kvp.Value}");
                }
            }
        }

        private static void GetModuleLoggingFunctions(StringBuilder sb)
        {
            var moduleLoggingSettings = Helpers.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging");
            sb.AppendSubHeaderLine("Module Logging Settings");

            if ((moduleLoggingSettings != null) && (moduleLoggingSettings.Count != 0))
            {
                foreach (var kvp in moduleLoggingSettings)
                {
                    sb.AppendLine($"  {kvp.Key,30} : {kvp.Value}");
                }
            }
        }

        private static void GetScriptBlockSettings(StringBuilder sb)
        {
            var scriptBlockSettings = Helpers.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging");
            sb.AppendSubHeaderLine("Scriptblock Logging Settings");

            if ((scriptBlockSettings != null) && (scriptBlockSettings.Count != 0))
            {
                foreach (var kvp in scriptBlockSettings)
                {
                    sb.AppendLine($"  {kvp.Key,30} : {kvp.Value}");
                }
            }
        }
    }
}
