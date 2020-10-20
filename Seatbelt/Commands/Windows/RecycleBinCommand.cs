#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Seatbelt.Commands.Windows
{
    internal class RecycleBinCommand : CommandBase
    {
        public override string Command => "RecycleBin";
        public override string Description => "Items in the Recycle Bin deleted in the last 30 days - only works from a user context!";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false; // not possible

        public RecycleBinCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists recently deleted files (needs to be run from a user context!)

            // Reference: https://stackoverflow.com/questions/18071412/list-filenames-in-the-recyclebin-with-c-sharp-without-using-any-external-files
            WriteHost("Recycle Bin Files Within the last 30 Days\n");

            var lastDays = 30;

            var startTime = DateTime.Now.AddDays(-lastDays);

            // Shell COM object GUID
            var shell = Type.GetTypeFromProgID("Shell.Application");
            var shellObj = Activator.CreateInstance(shell);

            // namespace for recycle bin == 10 - https://msdn.microsoft.com/en-us/library/windows/desktop/bb762494(v=vs.85).aspx
            var recycle = shellObj.GetType().InvokeMember("Namespace", BindingFlags.InvokeMethod, null, shellObj, new object[] { 10 });
            // grab all the deletes items
            var items = recycle.GetType().InvokeMember("Items", BindingFlags.InvokeMethod, null, recycle, null);
            // grab the number of deleted items
            var count = items.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, items, null);
            var deletedCount = int.Parse(count.ToString());

            // iterate through each item
            for (var i = 0; i < deletedCount; i++)
            {
                // grab the specific deleted item
                var item = items.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, items, new object[] { i });
                var DateDeleted = item.GetType().InvokeMember("ExtendedProperty", BindingFlags.InvokeMethod, null, item, new object[] { "System.Recycle.DateDeleted" });
                var modifiedDate = DateTime.Parse(DateDeleted.ToString());
                if (modifiedDate > startTime)
                {
                    // additional extended properties from https://blogs.msdn.microsoft.com/oldnewthing/20140421-00/?p=1183
                    var Name = item.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, item, null);
                    var Path = item.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, item, null);
                    var Size = item.GetType().InvokeMember("Size", BindingFlags.GetProperty, null, item, null);
                    var DeletedFrom = item.GetType().InvokeMember("ExtendedProperty", BindingFlags.InvokeMethod, null, item, new object[] { "System.Recycle.DeletedFrom" });

                    yield return new RecycleBinDTO()
                    {
                        Name = Name.ToString(),
                        Path = Path.ToString(),
                        Size = (int)Size,
                        DeletedFrom = DeletedFrom.ToString(),
                        DateDeleted = (DateTime)DateDeleted
                    };
                }

                Marshal.ReleaseComObject(item);
                item = null;
            }

            Marshal.ReleaseComObject(recycle);
            recycle = null;
            Marshal.ReleaseComObject(shellObj);
            shellObj = null;
        }

        internal class RecycleBinDTO : CommandDTOBase
        {
            public string Name { get; set; }

            public string Path { get; set; }

            public int Size { get; set; }

            public string DeletedFrom { get; set; }

            public DateTime DateDeleted { get; set; }
        }
    }
}
#nullable enable