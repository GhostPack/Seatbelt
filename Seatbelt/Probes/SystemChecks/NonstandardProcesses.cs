using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Seatbelt.Probes.SystemChecks
{
    public class NonstandardProcesses :IProbe
    {

        public static string ProbeName => "NonstandardProcesses";


        private static string CleanFileName(string fileName)
            => fileName.TrimStart(@"\\?\".ToCharArray());

        public string List()
        {
            var sb = new StringBuilder();

            // lists currently running processes that don't have "Microsoft Corporation" as the company name in their file info
            //      or all processes if "full" is passed

            if (FilterResults.Filter)
            {
                sb.AppendProbeHeaderLine("Non Microsoft Processes (via WMI)");
            }
            else
            {
                sb.AppendProbeHeaderLine("All Processes (via WMI)");
            }

            try
            {
                var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
                using (var searcher = new ManagementObjectSearcher(wmiQueryString))
                using (var results = searcher.Get())
                {
                    var query = from p in Process.GetProcesses()
                                join mo in results.Cast<ManagementObject>()
                                on p.Id equals (int)(uint)mo["ProcessId"]
                                select new
                                {
                                    Process = p,
                                    Path = (string)mo["ExecutablePath"],
                                    CommandLine = (string)mo["CommandLine"],
                                };
                    foreach (var item in query)
                    {
                        //OLD -  if ((item.Path != null) && ((!FilterResults.filter) || (!Regex.IsMatch(item.Path, "C:\\\\WINDOWS\\\\", RegexOptions.IgnoreCase))))
                        if ((item.Path != null))
                        {
                            var pathWithTrimmedIllegalCharacters = CleanFileName(item.Path);

                            var myFileVersionInfo = FileVersionInfo.GetVersionInfo(pathWithTrimmedIllegalCharacters);
                            var companyName = myFileVersionInfo.CompanyName;
                            if ((String.IsNullOrEmpty(companyName)) || (!FilterResults.Filter) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                            {
                                var isDotNet = false;
                                try
                                {
                                    var myAssemblyName = AssemblyName.GetAssemblyName(pathWithTrimmedIllegalCharacters);
                                    isDotNet = true;
                                }
                                catch (FileNotFoundException)
                                {
                                    // System.Console.WriteLine("The file cannot be found.");
                                }
                                catch (BadImageFormatException exception)
                                {
                                    if (Regex.IsMatch(exception.Message, ".*This assembly is built by a runtime newer than the currently loaded runtime and cannot be loaded.*", RegexOptions.IgnoreCase))
                                    {
                                        isDotNet = true;
                                    }
                                }
                                catch
                                {
                                    // System.Console.WriteLine("The assembly has already been loaded.");
                                }

                                sb.AppendLine($"  Name           : {item.Process.ProcessName}");
                                sb.AppendLine($"  Company Name   : {companyName}");
                                sb.AppendLine($"  PID            : {item.Process.Id}");
                                sb.AppendLine($"  Path           : {item.Path}");
                                sb.AppendLine($"  CommandLine    : {item.CommandLine}");
                                sb.AppendLine($"  IsDotNet       : {isDotNet}");
                                sb.AppendLine();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }
    }
}
