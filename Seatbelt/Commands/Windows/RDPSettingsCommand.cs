using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;
using System.Collections.Generic;


namespace Seatbelt.Commands.Windows
{
    internal class RDPSettings : CommandBase
    {
        public override string Command => "RDPsettings";
        public override string Description => "Remote Desktop Server/Client Settings";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public RDPSettings(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // Client settings
            var credDelegKey = @"Software\Policies\Microsoft\Windows\CredentialsDelegation";
            var restrictedAdmin = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, credDelegKey,
                "RestrictedRemoteAdministration");
            var restrictedAdminType = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, credDelegKey,
                "RestrictedRemoteAdministrationType");
            var serverAuthLevel = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine,
                @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "AuthenticationLevel");

            var termServKey = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services";
            var disablePwSaving =
                RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "DisablePasswordSaving");

            // Server settings
            var nla = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "UserAuthentication");
            var blockClipboard = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "fDisableClip");
            var blockComPort = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "fDisableCcm");
            var blockDrives = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "fDisableCdm");
            var blockLptPort = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "fDisableLPT");
            var blockSmartCard = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "fEnableSmartCard");
            var blockPnp = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "fDisablePNPRedir");
            var blockPrinters = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, termServKey, "fDisableCpm");

            yield return new RDPSettingsDTO(
                new RDPClientSettings(
                    restrictedAdmin != null && restrictedAdmin != 0,
                    restrictedAdminType,
                    serverAuthLevel,
                    disablePwSaving == null || disablePwSaving == 1),
                new RDPServerSettings(
                    nla,
                    blockClipboard,
                    blockComPort,
                    blockDrives,
                    blockLptPort,
                    blockSmartCard,
                    blockPnp,
                    blockPrinters
                    )
            );
        }
    }

    internal class RDPClientSettings
    {
        public RDPClientSettings(bool restrictedRemoteAdministration, uint? restrictedRemoteAdministrationType,
            uint? serverAuthLevel, bool disablePasswordSaving)
        {
            RestrictedRemoteAdministration = restrictedRemoteAdministration;
            RestrictedRemoteAdministrationType = restrictedRemoteAdministrationType;
            ServerAuthLevel = serverAuthLevel;
            DisablePasswordSaving = disablePasswordSaving;
        }

        public bool RestrictedRemoteAdministration { get; }
        public uint? RestrictedRemoteAdministrationType { get; }
        public uint? ServerAuthLevel { get; }
        public bool DisablePasswordSaving { get; }
    }

    internal class RDPServerSettings
    {
        public RDPServerSettings(uint? networkLevelAuthentication, uint? blockClipboardRedirection, uint? blockComPortRedirection, uint? blockDriveRedirection, uint? blockLptPortRedirection, uint? allowSmartCardRedirection, uint? blockPnPDeviceRedirection, uint? blockPrinterRedirection)
        {
            NetworkLevelAuthentication = networkLevelAuthentication;
            BlockClipboardRedirection = blockClipboardRedirection;
            BlockComPortRedirection = blockComPortRedirection;
            BlockDriveRedirection = blockDriveRedirection;
            BlockLptPortRedirection = blockLptPortRedirection;
            AllowSmartCardRedirection = allowSmartCardRedirection;
            BlockPnPDeviceRedirection = blockPnPDeviceRedirection;
            BlockPrinterRedirection = blockPrinterRedirection;  
        }
        public uint? NetworkLevelAuthentication { get; }
        public uint? BlockClipboardRedirection { get; }
        public uint? BlockComPortRedirection { get; }
        public uint? BlockDriveRedirection { get; }
        public uint? BlockLptPortRedirection { get; }
        public uint? AllowSmartCardRedirection { get; }
        public uint? BlockPnPDeviceRedirection { get; }
        public uint? BlockPrinterRedirection { get; }

    }

    internal class RDPSettingsDTO : CommandDTOBase
    {
        public RDPSettingsDTO(RDPClientSettings clientSettings, RDPServerSettings serverSettings)
        {
            ClientSettings = clientSettings;
            ServerSettings = serverSettings;
        }
        public RDPClientSettings ClientSettings { get; }
        public RDPServerSettings ServerSettings { get; }
    }

    [CommandOutputType(typeof(RDPSettingsDTO))]
    internal class RDPSettingsFormatter : TextFormatterBase
    {
        public RDPSettingsFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (RDPSettingsDTO)result;
            string str;

            var server = dto.ServerSettings;
            WriteLine("RDP Server Settings:");
            WriteLine($"  NetworkLevelAuthentication: {server.NetworkLevelAuthentication}");
            WriteLine($"  BlockClipboardRedirection:  {server.BlockClipboardRedirection}");
            WriteLine($"  BlockComPortRedirection:    {server.BlockComPortRedirection}");
            WriteLine($"  BlockDriveRedirection:      {server.BlockDriveRedirection}");
            WriteLine($"  BlockLptPortRedirection:    {server.BlockLptPortRedirection}");
            WriteLine($"  BlockPnPDeviceRedirection:  {server.BlockPnPDeviceRedirection}");
            WriteLine($"  BlockPrinterRedirection:    {server.BlockPrinterRedirection}");
            WriteLine($"  AllowSmartCardRedirection:  {server.AllowSmartCardRedirection}");

            WriteLine("\nRDP Client Settings:");
            WriteLine($"  DisablePasswordSaving: {dto.ClientSettings.DisablePasswordSaving}");
            WriteLine($"  RestrictedRemoteAdministration: {dto.ClientSettings.RestrictedRemoteAdministration}");

            var type = dto.ClientSettings.RestrictedRemoteAdministrationType;

            if (type != null)
            {
                if (type == 1)
                    str = "Require Restricted Admin Mode";
                else if (type == 2)
                    str = "Require Remote Credential Guard";
                else if (type == 3)
                    str = "Require Restricted Admin or Remote Credential Guard";
                else
                    str = $"{type} - Unknown, please report this";

                WriteLine($"  RestrictedRemoteAdministrationType: {str}");
            }

            var level = dto.ClientSettings.ServerAuthLevel;
            if (level != null)
            {
                if (level == 1)
                    str = "Require Restricted Admin Mode";
                else if (level == 2)
                    str = "Require Remote Credential Guard";
                else if (level == 3)
                    str = "Require Restricted Admin or Remote Credential Guard";
                else
                    str = $"Unknown, please report this";

                WriteLine($"  ServerAuthenticationLevel: {level} - {str}");
            }
        }
    }
}