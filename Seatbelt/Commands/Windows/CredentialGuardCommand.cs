using System.Collections.Generic;
using System.Linq;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using System.Management;
using System;

namespace Seatbelt.Commands
{
    enum VBS
    {
        NOT_ENABLED = 0,
        ENABLED_NOT_RUNNING = 1,
        ENABLED_AND_RUNNING = 2
    }

    internal class CredentialGuardCommand : CommandBase
    {
        public override string Command => "CredGuard";
        public override string Description => "CredentialGuard configuration";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;


        public CredentialGuardCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // adapted from @chrismaddalena's PR (https://github.com/GhostPack/Seatbelt/pull/22/files)

            // settings reference - https://www.tenforums.com/tutorials/68926-verify-if-device-guard-enabled-disabled-windows-10-a.html

            ManagementObjectCollection? data = null;
            try
            {
                var wmiData = ThisRunTime.GetManagementObjectSearcher(@"root\Microsoft\Windows\DeviceGuard", "SELECT * FROM Win32_DeviceGuard");
                data = wmiData.Get();
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
            {
                WriteError(string.Format("  [X] 'Win32_DeviceGuard' WMI class unavailable", ex.Message));
            }
            catch (Exception ex)
            {
                WriteError(ex.ToString());
            }

            if (data == null)
            {
                yield break;
            }

            foreach (var result in data)
            {
                // reference:
                //  https://github.com/GhostPack/Seatbelt/blob/f47c342150e70e96669017bbec258e27227ba1ef/Seatbelt/Program.cs#L1754-L1766

                var configCheck = (int[])result.GetPropertyValue("SecurityServicesConfigured");
                var serviceCheck = (int[])result.GetPropertyValue("SecurityServicesRunning");

                var vbsSetting = (VBS)0;
                var configured = false;
                var running = false;

                uint? vbs = (uint)result.GetPropertyValue("VirtualizationBasedSecurityStatus");
                if(vbs != null)
                {
                    vbsSetting = (VBS)vbs;
                }

                if(configCheck.Contains(1))
                {
                    configured = true;
                }

                if (serviceCheck.Contains(1))
                {
                    running = true;
                }

                yield return new CredGuardDTO()
                {
                    VirtualizationBasedSecurityStatus = vbsSetting,
                    Configured = configured,
                    Running = running
                };
            }
        }

        class CredGuardDTO : CommandDTOBase
        {
            public VBS VirtualizationBasedSecurityStatus { get; set; }
            
            public bool Configured { get; set; }

            public bool Running { get; set; }
        }

        [CommandOutputType(typeof(CredGuardDTO))]
        internal class CredentialGuardFormatter : TextFormatterBase
        {
            public CredentialGuardFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (CredGuardDTO)result;

                
            }
        }
    }
}