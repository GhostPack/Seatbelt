#nullable disable
using System;
using System.Collections.Generic;
using System.Management;
using System.Text.RegularExpressions;
using static Seatbelt.Interop.Secur32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    enum StateEnum
    {
        Unknown = 0,
        Disabled = 1,
        Queued = 2,
        Ready = 3,
        Running = 4
    };

    enum RunlevelEnum
    {
        TASK_RUNLEVEL_LUA = 0,
        TASK_RUNLEVEL_HIGHEST = 1
    }

    class ScheduledTaskPrincipal
    {
        public string DisplayName { get; set; }
        public string GroupId { get; set; }
        public string Id { get; set; }
        public string LogonType { get; set; }
        //public string ProcessTokenSidType { get; set; }
        //public string RequiredPrivilege { get; set; }
        public string RunLevel { get; set; }
        public string UserId { get; set; }
    }

    class ScheduledTaskTrigger
    {
        public object Type { get; set; }
        public object Enabled { get; set; }
        public object EndBoundary { get; set; }
        public object ExecutionTimeLimit { get; set; }
        public object StartBoundary { get; set; }
        public object Duration { get; set; }
        public object Interval { get; set; }
        public object StopAtDurationEnd { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }

    class ScheduledTaskAction
    {
        public object Type { get; set; }

        public object Id { get; set; }

        public Dictionary<string, object> Properties { get; set; }
    }

    internal class ScheduledTasksCommand : CommandBase
    {
        public override string Command => "ScheduledTasks";
        //public override string Description => "ScheduledTasks (via WMI)";
        public override string Description => "Scheduled tasks (via WMI) that aren't authored by 'Microsoft', \"-full\" dumps all Scheduled tasks";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public ScheduledTasksCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            WriteHost(Runtime.FilterResults ? "Non Microsoft scheduled tasks (via WMI)\n" : "All scheduled tasks (via WMI)\n");

            ManagementObjectCollection data = null;

            try
            {
                var wmiData = ThisRunTime.GetManagementObjectSearcher(@"Root\Microsoft\Windows\TaskScheduler", "SELECT * FROM MSFT_ScheduledTask");
                data = wmiData.Get();
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
            {
                WriteError(string.Format("  [X] 'MSFT_ScheduledTask' WMI class unavailable (minimum supported versions of Windows: 8/2012)", ex.Message));
            }
            catch (Exception ex)
            {
                WriteError(ex.ToString());
            }


            if (data == null)
            {
                yield break;
            }

            foreach (var o in data)
            {
                var result = (ManagementObject)o;

                if (Runtime.FilterResults)
                {
                    if (Regex.IsMatch($"{result["Author"]}", "Microsoft"))
                    {
                        continue;
                    }
                }

                var tempPrincipal = (ManagementBaseObject)result["Principal"];
                var settings = (ManagementBaseObject)result["Settings"];
                var actions = (ManagementBaseObject[])result["Actions"];
                var triggers = (ManagementBaseObject[])result["Triggers"];

                var Principal = new ScheduledTaskPrincipal();
                Principal.DisplayName = $"{tempPrincipal["DisplayName"]}";
                Principal.Id = $"{tempPrincipal["Id"]}";
                Principal.GroupId = $"{tempPrincipal["GroupId"]}";
                var tempLogonType = $"{tempPrincipal["LogonType"]}";
                Principal.LogonType = $"{(SECURITY_LOGON_TYPE)Int32.Parse(tempLogonType)}";
                //Principal.ProcessTokenSidType = $"{tempPrincipal["ProcessTokenSidType"]}";
                //Principal.RequiredPrivilege = $"{tempPrincipal["RequiredPrivilege"]}";
                var tempRunLevel = $"{tempPrincipal["RunLevel"]}";
                Principal.RunLevel = $"{(RunlevelEnum)Int32.Parse(tempRunLevel)}";
                Principal.UserId = $"{tempPrincipal["UserId"]}";

                var Actions = new List<ScheduledTaskAction>();
                foreach (var obj in actions)
                {
                    var action = new ScheduledTaskAction();
                    action.Type = $"{obj.SystemProperties["__SUPERCLASS"].Value}";

                    var Properties = new Dictionary<string, object>();

                    foreach (var prop in obj.Properties)
                    {
                        if (!prop.Name.Equals("PSComputerName"))
                        {
                            Properties[prop.Name] = prop.Value;
                        }
                    }
                    action.Properties = Properties;

                    Actions.Add(action);
                }

                var TriggerObjects = new List<ScheduledTaskTrigger>();
                if (triggers != null)
                {
                    foreach (var obj in triggers)
                    {
                        var trigger = new ScheduledTaskTrigger();
                        trigger.Type = $"{obj.SystemProperties["__CLASS"].Value}";

                        // MSFT_TaskTrigger base properties
                        trigger.Enabled = obj["Enabled"];
                        trigger.EndBoundary = obj["EndBoundary"];
                        trigger.ExecutionTimeLimit = obj["ExecutionTimeLimit"];
                        trigger.StartBoundary = obj["StartBoundary"];

                        var repetitionobj = (ManagementBaseObject)obj["repetition"];
                        trigger.Duration = repetitionobj["Duration"];
                        trigger.Interval = repetitionobj["Interval"];
                        trigger.StopAtDurationEnd = repetitionobj["StopAtDurationEnd"];

                        // additional properties for subclasses
                        var properties = new Dictionary<string, string>();
                        foreach (var prop in obj.Properties)
                        {
                            //if(prop.Name
                            if (!Regex.IsMatch($"{prop.Name}", "Id|Enabled|EndBoundary|ExecutionTimeLimit|StartBoundary|Repetition"))
                            {
                                properties.Add(prop.Name, $"{prop.Value}");
                            }
                        }
                        trigger.Properties = properties;

                        TriggerObjects.Add(trigger);
                    }
                }

                yield return new ScheduledTasksDTO()
                {
                    Name = result["TaskName"],
                    Principal = Principal,
                    Author = result["Author"],
                    Description = result["Description"],
                    Source = result["Source"],
                    State = (StateEnum)result["State"],
                    SDDL = result["SecurityDescriptor"],
                    Actions = Actions,
                    Triggers = TriggerObjects,
                    Enabled = settings["Enabled"],
                    TaskPath = result["TaskPath"],
                    Hidden = settings["Hidden"],
                    Date = result["Date"],
                    AllowDemandStart = settings["AllowDemandStart"],
                    AllowHardTerminate = settings["AllowHardTerminate"],
                    DisallowStartIfOnBatteries = settings["DisallowStartIfOnBatteries"],
                    ExecutionTimeLimit = settings["ExecutionTimeLimit"],
                    StopIfGoingOnBatteries = settings["StopIfGoingOnBatteries"]
                };
            }

            data.Dispose();
        }
    }

    internal class ScheduledTasksDTO : CommandDTOBase
    {
        public object Name { get; set; }

        public ScheduledTaskPrincipal Principal { get; set; }

        public object Author { get; set; }

        public object Description { get; set; }

        public object Source { get; set; }

        public object State { get; set; }

        public object SDDL { get; set; }

        public List<ScheduledTaskAction> Actions { get; set; }

        public List<ScheduledTaskTrigger> Triggers { get; set; }

        public object Enabled { get; set; }

        public object TaskPath { get; set; }

        public object Hidden { get; set; }

        public object Date { get; set; }

        public object AllowDemandStart { get; set; }

        public object AllowHardTerminate { get; set; }

        public object DisallowStartIfOnBatteries { get; set; }

        public object ExecutionTimeLimit { get; set; }

        public object StopIfGoingOnBatteries { get; set; }
    }

    [CommandOutputType(typeof(ScheduledTasksDTO))]
    internal class ScheduledTasksFormatter : TextFormatterBase
    {
        public ScheduledTasksFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (ScheduledTasksDTO)result;

            WriteLine("  {0,-30}    :   {1}", "Name", dto.Name);

            WriteLine("  {0,-30}    :", "Principal");
            //WriteLine("      {0,-30}:   {1}", "DisplayName", dto.Principal.DisplayName);
            WriteLine("      {0,-30}:   {1}", "GroupId", dto.Principal.GroupId);
            WriteLine("      {0,-30}:   {1}", "Id", dto.Principal.Id);
            WriteLine("      {0,-30}:   {1}", "LogonType", dto.Principal.LogonType);
            WriteLine("      {0,-30}:   {1}", "RunLevel", dto.Principal.RunLevel);
            WriteLine("      {0,-30}:   {1}", "UserId", dto.Principal.UserId);

            WriteLine("  {0,-30}    :   {1}", "Author", dto.Author);
            WriteLine("  {0,-30}    :   {1}", "Description", dto.Description);
            WriteLine("  {0,-30}    :   {1}", "Source", dto.Source);
            WriteLine("  {0,-30}    :   {1}", "State", dto.State);
            WriteLine("  {0,-30}    :   {1}", "SDDL", dto.SDDL);
            //WriteLine("  {0,-30}    :   {1}", "Actions", dto.State);
            WriteLine("  {0,-30}    :   {1}", "Enabled", dto.Enabled);
            WriteLine("  {0,-30}    :   {1}", "Date", (DateTime)Convert.ToDateTime(dto.Date));
            WriteLine("  {0,-30}    :   {1}", "AllowDemandStart", dto.AllowDemandStart);
            //WriteLine("  {0,-30}    :   {1}", "AllowHardTerminate", dto.AllowHardTerminate);
            WriteLine("  {0,-30}    :   {1}", "DisallowStartIfOnBatteries", dto.DisallowStartIfOnBatteries);
            WriteLine("  {0,-30}    :   {1}", "ExecutionTimeLimit", dto.ExecutionTimeLimit);
            WriteLine("  {0,-30}    :   {1}", "StopIfGoingOnBatteries", dto.StopIfGoingOnBatteries);

            WriteLine("  {0,-30}    :", "Actions");
            WriteLine("      ------------------------------");
            foreach (var action in dto.Actions)
            {
                //WriteLine("      {0,-30}:   {1}", "Id", action.Id);
                WriteLine("      {0,-30}:   {1}", "Type", action.Type);
                foreach (var kvp in (Dictionary<string, object>)action.Properties)
                {
                    if (!String.IsNullOrEmpty($"{kvp.Value}"))
                    {
                        WriteLine("      {0,-30}:   {1}", kvp.Key, kvp.Value);
                    }
                }
                WriteLine("      ------------------------------");
            }

            WriteLine("  {0,-30}    :", "Triggers");
            WriteLine("      ------------------------------");
            foreach (var trigger in dto.Triggers)
            {
                WriteLine("      {0,-30}:   {1}", "Type", trigger.Type);
                WriteLine("      {0,-30}:   {1}", "Enabled", trigger.Enabled);
                if (!String.IsNullOrEmpty($"{trigger.StartBoundary}"))
                {
                    WriteLine("      {0,-30}:   {1}", "StartBoundary", trigger.StartBoundary);
                }
                if (!String.IsNullOrEmpty($"{trigger.EndBoundary}"))
                {
                    WriteLine("      {0,-30}:   {1}", "EndBoundary", trigger.EndBoundary);
                }
                if (!String.IsNullOrEmpty($"{trigger.ExecutionTimeLimit}"))
                {
                    WriteLine("      {0,-30}:   {1}", "ExecutionTimeLimit", trigger.ExecutionTimeLimit);
                }
                if (!String.IsNullOrEmpty($"{trigger.Duration}"))
                {
                    WriteLine("      {0,-30}:   {1}", "Duration", trigger.Duration);
                }
                if (!String.IsNullOrEmpty($"{trigger.Interval}"))
                {
                    WriteLine("      {0,-30}:   {1}", "Interval", trigger.Interval);
                }
                if (!String.IsNullOrEmpty($"{trigger.StopAtDurationEnd}"))
                {
                    WriteLine("      {0,-30}:   {1}", "StopAtDurationEnd", trigger.StopAtDurationEnd);
                }

                if (trigger.Properties != null)
                {
                    foreach (var kvp in trigger.Properties)
                    {
                        if (!String.IsNullOrEmpty(kvp.Value))
                        {
                            WriteLine("      {0,-30}:   {1}", kvp.Key, kvp.Value);
                        }
                    }
                }

                WriteLine("      ------------------------------");
            }

            WriteLine();
        }
    }
}
#nullable enable