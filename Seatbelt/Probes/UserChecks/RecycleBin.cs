using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;


namespace Seatbelt.Probes.UserChecks
{
    public class RecycleBin  : IProbe
    {
        public static string ProbeName => "RecycleBin";

        public string List()
        {
            var sb = new StringBuilder();

            // lists recently deleted files (needs to be run from a user context!)

            // Reference: https://stackoverflow.com/questions/18071412/list-filenames-in-the-recyclebin-with-c-sharp-without-using-any-external-files

            sb.AppendProbeHeaderLine("Recycle Bin Files Within the last 30 Days");

            var lastDays = 30;

            var startTime = DateTime.Now.AddDays(-lastDays);

            // Shell COM object GUID
            var shell = Type.GetTypeFromCLSID(new Guid("13709620-C279-11CE-A49E-444553540000"));
            var shellObj = Activator.CreateInstance(shell);

            // namespace for recycle bin == 10 - https://msdn.microsoft.com/en-us/library/windows/desktop/bb762494(v=vs.85).aspx
            var recycle = shellObj.GetType().InvokeMember("Namespace", BindingFlags.InvokeMethod, null, shellObj, new object[] { 10 });
          
            // grab all the deletes items
            var items = recycle.GetType().InvokeMember("Items", BindingFlags.InvokeMethod, null, recycle, null);
            
            // grab the number of deleted items
            var count = items.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, items, null);
            var deletedCount = Int32.Parse(count.ToString());

            // iterate through each item
            for (var i = 0; i < deletedCount; i++)
            {
                // grab the specific deleted item
                var item = items.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, items, new object[] { i });
                var dateDeleted = item.GetType().InvokeMember("ExtendedProperty", BindingFlags.InvokeMethod, null, item, new object[] { "System.Recycle.DateDeleted" });
                var modifiedDate = DateTime.Parse(dateDeleted.ToString());
                if (modifiedDate > startTime)
                {
                    // additional extended properties from https://blogs.msdn.microsoft.com/oldnewthing/20140421-00/?p=1183
                    var name = item.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, item, null);
                    var path = item.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, item, null);
                    var size = item.GetType().InvokeMember("Size", BindingFlags.GetProperty, null, item, null);
                    var deletedFrom = item.GetType().InvokeMember("ExtendedProperty", BindingFlags.InvokeMethod, null, item, new object[] { "System.Recycle.DeletedFrom" });

                    sb.AppendLine($"  Name           : {name}");
                    sb.AppendLine($"  Path           : {path}");
                    sb.AppendLine($"  Size           : {size}");
                    sb.AppendLine($"  Deleted From   : {deletedFrom}");
                    sb.AppendLine($"  Date Deleted   : {dateDeleted}");
                    sb.AppendLine();

                }
                Marshal.ReleaseComObject(item);
                item = null;
            }

            Marshal.ReleaseComObject(recycle);
            recycle = null;

            Marshal.ReleaseComObject(shellObj);
            shellObj = null;

            return sb.ToString();
        }
    }
}
