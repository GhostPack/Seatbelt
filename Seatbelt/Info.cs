using System.Text;

namespace Seatbelt
{
    public static class Info
    {
        public static string Logo()
        {
            var sb = new StringBuilder();

            sb.AppendLine().AppendLine();
            sb.AppendLine("                        %&&@@@&&                                                                                  ");
            sb.AppendLine("                        &&&&&&&%%%,                       #&&@@@@@@%%%%%%###############%                         ");
            sb.AppendLine("                        &%&   %&%%                        &////(((&%%%%%#%################//((((###%%%%%%%%%%%%%%%");
            sb.AppendLine("%%%%%%%%%%%######%%%#%%####%  &%%**#                      @////(((&%%%%%%######################(((((((((((((((((((");
            sb.AppendLine("#%#%%%%%%%#######%#%%#######  %&%,,,,,,,,,,,,,,,,         @////(((&%%%%%#%#####################(((((((((((((((((((");
            sb.AppendLine("#%#%%%%%%#####%%#%#%%#######  %%%,,,,,,  ,,.   ,,         @////(((&%%%%%%%######################(#(((#(#((((((((((");
            sb.AppendLine("#####%%%####################  &%%......  ...   ..         @////(((&%%%%%%%###############%######((#(#(####((((((((");
            sb.AppendLine("#######%##########%#########  %%%......  ...   ..         @////(((&%%%%%#########################(#(#######((#####");
            sb.AppendLine("###%##%%####################  &%%...............          @////(((&%%%%%%%%##############%#######(#########((#####");
            sb.AppendLine("#####%######################  %%%..                       @////(((&%%%%%%%################                        ");
            sb.AppendLine("                        &%&   %%%%%      Seatbelt         %////(((&%%%%%%%%#############*                         ");
            sb.AppendLine("                        &%%&&&%%%%%        v0.2.0         ,(((&%%%%%%%%%%%%%%%%%,                                 ");
            sb.AppendLine("                         #%%%%##,                                                                                 ").AppendLine().AppendLine();

            return sb.ToString();
        }

        public static string Usage()
        {
            var sb = new StringBuilder();

            sb.AppendLine(" \"SeatBelt.exe system\" collects the following system data:").AppendLine();
            sb.AppendLine("\tBasicOSInfo           -   Basic OS info (i.e. architecture, OS version, etc.)");
            sb.AppendLine("\tRebootSchedule        -   Reboot schedule (last 15 days) based on event IDs 12 and 13");
            sb.AppendLine("\tTokenGroupPrivs       -   Current process/token privileges (e.g. SeDebugPrivilege/etc.)");
            sb.AppendLine("\tUACSystemPolicies     -   UAC system policies via the registry");
            sb.AppendLine("\tPowerShellSettings    -   PowerShell versions and security settings");
            sb.AppendLine("\tAuditSettings         -   Audit settings via the registry");
            sb.AppendLine("\tWEFSettings           -   Windows Event Forwarding (WEF) settings via the registry");
            sb.AppendLine("\tLSASettings           -   LSA settings (including auth packages)");
            sb.AppendLine("\tUserEnvVariables      -   Current user environment variables");
            sb.AppendLine("\tSystemEnvVariables    -   Current system environment variables");
            sb.AppendLine("\tUserFolders           -   Folders in C:\\Users\\");
            sb.AppendLine("\tNonstandardServices   -   Services with file info company names that don't contain 'Microsoft'");
            sb.AppendLine("\tInternetSettings      -   Internet settings including proxy configs");
            sb.AppendLine("\tLapsSettings          -   LAPS settings, if installed");
            sb.AppendLine("\tAppLockerSettings     -   AppLocker settings, if installed");
            sb.AppendLine("\tLocalGroupMembers     -   Members of local admins, RDP, and DCOM");
            sb.AppendLine("\tMappedDrives          -   Mapped drives");
            sb.AppendLine("\tRDPSessions           -   Current incoming RDP sessions");
            sb.AppendLine("\tWMIMappedDrives       -   Mapped drives via WMI");
            sb.AppendLine("\tNetworkShares         -   Network shares");
            sb.AppendLine("\tFirewallRules         -   Deny firewall rules, \"full\" dumps all");
            sb.AppendLine("\tAntiVirusWMI          -   Registered antivirus (via WMI)");
            sb.AppendLine("\tInterestingProcesses  -   \"Interesting\" processes- defensive products and admin tools");
            sb.AppendLine("\tRegistryAutoRuns      -   Registry autoruns");
            sb.AppendLine("\tRegistryAutoLogon     -   Registry autologon information");
            sb.AppendLine("\tDNSCache              -   DNS cache entries (via WMI)");
            sb.AppendLine("\tARPTable              -   Lists the current ARP table and adapter information (equivalent to arp -a)");
            sb.AppendLine("\tAllTcpConnections     -   Lists current TCP connections and associated processes");
            sb.AppendLine("\tAllUdpConnections     -   Lists current UDP connections and associated processes");
            sb.AppendLine("\tNonstandardProcesses  -   Running processes with file info company names that don't contain 'Microsoft'");
            sb.AppendLine("\t *  If the user is in high integrity, the following additional actions are run:");
            sb.AppendLine("\tSysmonConfig          -   Sysmon configuration from the registry").AppendLine().AppendLine();

            sb.AppendLine(" \"SeatBelt.exe user\" collects the following user data:").AppendLine();
            sb.AppendLine("\tSavedRDPConnections   -   Saved RDP connections");
            sb.AppendLine("\tTriageIE              -   Internet Explorer bookmarks and history  (last 7 days)");
            sb.AppendLine("\tDumpVault             -   Dump saved credentials in Windows Vault (i.e. logins from Internet Explorer and Edge), from SharpWeb");
            sb.AppendLine("\tRecentRunCommands     -   Recent \"run\" commands");
            sb.AppendLine("\tPuttySessions         -   Interesting settings from any saved Putty configurations");
            sb.AppendLine("\tPuttySSHHostKeys      -   Saved putty SSH host keys");
            sb.AppendLine("\tCloudCreds            -   AWS/Google/Azure cloud credential files");
            sb.AppendLine("\tRecentFiles           -   Parsed \"recent files\" shortcuts  (last 7 days)");
            sb.AppendLine("\tMasterKeys            -   List DPAPI master keys");
            sb.AppendLine("\tCredFiles             -   List Windows credential DPAPI blobs");
            sb.AppendLine("\tRDCManFiles           -   List Windows Remote Desktop Connection Manager settings files");
            sb.AppendLine("\t *  If the user is in high integrity, this data is collected for ALL users instead of just the current user").AppendLine().AppendLine();

            sb.AppendLine(" Non-default options:").AppendLine();
            sb.AppendLine("\tCurrentDomainGroups   -   The current user's local and domain groups");
            sb.AppendLine("\tPatches               -   Installed patches via WMI (takes a bit on some systems)");
            sb.AppendLine("\tLogonSessions         -   User logon session data");
            sb.AppendLine("\tKerberosTGTData       -   ALL TEH TGTZ!");
            sb.AppendLine("\tInterestingFiles      -   \"Interesting\" files matching various patterns in the user's folder");
            sb.AppendLine("\tIETabs                -   Open Internet Explorer tabs");
            sb.AppendLine("\tTriageChrome          -   Chrome bookmarks and history");
            sb.AppendLine("\tTriageFirefox         -   Firefox history (no bookmarks)");
            sb.AppendLine("\tRecycleBin            -   Items in the Recycle Bin deleted in the last 30 days - only works from a user context!");
            sb.AppendLine("\t4624Events            -   4624 logon events from the security event log");
            sb.AppendLine("\t4648Events            -   4648 explicit logon events from the security event log (runas or outbound RDP)");
            sb.AppendLine("\tKerberosTickets       -   List Kerberos tickets. If elevated, grouped by all logon sessions.");

            sb.AppendLine().AppendLine();
            sb.AppendLine(" \"SeatBelt.exe all\" will run ALL enumeration checks, can be combined with \"full\".");
            sb.AppendLine().AppendLine();
            sb.AppendLine(" \"SeatBelt.exe [CheckName] full\" will prevent any filtering and will return complete results.");
            sb.AppendLine().AppendLine();
            sb.AppendLine(" \"SeatBelt.exe [CheckName] [CheckName2] ...\" will run one or more specified checks only");
            sb.AppendLine().AppendLine();
            sb.AppendLine(" \"SeatBelt.exe ToFile \"file name here.txt\" [all|full|user|system|CheckName(s)]...\" will write the results to the named file");

            return sb.ToString();
        }
    }
}
