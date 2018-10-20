using System;
using System.Collections.Generic;
using System.Linq;
using Seatbelt.Probes;

namespace Seatbelt
{
    /// <summary>
    /// Uses reflection to get all the classes implementing IProbe
    /// To add a new Probe just create a class the implements IProbe and has a static property "ProbeName"
    /// </summary>
    public class AvailableProbes
    {
        private readonly Dictionary<string, Func<string>> _availableProbes = new Dictionary<string, Func<string>>(55);
        private readonly List<string> _probesDone = new List<string>(55);

        public AvailableProbes()
        {
            // Get all the IProbe Classes (except for the IProbe interface type its self!)
            var iprobeTypes = System.Reflection.Assembly
                .GetEntryAssembly()
                .GetTypes()
                .Where(type => typeof(IProbe).IsAssignableFrom(type) && type.Name != typeof(IProbe).Name);


            foreach (var probe in iprobeTypes)
            {
                // the name comes from the static ProbeName property
                var probeName = probe.GetProperty("ProbeName").GetValue(probe, null).ToString().ToLowerInvariant();

                // the action, when invoked, will create an instance of the class and then call the List() method
                var probeAction = new Func<string>(() => (Activator.CreateInstance(probe) as IProbe).List());

                _availableProbes.Add(probeName, probeAction);
            }
        }


        /// <summary>
        /// Runs a probe and returns the results
        /// NOTE: Will not run a probe more than once!
        /// (this is to prevent multiple calls to a probe when that probe is part of a preset and called with the all flag)
        /// </summary>
        public string RunProbe(string name)
        {
            // Ignore the case
            var nameLowerCase = name.ToLowerInvariant();

            // Don't re-run a probe if it's already been run
            if (string.IsNullOrEmpty(name) || _probesDone.Contains(nameLowerCase))
                return string.Empty;

            if (_availableProbes.TryGetValue(nameLowerCase, out var probe))
            {
                _probesDone.Add(nameLowerCase);
                return probe.Invoke();
            }
            else
                return $"[X] Check \"{name}\" not found!";
        }

        /// <summary>
        /// Return all the available probe names
        /// </summary>
        public IEnumerable<string> GetAll()
            => _availableProbes.Keys;

        /// <summary>
        /// Intended for debugging - return the list of probes done
        /// </summary>
        internal IEnumerable<string> GetProbesDone()
            => _probesDone;

    }
}