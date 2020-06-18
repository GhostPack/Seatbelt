#nullable disable
using System;

namespace Seatbelt.Commands.Windows.EventLogs.ExplicitLogonEvents
{
    internal class ExplicitLogonEventsDTO : CommandDTOBase
    {
        public string SubjectUser { get; set; }
        public string SubjectDomain { get; set; }
        public string TargetUser { get; set; }
        public string TargetDomain { get; set; }
        public string Process { get; set; }
        public string IpAddress { get; set; }
        public DateTime? TimeCreatedUtc { get; set; }
    }
}
#nullable enable
