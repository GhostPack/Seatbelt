using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Seatbelt.Util;

namespace Seatbelt.Commands.Windows
{
    internal class ServicesCommand : CommandBase
    {
        public override string Command => "Services";
        public override string Description => "Services with file info company names that don't contain 'Microsoft', \"-full\" dumps all processes";
        public override CommandGroup[] Group => new[] { CommandGroup.System };
        public override bool SupportRemote => false; // tracking back some of the  service stuff needs local API calls

        public ServicesCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // lists installed services that don't have "Microsoft Corporation" as the company name in their file info
            //      or all services if "-full" is passed

            WriteHost(Runtime.FilterResults ? "Non Microsoft Services (via WMI)\n" : "All Services (via WMI)\n");

            var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_service");
            var data = wmiData.Get();

            foreach (ManagementObject result in data)
            {
                string? serviceName = (string)result["Name"];

                if (args.Length > 0 && !args.Contains(serviceName)) continue;

                string? companyName = null;
                string? description = null;
                string? version = null;
                string? binaryPathSddl = null;
                bool? isDotNet = null;

                string? serviceCommand = GetServiceCommand(result);
                string? binaryPath = serviceCommand == null ? null : GetServiceBinaryPath(serviceCommand);
                string? serviceDll = serviceName == null ? null : GetServiceDll(serviceName);


                // ServiceDll could be null if access to the Parameters key is denied 
                //  - Examples: The lmhosts service on Win10 as an unprivileged user
                if (binaryPath != null && binaryPath.ToLower().EndsWith("\\svchost.exe") && serviceDll != null)
                {
                    binaryPath = serviceDll;
                }

                if (!string.IsNullOrEmpty(binaryPath) && File.Exists(binaryPath))
                {
                    try
                    {
                        var myFileVersionInfo = FileVersionInfo.GetVersionInfo(binaryPath);
                        companyName = myFileVersionInfo.CompanyName;
                        description = myFileVersionInfo.FileDescription;
                        version = myFileVersionInfo.FileVersion;
                    }
                    catch
                    {
                        // ignored
                    }


                    if (Runtime.FilterResults && args.Length == 0)
                    {
                        if (companyName != null && Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase))
                        {
                            continue;
                        }
                    }

                    isDotNet = binaryPath == null ? null : (bool?)FileUtil.IsDotNetAssembly(binaryPath);

                    try
                    {
                        binaryPathSddl = File.GetAccessControl(binaryPath)
                            .GetSecurityDescriptorSddlForm(AccessControlSections.Owner | AccessControlSections.Access);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        WriteWarning($"Could not get the SDDL of service binary '{binaryPath}': Access denied");
                    }
                }

                var serviceSddl = serviceName == null ? null : TryGetServiceSddl(serviceName);

                yield return new ServicesDTO(
                    serviceName,
                    (string)result["DisplayName"],
                    (string)result["Description"],
                    (string)result["StartName"],
                    (string)result["State"],
                    (string)result["StartMode"],
                    (string)result["ServiceType"],
                    serviceCommand,
                    binaryPath,
                    binaryPathSddl,
                    serviceDll,
                    serviceSddl,
                    companyName,
                    description,
                    version,
                    isDotNet
                );
            }
        }

        private string? TryGetServiceSddl(string serviceName)
        {
            try
            {
                var info = SecurityUtil.GetSecurityInfos(serviceName, Interop.Advapi32.SE_OBJECT_TYPE.SE_SERVICE);
                return info.SDDL;
            }
            catch
            {
                return null;
            }
        }


        private string? GetServiceDll(string serviceName)
        {
            // ServiceDll's can be at the following locations
            //  - HKLM\\SYSTEM\\CurrentControlSet\\Services\\ ! ServiceDll
            //    - Ex: DoSvc on Win10
            //  - HKLM\\SYSTEM\\CurrentControlSet\\Services\\Parameters ! ServiceDll
            //    - Ex: DnsCache on Win10

            string? path = null;

            try
            {
                path = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, $"SYSTEM\\CurrentControlSet\\Services\\{serviceName}\\Parameters", "ServiceDll");
            }
            catch
            {
            }

            if (path != null)
                return path;

            try
            {
                path = RegistryUtil.GetStringValue(RegistryHive.LocalMachine, $"SYSTEM\\CurrentControlSet\\Services\\{serviceName}", "ServiceDll");
            }
            catch
            {
                // ignored
            }

            return path;
        }

        private string? GetServiceCommandFromRegistry(string serviceName)
        {
            try
            {
                return RegistryUtil.GetStringValue(RegistryHive.LocalMachine, $"SYSTEM\\CurrentControlSet\\Services\\{serviceName}", "ImagePath");
            }
            catch
            {
                return null;
            }
        }

        // TODO: Parsing binary paths is hard...
        //  - 1) We don't account for PATHEXT
        //      - Example image path: C:\windows\system32\cmd
        //  - 2) We don't account for the PATH environment variable
        //      - Example image path: cmd.exe
        //      - Example image path: cmd    (combination of 1 & 2) 
        //  - 3) We don't account for per-user services in Win 10 (see https://docs.microsoft.com/en-us/windows/application-management/per-user-services-in-windows)
        private string GetServiceBinaryPath(string command)
        {
            //// The "Path Name" for a service can include a fully quoted path (that includes spaces), as well as
            //// Program arguments (such as the ones that live inside svchost). Some paths, such as Carbon Black's agent)
            //// don't even have a file extension. So it's fair to say that if there are quotes, we'll take what's inside
            //// them, otherwise we'll split on spaces and take the first entry, regardless of its extension).
            //// Example: "C:\Program Files\Windows Defender\MsMpEng.exe"
            //if (command.StartsWith("\""))
            //{
            //    // Quotes are present, so split on quotes. Given that this is a service path,
            //    // it's fair to assume that the path is valid (otherwise the service wouldn't
            //    // be installed) and so we can just rip out the bit between the quotes. This
            //    // split should result in a minimum of 2 parts, so taking the second should
            //    // give us what we need.
            //    return command.Split('"')[1];
            //}
            //else
            //{
            //    // Exmaple image paths we have to deal with:
            //    //   1) C:\Program Files\Windows Identity Foundation\v3.5\c2wtshost.exe
            //    //   2) C:\WINDOWS\system32\msiexec.exe /V
            //    //   3) C:\WINDOWS\system32\svchost.exe -k appmodel -p
            //    if (File.Exists(command))  // Case 1
            //    {
            //        return command;
            //    }
            //    else // Case 2 & 3
            //    {
            //        return command.Split(' ')[0];
            //    }
            //}

            var path = Regex.Match(command, @"^\W*([a-z]:\\.+?(\.exe|\.dll|\.sys))\W*", RegexOptions.IgnoreCase);
            return path.Groups[1].ToString();
        }

        private string? GetServiceCommand(ManagementObject result)
        {
            // Get the service's path.  Sometimes result["PathName"] is not populated, so
            // in those cases we'll try and get the value from the registry. The converse is
            // also true - sometimes we can't acccess a registry key, but result["PathName"]
            // is populated
            string? serviceCommand;
            if (result["PathName"] != null)
            {
                serviceCommand = ((string)result["PathName"]).Trim();
                if (serviceCommand == string.Empty)
                {
                    serviceCommand = GetServiceCommandFromRegistry((string)result["Name"]);
                }
            }
            else
            {
                serviceCommand = GetServiceCommandFromRegistry((string)result["Name"]);
            }

            return serviceCommand;
        }
    }

    internal class ServicesDTO : CommandDTOBase
    {
        public ServicesDTO(string? name, string? displayName, string? description, string? user, string? state, string? startMode, string? type, string? serviceCommand, string? binaryPath, string? binaryPathSddl, string? serviceDll, string? serviceSddl, string? companyName, string? fileDescription, string? version, bool? isDotNet)
        {
            Name = name;
            DisplayName = displayName;
            Description = description;
            User = user;
            State = state;
            StartMode = startMode;
            Type = type;
            ServiceCommand = serviceCommand;
            BinaryPath = binaryPath;
            BinaryPathSDDL = binaryPathSddl;
            ServiceDll = serviceDll;
            ServiceSDDL = serviceSddl;
            CompanyName = companyName;
            FileDescription = fileDescription;
            Version = version;
            IsDotNet = isDotNet;
        }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? User { get; set; }
        public string? State { get; set; }
        public string? StartMode { get; set; }
        public string? Type { get; set; }
        public string? ServiceCommand { get; set; }
        public string? BinaryPath { get; set; }
        public string? BinaryPathSDDL { get; set; }
        public string? ServiceDll { get; set; }
        public string? ServiceSDDL { get; set; }
        public string? CompanyName { get; set; }
        public string? FileDescription { get; set; }
        public string? Version { get; set; }
        public bool? IsDotNet { get; set; }
    }
}
