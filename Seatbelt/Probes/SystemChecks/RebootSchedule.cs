using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text;

namespace Seatbelt.Probes.SystemChecks
{
    public  class RebootSchedule : IProbe
    {

        public static string ProbeName => "RebootSchedule";

        public string List()
        {
            var sb = new StringBuilder();


            // queries event IDs 12 (kernel boot) and 13 (kernel shutdown), sorts, and gives reboot schedule
            // grab events from the last X days - 15 for default
            var lastDays = 15;

            sb.AppendProbeHeaderLine($"Reboot Schedule (event ID 12/13 from last {lastDays} days)");

            var events = new SortedDictionary<DateTime, string>();

            var startTime = DateTime.Now.AddDays(-lastDays).ToUniversalTime();
            var endTime = DateTime.Now.ToUniversalTime();

            // eventID 12 == start up
            var startUpQuery = $@"*[System/EventID=12] and *[System[TimeCreated[@SystemTime >= '{startTime:o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime:o}']]]";

            var startUpEventsQuery = new EventLogQuery("System", PathType.LogName, startUpQuery);

            try
            {
                var logReader = new EventLogReader(startUpEventsQuery);

                for (var startUpEvent = logReader.ReadEvent(); startUpEvent != null; startUpEvent = logReader.ReadEvent())
                {
                    if (startUpEvent.Properties.Count >= 7 && startUpEvent.Properties[6].Value is DateTime)
                    {
                        var time = (DateTime) startUpEvent.Properties[6].Value;
                        events.Add(time, "startup");
                    }
                    
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            // eventID 13 == shutdown
            var shutDownQuery = $@"*[System/EventID=13] and *[System[TimeCreated[@SystemTime >= '{startTime:o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime:o}']]]";

            var shutDownEventsQuery = new EventLogQuery("System", PathType.LogName, shutDownQuery);

            try
            {
                var logReader2 = new EventLogReader(shutDownEventsQuery);

                for (var shutDownEvent = logReader2.ReadEvent(); shutDownEvent != null; shutDownEvent = logReader2.ReadEvent())
                {
                    if (shutDownEvent.Properties[0].Value is DateTime)
                    {
                        var time = (DateTime)shutDownEvent.Properties[0].Value;
                        events.Add(time, "shutdown");
                    }

                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            foreach (var kvp in events)
            {
                sb.AppendLine($"  {kvp.Key,-23} :  {kvp.Value}");
                if (kvp.Value == "shutdown")
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }


        
    }
}
