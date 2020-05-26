using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace Seatbelt.Commands.Browser
{
    internal class InternetExplorerTabCommand : CommandBase
    {
        public override string Command => "IETabs";
        public override string Description => "Open Internet Explorer tabs";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public InternetExplorerTabCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // Lists currently open Internet Explorer tabs, via COM
            // Notes:
            //  https://searchcode.com/codesearch/view/9859954/
            //  https://gist.github.com/yizhang82/a1268d3ea7295a8a1496e01d60ada816

            // Shell.Application COM GUID
            var shell = Type.GetTypeFromProgID("Shell.Application");

            // actually instantiate the Shell.Application COM object
            var shellObj = Activator.CreateInstance(shell);

            // grab all the current windows
            var windows = shellObj.GetType().InvokeMember("Windows", BindingFlags.InvokeMethod, null, shellObj, null);

            // grab the open tab count
            var openTabs = windows.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, windows, null);
            var openTabsCount = int.Parse(openTabs.ToString());

            for (var i = 0; i < openTabsCount; i++)
            {
                // grab the acutal tab
                object? item = windows.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, windows, new object[] { i });
                string locationName = "", locationUrl = "";

                try
                {
                    // extract the tab properties
                    locationName = (string)item.GetType().InvokeMember("LocationName", BindingFlags.GetProperty, null, item, null);
                    locationUrl = (string)item.GetType().InvokeMember("LocationUrl", BindingFlags.GetProperty, null, item, null);
                    
                    Marshal.ReleaseComObject(item);
                    item = null;
                }
                catch { }

                // ensure we have a site address
                if (Regex.IsMatch(locationUrl, @"(^https?://.+)|(^ftp://)"))
                {
                    yield return new InternetExplorerTabsDTO(
                        locationName,
                        locationUrl
                    );
                }
            }

            Marshal.ReleaseComObject(windows);
            Marshal.ReleaseComObject(shellObj);
        }

        internal class InternetExplorerTabsDTO : CommandDTOBase
        {
            public InternetExplorerTabsDTO(string locationName, string locationUrl)
            {
                LocationName = locationName;
                LocationUrl = locationUrl;
            }
            public string LocationName { get; }
            public string LocationUrl { get; }
        }
    }
}
