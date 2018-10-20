using Seatbelt.IO;
using Seatbelt.Probes.SystemChecks;

namespace Seatbelt.ProbePresets
{
    public static class SystemPreset
    {
        public static void Run(IOutputSink output, AvailableProbes probes)
        {
            output.WriteLine("\r\n=== Running System Triage Checks ===\r\n");

            output.Write(probes.RunProbe(BasicOSInfo.ProbeName));
            output.Write(probes.RunProbe(RebootSchedule.ProbeName));
            output.Write(probes.RunProbe(TokenGroupPrivs.ProbeName));
            output.Write(probes.RunProbe(UACSystemPolicies.ProbeName));
            output.Write(probes.RunProbe(PowerShellSettings.ProbeName));
            output.Write(probes.RunProbe(AuditSettings.ProbeName));
            output.Write(probes.RunProbe(WEFSettings.ProbeName));
            output.Write(probes.RunProbe(LSASettings.ProbeName));
            output.Write(probes.RunProbe(UserEnvVariables.ProbeName));
            output.Write(probes.RunProbe(SystemEnvVariables.ProbeName));
            output.Write(probes.RunProbe(UserFolders.ProbeName));
            output.Write(probes.RunProbe(NonstandardServices.ProbeName));
            output.Write(probes.RunProbe(InternetSettings.ProbeName));
            output.Write(probes.RunProbe(LapsSettings.ProbeName));
            output.Write(probes.RunProbe(AppLockerSettings.ProbeName));
            output.Write(probes.RunProbe(LocalGroupMembers.ProbeName));
            output.Write(probes.RunProbe(MappedDrives.ProbeName));
            output.Write(probes.RunProbe(RDPSessions.ProbeName));
            output.Write(probes.RunProbe(WMIMappedDrives.ProbeName));
            output.Write(probes.RunProbe(NetworkShares.ProbeName));
            output.Write(probes.RunProbe(FirewallRules.ProbeName));
            output.Write(probes.RunProbe(AntiVirusWMI.ProbeName));
            output.Write(probes.RunProbe(InterestingProcesses.ProbeName));
            output.Write(probes.RunProbe(RegistryAutoLogon.ProbeName));
            output.Write(probes.RunProbe(RegistryAutoRuns.ProbeName));
            output.Write(probes.RunProbe(DNSCache.ProbeName));
            output.Write(probes.RunProbe(ARPTable.ProbeName));
            output.Write(probes.RunProbe(AllTcpConnections.ProbeName));
            output.Write(probes.RunProbe(AllUdpConnections.ProbeName));
            output.Write(probes.RunProbe(NonstandardProcesses.ProbeName));

            // list patches and List4624Events/List4648Events if we're doing "full" collection
            if (!FilterResults.Filter)
            {
                output.Write(probes.RunProbe(Patches.ProbeName));
                output.Write(probes.RunProbe(Events4624.ProbeName));
                output.Write(probes.RunProbe(Events4648.ProbeName));
            }

            if (Helpers.IsHighIntegrity())
            {
                output.WriteLine("\r\n\r\n [*] In high integrity, performing elevated collection options.");
                output.Write(probes.RunProbe(SysmonConfig.ProbeName));
            }
        }
    }
}