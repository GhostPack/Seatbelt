using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Seatbelt.Probes.SystemChecks
{

    // NOTE: This code from https://github.com/GhostPack/Seatbelt/pull/14#
    // Fix issues with parsing of Service binary paths
    // from OJ - https://github.com/OJ

    public class NonstandardServices : IProbe
    {
        public static string ProbeName => "NonstandardServices";

        private readonly Regex _msPattern = new Regex(@"^Microsoft.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _assemblyRuntimePattern = new Regex(".*This assembly is built by a runtime newer than the currently loaded runtime and cannot be loaded.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public string List()
        {
            var sb = new StringBuilder();

            // lists installed services that don't have "Microsoft Corporation" as the company name in their file info
            //      or all services if "full" is passed

            if (FilterResults.Filter)
            {
                sb.AppendProbeHeaderLine("Non Microsoft Services (via WMI)");
            }
            else
            {
                sb.AppendProbeHeaderLine("All Services (via WMI)");
            }

            try
            {
                var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_service");
                var data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    // Bail for null items
                    if (result["PathName"] == null || string.IsNullOrEmpty(result["PathName"].ToString()))
                        continue;

                    // The "Path Name" for a service can include a fully quoted path (that includes spaces), as well as
                    // Program arguments (such as the ones that live inside svchost). Some paths, such as Carbon Black's agent)
                    // don't even have a file extension. So it's fair to say that if there are quotes, we'll take what's inside
                    // them, otherwise we'll split on spaces and take the first entry, regardless of its extension).
                    var binaryPath = result["PathName"].ToString().Trim();
                    if (binaryPath.Contains("\""))
                    {
                        // Quotes are present, so split on quotes. Given that this is a service path,
                        // it's fair to assume that the path is valid (otherwise the service wouldn't
                        // be installed) and so we can just rip out the bit between the quotes. This
                        // split should result in a minimum of 2 parts, so taking the second should
                        // give us what we need.
                        binaryPath = binaryPath.Split('"')[1];
                    }
                    else
                    {
                        // No quotes, so it's safe to assume that we can split on spaces and take the first element.
                        binaryPath = binaryPath.Split(' ')[0];
                    }
                    if (!string.IsNullOrEmpty(binaryPath) && File.Exists(binaryPath))
                    {
                        var companyName = "";
                        try
                        {
                            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(binaryPath);
                            companyName = myFileVersionInfo.CompanyName;
                        }
                        catch
                        {
                            // Nope! Something is up, but let's not bail on the entire loop like we were doing so before.
                            continue;
                        }

                        if ((!string.IsNullOrEmpty(companyName)) && (FilterResults.Filter) && (_msPattern.IsMatch(companyName))) continue;

                        bool isDotNet = false;
                        try
                        {
                            AssemblyName myAssemblyName = AssemblyName.GetAssemblyName(binaryPath);
                            isDotNet = true;
                        }
                        catch (FileNotFoundException)
                        {
                            // System.Console.WriteLine("The file cannot be found.");
                        }
                        catch (BadImageFormatException exception)
                        {
                            if (_assemblyRuntimePattern.IsMatch(exception.Message))
                                isDotNet = true;
                        }
                        catch
                        {
                            // System.Console.WriteLine("The assembly has already been loaded.");
                        }

                        sb.AppendLine($"  Name             : {result["Name"]}");
                        sb.AppendLine($"  DisplayName      : {result["DisplayName"]}");
                        sb.AppendLine($"  Company Name     : {companyName}");
                        sb.AppendLine($"  Description      : {result["Description"]}");
                        sb.AppendLine($"  State            : {result["State"]}");
                        sb.AppendLine($"  StartMode        : {result["StartMode"]}");
                        sb.AppendLine($"  PathName         : {result["PathName"]}");
                        sb.AppendLine($"  IsDotNet         : {isDotNet}\r\n");
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
