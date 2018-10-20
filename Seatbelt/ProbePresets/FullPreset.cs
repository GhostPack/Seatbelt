using Seatbelt.IO;
using Seatbelt.Probes.SystemChecks;
using Seatbelt.Probes.UserChecks;

namespace Seatbelt.ProbePresets
{
    public static class FullPreset
    {
        public static void Run(IOutputSink output, AvailableProbes probes)
        {
            SystemPreset.Run(output, probes);
            output.Write(probes.RunProbe(KerberosTickets.ProbeName));
            UserPreset.Run(output, probes);
            output.Write(probes.RunProbe(IETabs.ProbeName));
            output.Write(probes.RunProbe(Patches.ProbeName));
            output.Write(probes.RunProbe(RecycleBin.ProbeName));

        }
    }
}
