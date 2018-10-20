using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Seatbelt.Probes.UserChecks
{
    public class IETabs : IProbe
    {

        public static string ProbeName => "IETabs";


        public string List()
        {
            var sb = new StringBuilder();

            // Lists currently open Internet Explorer tabs, via COM
            // Notes:
            //  https://searchcode.com/codesearch/view/9859954/
            //  https://gist.github.com/yizhang82/a1268d3ea7295a8a1496e01d60ada816

            sb.AppendProbeHeaderLine("Internet Explorer Open Tabs");

            try
            {
                // Shell.Application COM GUID
                var shell = Type.GetTypeFromCLSID(new Guid("13709620-C279-11CE-A49E-444553540000"));

                // actually instantiate the Shell.Application COM object
                var shellObj = Activator.CreateInstance(shell);

                // grab all the current windows
                var windows = shellObj.GetType().InvokeMember("Windows", BindingFlags.InvokeMethod, null, shellObj, null);

                // grab the open tab count
                var openTabs = windows.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, windows, null);
                var openTabsCount = Int32.Parse(openTabs.ToString());

                for (var i = 0; i < openTabsCount; i++)
                {
                    // grab the acutal tab
                    var item = windows.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, windows, new object[] { i });
                    try
                    {
                        // extract the tab properties
                        var locationName = item.GetType().InvokeMember("LocationName", BindingFlags.GetProperty, null, item, null);
                        var locationURL = item.GetType().InvokeMember("LocationUrl", BindingFlags.GetProperty, null, item, null);

                        // ensure we have a site address
                        if (Regex.IsMatch(locationURL.ToString(), @"(^https?://.+)|(^ftp://)"))
                        {
                            sb.AppendLine($"  Location Name : {locationName}");
                            sb.AppendLine($"  Location URL  : {locationURL}");
                            sb.AppendLine();
                        }
                        Marshal.ReleaseComObject(item);
                        item = null;
                    }
                    catch
                    {
                        //
                    }
                }
                Marshal.ReleaseComObject(windows);
                windows = null;
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }
    }
}
