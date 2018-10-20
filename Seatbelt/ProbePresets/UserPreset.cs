using System.Collections.Generic;
using System.Security.Principal;
using Seatbelt.IO;
using Seatbelt.Probes.UserChecks;

namespace Seatbelt.ProbePresets
{
    public static class UserPreset
    {
        public static void Run(IOutputSink output, AvailableProbes probes)
        {
            output.WriteLine("\r\n=== Running User Triage Checks ===\r\n");
            output.WriteLine("");

            if (Helpers.IsHighIntegrity())
                output.WriteLine(" [*] In high integrity, attempting triage for all users on the machine.");
            else
                output.WriteLine(" [*] In medium integrity, attempting triage of current user.");

            output.WriteLine("");
            output.WriteLine($"     Current user : {WindowsIdentity.GetCurrent().Name} - {WindowsIdentity.GetCurrent().User} ");


            output.Write(probes.RunProbe(CheckFirefox.ProbeName));
            output.Write(probes.RunProbe(CheckChrome.ProbeName));
            output.Write(probes.RunProbe(TriageIE.ProbeName));
            output.Write(probes.RunProbe(DumpVault.ProbeName));
            output.Write(probes.RunProbe(SavedRDPConnections.ProbeName));
            output.Write(probes.RunProbe(RecentRunCommands.ProbeName));
            output.Write(probes.RunProbe(PuttySessions.ProbeName));
            output.Write(probes.RunProbe(PuttySSHHostKeys.ProbeName));
            output.Write(probes.RunProbe(CloudCreds.ProbeName));
            output.Write(probes.RunProbe(RecentFiles.ProbeName));
            output.Write(probes.RunProbe(MasterKeys.ProbeName));
            output.Write(probes.RunProbe(CredFiles.ProbeName));
            output.Write(probes.RunProbe(RDCManFiles.ProbeName));


            if (!FilterResults.Filter)
            {
                output.Write(probes.RunProbe(TriageChrome.ProbeName));
                output.Write(probes.RunProbe(TriageFirefox.ProbeName));
                output.Write(probes.RunProbe(InterestingFiles.ProbeName));
            }
        }
    }
}