using Seatbelt.IO;
using Seatbelt.Probes.SystemChecks;
//using Seatbelt.Probes.UserChecks;

namespace Seatbelt.ProbePresets
{
    public static class AllPreset
    {
        public static void Run(IOutputSink output, AvailableProbes probes)
        {
        
            SystemPreset.Run(output, probes);
            output.Write(probes.RunProbe(KerberosTickets.ProbeName));
            UserPreset.Run(output, probes);

            // NOTE: This used to run the probes below only:

            //output.Write(probes.RunProbe(IETabs.ProbeName));
            //output.Write(probes.RunProbe(Patches.ProbeName));
            //output.Write(probes.RunProbe(TriageChrome.ProbeName));
            //output.Write(probes.RunProbe(TriageFirefox.ProbeName));
            //output.Write(probes.RunProbe(RecycleBin.ProbeName));
            //output.Write(probes.RunProbe(InterestingFiles.ProbeName));

            // Run all available probes
            foreach (var probeName in probes.GetAll())
                output.Write(probes.RunProbe(probeName));

        }
    }
}