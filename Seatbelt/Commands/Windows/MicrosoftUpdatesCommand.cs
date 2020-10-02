using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands.Windows
{
    internal class MicrosoftUpdateCommand : CommandBase
    {
        public override string Command => "MicrosoftUpdates";
        public override string Description => "All Microsoft updates (via COM)";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false;

        // TODO: remote? https://stackoverflow.com/questions/15786294/retrieve-windows-update-history-using-wuapilib-from-a-remote-machine

        public MicrosoftUpdateCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            WriteHost("Enumerating *all* Microsoft updates\r\n");

            // oh how I hate COM...
            var searcher = Type.GetTypeFromProgID("Microsoft.Update.Searcher");
            var searcherObj = Activator.CreateInstance(searcher);

            // get the total number of updates
            var count = (int)searcherObj.GetType().InvokeMember("GetTotalHistoryCount", BindingFlags.InvokeMethod, null, searcherObj, new object[] { });

            // get the pointer to the update collection
            var results = searcherObj.GetType().InvokeMember("QueryHistory", BindingFlags.InvokeMethod, null, searcherObj, new object[] { 0, count });

            for (int i = 0; i < count; ++i)
            {
                // get the actual update item
                var item = searcherObj.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, results, new object[] { i });

                // get our properties
                //  ref - https://docs.microsoft.com/en-us/windows/win32/api/wuapi/nn-wuapi-iupdatehistoryentry
                var title = searcherObj.GetType().InvokeMember("Title", BindingFlags.GetProperty, null, item, new object[] { }).ToString();
                var date = searcherObj.GetType().InvokeMember("Date", BindingFlags.GetProperty, null, item, new object[] { });
                var description = searcherObj.GetType().InvokeMember("Description", BindingFlags.GetProperty, null, item, new object[] { });
                var clientApplicationID = searcherObj.GetType().InvokeMember("ClientApplicationID", BindingFlags.GetProperty, null, item, new object[] { });

                string hotfixId = "";
                Regex reg = new Regex(@"KB\d+");
                var matches = reg.Matches(title);
                if (matches.Count > 0)
                {
                    hotfixId = matches[0].ToString();
                }

                yield return new MicrosoftUpdateDTO(
                    hotfixId,
                    Convert.ToDateTime(date.ToString()).ToUniversalTime(),
                    title,
                    clientApplicationID.ToString(),
                    description.ToString()
                );

                Marshal.ReleaseComObject(item);
            }

            Marshal.ReleaseComObject(results);
            Marshal.ReleaseComObject(searcherObj);
        }
    }

    internal class MicrosoftUpdateDTO : CommandDTOBase
    {
        public MicrosoftUpdateDTO(string hotFixID, DateTime? installedOnUTC, string title, string dlientApplicationID, string description)
        {
            HotFixID = hotFixID;
            InstalledOnUTC = installedOnUTC;
            Title = title;
            ClientApplicationID = dlientApplicationID;
            Description = description;
        }
        public string HotFixID { get; set; }
        public DateTime? InstalledOnUTC { get; set; }
        public string Title { get; set; }
        public string ClientApplicationID { get; set; }
        public string Description { get; set; }
    }

    [CommandOutputType(typeof(MicrosoftUpdateDTO))]
    internal class MicrosoftUpdateFormatter : TextFormatterBase
    {
        public MicrosoftUpdateFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (MicrosoftUpdateDTO)result;

            WriteLine($"  {dto.HotFixID,-10} {dto.InstalledOnUTC?.ToLocalTime(),-23} {dto.ClientApplicationID,-20} {dto.Title}");
        }
    }
}
