using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Seatbelt.Probes.UserChecks
{
    public class RecentFiles : IProbe
    {
        public static string ProbeName => "RecentFiles";


        public string List()
        {
            var sb = new StringBuilder(4096);

            // parses recent file shortcuts via COM

            var lastDays = 7;

            if (!FilterResults.Filter)
                lastDays = 30;

            var startTime = DateTime.Now.AddDays(-lastDays);

            // WshShell COM object GUID 
            var shell = Type.GetTypeFromCLSID(new Guid("F935DC22-1CF0-11d0-ADB9-00C04FD58A0B"));
            var shellObj = Activator.CreateInstance(shell);

            try
            {

                if (Helpers.IsHighIntegrity())
                    RunForHighIntegrity(sb, lastDays, startTime, shellObj);
                else
                    RunForOtherIntegrity(sb, lastDays, startTime, shellObj);


            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }
            finally
            {
                // release the WshShell COM object
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }

            return sb.ToString();
        }

        private static void RunForOtherIntegrity(StringBuilder sb, int lastDays, DateTime startTime, object shellObj)
        {
            sb.AppendProbeHeaderLine($"Recently Accessed Files (Current User) Last {lastDays} Days");

            var recentPath = String.Format("{0}\\Microsoft\\Windows\\Recent\\", Environment.GetEnvironmentVariable("APPDATA"));

            var recentFiles = Directory.GetFiles(recentPath, "*.lnk", SearchOption.AllDirectories);

            foreach (var recentFile in recentFiles)
            {
                // old method (needed interop dll)
                //WshShell shell = new WshShell();
                //IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(recentFile);

                var lastAccessed = File.GetLastAccessTime(recentFile);
                if (lastAccessed <= startTime) continue;

                // invoke the WshShell com object, creating a shortcut to then extract the TargetPath from
                var shortcut = shellObj.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shellObj, new object[] {recentFile});
                var targetPath = shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, new object[] { });
                if (targetPath.ToString().Trim() != "")
                {
                    sb.AppendLine($"    Target:       {targetPath,-10}");
                    sb.AppendLine($"        Accessed: {lastAccessed}");
                    sb.AppendLine();
                }

                Marshal.ReleaseComObject(shortcut);
                shortcut = null;
            }
        }

        private static void RunForHighIntegrity(StringBuilder sb, int lastDays, DateTime startTime, object shellObj)
        {
            sb.AppendProbeHeaderLine($"Recently Accessed Files (All Users) Last {lastDays} Days");

            var userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));

            foreach (var dir in Directory.GetDirectories(userFolder))
            {
                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];

                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users")) continue;

                var recentPath = String.Format("{0}\\AppData\\Roaming\\Microsoft\\Windows\\Recent\\", dir);
                try
                {
                    var recentFiles = Directory.GetFiles(recentPath, "*.lnk", SearchOption.AllDirectories);

                    if (recentFiles.Length != 0)
                    {
                        sb.AppendLine($"   {userName} :").AppendLine();

                        foreach (var recentFile in recentFiles)
                        {
                            var lastAccessed = File.GetLastAccessTime(recentFile);

                            if (lastAccessed <= startTime) continue;

                            // invoke the WshShell com object, creating a shortcut to then extract the TargetPath from
                            var shortcut = shellObj.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                                null, shellObj, new object[] {recentFile});
                            var targetPath = shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null,
                                shortcut, new object[] { });

                            if (targetPath.ToString().Trim() != "")
                            {
                                sb.AppendLine($"      Target:       {targetPath,-10}");
                                sb.AppendLine($"          Accessed: {lastAccessed}");
                                sb.AppendLine();
                            }

                            Marshal.ReleaseComObject(shortcut);
                            shortcut = null;
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}
