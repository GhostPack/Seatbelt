using System;

namespace Seatbelt.Interop
{
    internal class COM
    {
        [Flags]
        public enum FirewallProfiles : int
        {
            DOMAIN = 1,
            PRIVATE = 2,
            PUBLIC = 4,
            ALL = 2147483647
        }
    }
}
