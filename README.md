# Seatbelt

----

Seatbelt is a C# project that performs a number of security oriented host-survey "safety checks" relevant from both offensive and defensive security perspectives.

[@andrewchiles](https://twitter.com/andrewchiles)' [HostEnum.ps1](https://github.com/threatexpress/red-team-scripts/blob/master/HostEnum.ps1) script and [@tifkin\_](https://twitter.com/tifkin_)'s [Get-HostProfile.ps1](https://github.com/leechristensen/Random/blob/master/PowerShellScripts/Get-HostProfile.ps1) provided inspiration for many of the artifacts to collect.

[@harmj0y](https://twitter.com/harmj0y) and [@tifkin_](https://twitter.com/tifkin_) are the primary authors of this implementation.

Seatbelt is licensed under the BSD 3-Clause license.


## Table of Contents

- [Seatbelt](#seatbelt)
  * [Table of Contents](#table-of-contents)
  * [Command Line Usage](#command-line-usage)
  * [Command Groups](#command-groups)
    + [system](#system)
    + [user](#user)
    + [misc](#misc)
    + [Additional Command Groups](#additional-command-groups)
  * [Command Arguments](#command-arguments)
  * [Output](#output)
  * [Remote Enumeration](#remote-enumeration)
  * [Building Your Own Modules](#building-your-own-modules)
  * [Compile Instructions](#compile-instructions)
  * [Acknowledgments](#acknowledgments)


## Command Line Usage

```


                        %&&@@@&&                                                                                  
                        &&&&&&&%%%,                       #&&@@@@@@%%%%%%###############%                         
                        &%&   %&%%                        &////(((&%%%%%#%################//((((###%%%%%%%%%%%%%%%
%%%%%%%%%%%######%%%#%%####%  &%%**#                      @////(((&%%%%%%######################(((((((((((((((((((
#%#%%%%%%%#######%#%%#######  %&%,,,,,,,,,,,,,,,,         @////(((&%%%%%#%#####################(((((((((((((((((((
#%#%%%%%%#####%%#%#%%#######  %%%,,,,,,  ,,.   ,,         @////(((&%%%%%%%######################(#(((#(#((((((((((
#####%%%####################  &%%......  ...   ..         @////(((&%%%%%%%###############%######((#(#(####((((((((
#######%##########%#########  %%%......  ...   ..         @////(((&%%%%%#########################(#(#######((#####
###%##%%####################  &%%...............          @////(((&%%%%%%%%##############%#######(#########((#####
#####%######################  %%%..                       @////(((&%%%%%%%################                        
                        &%&   %%%%%      Seatbelt         %////(((&%%%%%%%%#############*                         
                        &%%&&&%%%%%        v1.2.1         ,(((&%%%%%%%%%%%%%%%%%,                                 
                         #%%%%##,                                                                                 


Available commands (+ means remote usage is supported):

    + AMSIProviders          - Providers registered for AMSI
    + AntiVirus              - Registered antivirus (via WMI)
    + AppLocker              - AppLocker settings, if installed
      ARPTable               - Lists the current ARP table and adapter information (equivalent to arp -a)
      AuditPolicies          - Enumerates classic and advanced audit policy settings
    + AuditPolicyRegistry    - Audit settings via the registry
    + AutoRuns               - Auto run executables/scripts/programs
      azuread                - Return AzureAD info
      Certificates           - Finds user and machine personal certificate files
      CertificateThumbprints - Finds thumbprints for all certificate store certs on the system
    + ChromiumBookmarks      - Parses any found Chrome/Edge/Brave/Opera bookmark files
    + ChromiumHistory        - Parses any found Chrome/Edge/Brave/Opera history files
    + ChromiumPresence       - Checks if interesting Chrome/Edge/Brave/Opera files exist
    + CloudCredentials       - AWS/Google/Azure/Bluemix cloud credential files
    + CloudSyncProviders     - All configured Office 365 endpoints (tenants and teamsites) which are synchronised by OneDrive.
      CredEnum               - Enumerates the current user's saved credentials using CredEnumerate()
    + CredGuard              - CredentialGuard configuration
      dir                    - Lists files/folders. By default, lists users' downloads, documents, and desktop folders (arguments == [directory] [maxDepth] [regex] [boolIgnoreErrors]
    + DNSCache               - DNS cache entries (via WMI)
    + DotNet                 - DotNet versions
    + DpapiMasterKeys        - List DPAPI master keys
      EnvironmentPath        - Current environment %PATH$ folders and SDDL information
    + EnvironmentVariables   - Current environment variables
    + ExplicitLogonEvents    - Explicit Logon events (Event ID 4648) from the security event log. Default of 7 days, argument == last X days.
      ExplorerMRUs           - Explorer most recently used files (last 7 days, argument == last X days)
    + ExplorerRunCommands    - Recent Explorer "run" commands
      FileInfo               - Information about a file (version information, timestamps, basic PE info, etc. argument(s) == file path(s)
    + FileZilla              - FileZilla configuration files
    + FirefoxHistory         - Parses any found FireFox history files
    + FirefoxPresence        - Checks if interesting Firefox files exist
    + Hotfixes               - Installed hotfixes (via WMI)
      IdleTime               - Returns the number of seconds since the current user's last input.
    + IEFavorites            - Internet Explorer favorites
      IETabs                 - Open Internet Explorer tabs
    + IEUrls                 - Internet Explorer typed URLs (last 7 days, argument == last X days)
    + InstalledProducts      - Installed products via the registry
      InterestingFiles       - "Interesting" files matching various patterns in the user's folder. Note: takes non-trivial time.
    + InterestingProcesses   - "Interesting" processes - defensive products and admin tools
      InternetSettings       - Internet settings including proxy configs and zones configuration
    + KeePass                - Finds KeePass configuration files
    + LAPS                   - LAPS settings, if installed
    + LastShutdown           - Returns the DateTime of the last system shutdown (via the registry).
      LocalGPOs              - Local Group Policy settings applied to the machine/local users
    + LocalGroups            - Non-empty local groups, "-full" displays all groups (argument == computername to enumerate)
    + LocalUsers             - Local users, whether they're active/disabled, and pwd last set (argument == computername to enumerate)
    + LogonEvents            - Logon events (Event ID 4624) from the security event log. Default of 10 days, argument == last X days.
    + LogonSessions          - Windows logon sessions
      LOLBAS                 - Locates Living Off The Land Binaries and Scripts (LOLBAS) on the system. Note: takes non-trivial time.
    + LSASettings            - LSA settings (including auth packages)
    + MappedDrives           - Users' mapped drives (via WMI)
      McAfeeConfigs          - Finds McAfee configuration files
      McAfeeSiteList         - Decrypt any found McAfee SiteList.xml configuration files.
      MicrosoftUpdates       - All Microsoft updates (via COM)
      MTPuTTY                - MTPuTTY configuration files
      NamedPipes             - Named pipe names, any readable ACL information and associated process information.
    + NetworkProfiles        - Windows network profiles
    + NetworkShares          - Network shares exposed by the machine (via WMI)
    + NTLMSettings           - NTLM authentication settings
      OfficeMRUs             - Office most recently used file list (last 7 days)
      OneNote                - List OneNote backup files
    + OptionalFeatures       - List Optional Features/Roles (via WMI)
      OracleSQLDeveloper     - Finds Oracle SQLDeveloper connections.xml files
    + OSInfo                 - Basic OS info (i.e. architecture, OS version, etc.)
    + OutlookDownloads       - List files downloaded by Outlook
    + PoweredOnEvents        - Reboot and sleep schedule based on the System event log EIDs 1, 12, 13, 42, and 6008. Default of 7 days, argument == last X days.
    + PowerShell             - PowerShell versions and security settings
    + PowerShellEvents       - PowerShell script block logs (4104) with sensitive data.
    + PowerShellHistory      - Searches PowerShell console history files for sensitive regex matches.
      Printers               - Installed Printers (via WMI)
    + ProcessCreationEvents  - Process creation logs (4688) with sensitive data.
      Processes              - Running processes with file info company names that don't contain 'Microsoft', "-full" enumerates all processes
    + ProcessOwners          - Running non-session 0 process list with owners. For remote use.
    + PSSessionSettings      - Enumerates PS Session Settings from the registry
    + PuttyHostKeys          - Saved Putty SSH host keys
    + PuttySessions          - Saved Putty configuration (interesting fields) and SSH host keys
      RDCManFiles            - Windows Remote Desktop Connection Manager settings files
    + RDPSavedConnections    - Saved RDP connections stored in the registry
    + RDPSessions            - Current incoming RDP sessions (argument == computername to enumerate)
    + RDPsettings            - Remote Desktop Server/Client Settings
      RecycleBin             - Items in the Recycle Bin deleted in the last 30 days - only works from a user context!
      reg                    - Registry key values (HKLM\Software by default) argument == [Path] [intDepth] [Regex] [boolIgnoreErrors]
      RPCMappedEndpoints     - Current RPC endpoints mapped
    + SCCM                   - System Center Configuration Manager (SCCM) settings, if applicable
    + ScheduledTasks         - Scheduled tasks (via WMI) that aren't authored by 'Microsoft', "-full" dumps all Scheduled tasks
      SearchIndex            - Query results from the Windows Search Index, default term of 'passsword'. (argument(s) == <search path> <pattern1,pattern2,...>
      SecPackageCreds        - Obtains credentials from security packages
    + SecureBoot             - Secure Boot configuration
      SecurityPackages       - Enumerates the security packages currently available using EnumerateSecurityPackagesA()
      Services               - Services with file info company names that don't contain 'Microsoft', "-full" dumps all processes
    + SlackDownloads         - Parses any found 'slack-downloads' files
    + SlackPresence          - Checks if interesting Slack files exist
    + SlackWorkspaces        - Parses any found 'slack-workspaces' files
    + SuperPutty             - SuperPutty configuration files
    + Sysmon                 - Sysmon configuration from the registry
    + SysmonEvents           - Sysmon process creation logs (1) with sensitive data.
      TcpConnections         - Current TCP connections and their associated processes and services
      TokenGroups            - The current token's local and domain groups
      TokenPrivileges        - Currently enabled token privileges (e.g. SeDebugPrivilege/etc.)
    + UAC                    - UAC system policies via the registry
      UdpConnections         - Current UDP connections and associated processes and services
      UserRightAssignments   - Configured User Right Assignments (e.g. SeDenyNetworkLogonRight, SeShutdownPrivilege, etc.) argument == computername to enumerate
      WifiProfile            - Enumerates the saved Wifi profiles and extract the ssid, authentication type, cleartext key/passphrase (when possible)
    + WindowsAutoLogon       - Registry autologon information
      WindowsCredentialFiles - Windows credential DPAPI blobs
    + WindowsDefender        - Windows Defender settings (including exclusion locations)
    + WindowsEventForwarding - Windows Event Forwarding (WEF) settings via the registry
    + WindowsFirewall        - Non-standard firewall rules, "-full" dumps all (arguments == allow/deny/tcp/udp/in/out/domain/private/public)
      WindowsVault           - Credentials saved in the Windows Vault (i.e. logins from Internet Explorer and Edge).
    + WMI                    - Runs a specified WMI query
      WMIEventConsumer       - Lists WMI Event Consumers
      WMIEventFilter         - Lists WMI Event Filters
      WMIFilterBinding       - Lists WMI Filter to Consumer Bindings
    + WSUS                   - Windows Server Update Services (WSUS) settings, if applicable


Seatbelt has the following command groups: All, User, System, Slack, Chromium, Remote, Misc

    You can invoke command groups with         "Seatbelt.exe <group>"


    Or command groups except specific commands "Seatbelt.exe <group> -Command"

   "Seatbelt.exe -group=all" runs all commands

   "Seatbelt.exe -group=user" runs the following commands:

        azuread, Certificates, CertificateThumbprints, ChromiumPresence, CloudCredentials, 
        CloudSyncProviders, CredEnum, dir, DpapiMasterKeys, 
        ExplorerMRUs, ExplorerRunCommands, FileZilla, FirefoxPresence, 
        IdleTime, IEFavorites, IETabs, IEUrls, 
        KeePass, MappedDrives, MTPuTTY, OfficeMRUs, 
        OneNote, OracleSQLDeveloper, PowerShellHistory, PuttyHostKeys, 
        PuttySessions, RDCManFiles, RDPSavedConnections, SecPackageCreds, 
        SlackDownloads, SlackPresence, SlackWorkspaces, SuperPutty, 
        TokenGroups, WindowsCredentialFiles, WindowsVault

   "Seatbelt.exe -group=system" runs the following commands:

        AMSIProviders, AntiVirus, AppLocker, ARPTable, AuditPolicies, 
        AuditPolicyRegistry, AutoRuns, Certificates, CertificateThumbprints, 
        CredGuard, DNSCache, DotNet, EnvironmentPath, 
        EnvironmentVariables, Hotfixes, InterestingProcesses, InternetSettings, 
        LAPS, LastShutdown, LocalGPOs, LocalGroups, 
        LocalUsers, LogonSessions, LSASettings, McAfeeConfigs, 
        NamedPipes, NetworkProfiles, NetworkShares, NTLMSettings, 
        OptionalFeatures, OSInfo, PoweredOnEvents, PowerShell, 
        Processes, PSSessionSettings, RDPSessions, RDPsettings, 
        SCCM, SecureBoot, Services, Sysmon, 
        TcpConnections, TokenPrivileges, UAC, UdpConnections, 
        UserRightAssignments, WifiProfile, WindowsAutoLogon, WindowsDefender, 
        WindowsEventForwarding, WindowsFirewall, WMI, WMIEventConsumer, 
        WMIEventFilter, WMIFilterBinding, WSUS

   "Seatbelt.exe -group=slack" runs the following commands:

        SlackDownloads, SlackPresence, SlackWorkspaces

   "Seatbelt.exe -group=chromium" runs the following commands:

        ChromiumBookmarks, ChromiumHistory, ChromiumPresence

   "Seatbelt.exe -group=remote" runs the following commands:

        AMSIProviders, AntiVirus, AuditPolicyRegistry, ChromiumPresence, CloudCredentials, 
        DNSCache, DotNet, DpapiMasterKeys, EnvironmentVariables, 
        ExplicitLogonEvents, ExplorerRunCommands, FileZilla, Hotfixes, 
        InterestingProcesses, KeePass, LastShutdown, LocalGroups, 
        LocalUsers, LogonEvents, LogonSessions, LSASettings, 
        MappedDrives, NetworkProfiles, NetworkShares, NTLMSettings, 
        OptionalFeatures, OSInfo, PoweredOnEvents, PowerShell, 
        ProcessOwners, PSSessionSettings, PuttyHostKeys, PuttySessions, 
        RDPSavedConnections, RDPSessions, RDPsettings, SecureBoot, 
        Sysmon, WindowsDefender, WindowsEventForwarding, WindowsFirewall
        

   "Seatbelt.exe -group=misc" runs the following commands:

        ChromiumBookmarks, ChromiumHistory, ExplicitLogonEvents, FileInfo, FirefoxHistory, 
        InstalledProducts, InterestingFiles, LogonEvents, LOLBAS, 
        McAfeeSiteList, MicrosoftUpdates, OutlookDownloads, PowerShellEvents, 
        Printers, ProcessCreationEvents, ProcessOwners, RecycleBin, 
        reg, RPCMappedEndpoints, ScheduledTasks, SearchIndex, 
        SecurityPackages, SysmonEvents


Examples:
    'Seatbelt.exe <Command> [Command2] ...' will run one or more specified checks only
    'Seatbelt.exe <Command> -full' will return complete results for a command without any filtering.
    'Seatbelt.exe "<Command> [argument]"' will pass an argument to a command that supports it (note the quotes).
    'Seatbelt.exe -group=all' will run ALL enumeration checks, can be combined with "-full".
    'Seatbelt.exe -group=all -AuditPolicies' will run all enumeration checks EXCEPT AuditPolicies, can be combined with "-full".
    'Seatbelt.exe <Command> -computername=COMPUTER.DOMAIN.COM [-username=DOMAIN\USER -password=PASSWORD]' will run an applicable check remotely
    'Seatbelt.exe -group=remote -computername=COMPUTER.DOMAIN.COM [-username=DOMAIN\USER -password=PASSWORD]' will run remote specific checks
    'Seatbelt.exe -group=system -outputfile="C:\Temp\out.txt"' will run system checks and output to a .txt file.
    'Seatbelt.exe -group=user -q -outputfile="C:\Temp\out.json"' will run in quiet mode with user checks and output to a .json file.
```

**Note:** searches that target users will run for the current user if not-elevated and for ALL users if elevated.


## Command Groups

**Note:** many commands do some type of filtering by default. Supplying the `-full` argument prevents filtering output. Also, the command group `all` will run all current checks.

For example, the following command will run ALL checks and returns ALL output:

`Seatbelt.exe -group=all -full`

### system

Runs checks that mine interesting data about the system.

Executed with: `Seatbelt.exe -group=system`

| Command | Description |
| ----------- | ----------- |
| AMSIProviders | Providers registered for AMSI |
| AntiVirus |  Registered antivirus (via WMI) |
| AppLocker | AppLocker settings, if installed |
| ARPTable | Lists the current ARP table and adapter information(equivalent to arp -a) |
| AuditPolicies | Enumerates classic and advanced audit policy settings |
| AuditPolicyRegistry | Audit settings via the registry |
| AutoRuns | Auto run executables/scripts/programs |
| Certificates | User and machine personal certificate files |
| CertificateThumbprints | Thumbprints for all certificate store certs on the system |
| CredGuard | CredentialGuard configuration |
| DNSCache | DNS cache entries (via WMI) |
| DotNet | DotNet versions |
| EnvironmentPath | Current environment %PATH$ folders and SDDL information |
| EnvironmentVariables | Current user environment variables |
| Hotfixes | Installed hotfixes (via WMI) |
| InterestingProcesses | "Interesting" processes - defensive products and admin tools |
| InternetSettings | Internet settings including proxy configs |
| LAPS | LAPS settings, if installed |
| LastShutdown | Returns the DateTime of the last system shutdown (via the registry) |
| LocalGPOs |  Local Group Policy settings applied to the machine/local users |
| LocalGroups | Non-empty local groups, "full" displays all groups (argument == computername to enumerate) |
| LocalUsers | Local users, whether they're active/disabled, and pwd last set (argument == computername to enumerate) |
| LogonSessions | Logon events (Event ID 4624) from the security event log. Default of 10 days, argument == last X days. |
| LSASettings | LSA settings (including auth packages) |
| McAfeeConfigs | Finds McAfee configuration files |
| NamedPipes | Named pipe names and any readable ACL information |
| NetworkProfiles | Windows network profiles |
| NetworkShares |  Network shares exposed by the machine (via WMI) |
| NTLMSettings | NTLM authentication settings |
| OptionalFeatures | TODO |
| OSInfo | Basic OS info (i.e. architecture, OS version, etc.) |
| PoweredOnEvents | Reboot and sleep schedule based on the System event log EIDs 1, 12, 13, 42, and 6008. Default of 7 days, argument == last X days. |
| PowerShell | PowerShell versions and security settings |
| Processes | Running processes with file info company names that don't contain 'Microsoft', "full" enumerates all processes |
| PSSessionSettings | Enumerates PS Session Settings from the registry |
| RDPSessions | Current incoming RDP sessions (argument == computername to enumerate) |
| RDPsettings | Remote Desktop Server/Client Settings |
| SCCM | System Center Configuration Manager (SCCM) settings, if applicable |
| Services | Services with file info company names that don't contain 'Microsoft', "full" dumps all processes |
| Sysmon | Sysmon configuration from the registry |
| TcpConnections |  Current TCP connections and their associated processes and services |
| TokenPrivileges | Currently enabled token privileges (e.g. SeDebugPrivilege/etc.) |
| UAC | UAC system policies via the registry |
| UdpConnections | Current UDP connections and associated processes and services |
| UserRightAssignments | Configured User Right Assignments (e.g. SeDenyNetworkLogonRight, SeShutdownPrivilege, etc.) argument == computername to enumerate |
| WifiProfile | TODO |
| WindowsAutoLogon | Registry autologon information |
| WindowsDefender | Windows Defender settings (including exclusion locations) |
| WindowsEventForwarding | Windows Event Forwarding (WEF) settings via the registry |
| WindowsFirewall | Non-standard firewall rules, "full" dumps all (arguments == allow/deny/tcp/udp/in/out/domain/private/public) |
| WMIEventConsumer | Lists WMI Event Consumers |
| WMIEventFilter | Lists WMI Event Filters |
| WMIFilterBinding | Lists WMI Filter to Consumer Bindings |
| WSUS | Windows Server Update Services (WSUS) settings, if applicable |


### user

Runs checks that mine interesting data about the currently logged on user (if not elevated) or ALL users (if elevated).

Executed with: `Seatbelt.exe -group=user`

| Command | Description |
| ----------- | ----------- |
| Certificates | User and machine personal certificate files |
| CertificateThumbprints | Thumbprints for all certificate store certs on the system |
| ChromiumPresence | Checks if interesting Chrome/Edge/Brave/Opera files exist |
| CloudCredentials | AWS/Google/Azure cloud credential files |
| CloudSyncProviders | TODO |
| CredEnum | Enumerates the current user's saved credentials using CredEnumerate() |
| dir | Lists files/folders. By default, lists users' downloads, documents, and desktop folders (arguments == \<directory\> \<depth\> \<regex\> |
| DpapiMasterKeys | List DPAPI master keys |
| Dsregcmd | TODO |
| ExplorerMRUs | Explorer most recently used files (last 7 days, argument == last X days) |
| ExplorerRunCommands | Recent Explorer "run" commands |
| FileZilla | FileZilla configuration files |
| FirefoxPresence | Checks if interesting Firefox files exist |
| IdleTime | Returns the number of seconds since the current user's last input. |
| IEFavorites | Internet Explorer favorites |
| IETabs | Open Internet Explorer tabs |
| IEUrls| Internet Explorer typed URLs (last 7 days, argument == last X days) |
| KeePass | TODO |
| MappedDrives | Users' mapped drives (via WMI) |
| OfficeMRUs | Office most recently used file list (last 7 days) |
| OneNote | TODO |
| OracleSQLDeveloper | TODO |
| PowerShellHistory | Iterates through every local user and attempts to read their PowerShell console history if successful will print it  |
| PuttyHostKeys | Saved Putty SSH host keys |
| PuttySessions | Saved Putty configuration (interesting fields) and SSH host keys |
| RDCManFiles | Windows Remote Desktop Connection Manager settings files |
| RDPSavedConnections | Saved RDP connections stored in the registry |
| SecPackageCreds | Obtains credentials from security packages |
| SlackDownloads | Parses any found 'slack-downloads' files |
| SlackPresence | Checks if interesting Slack files exist |
| SlackWorkspaces | Parses any found 'slack-workspaces' files |
| SuperPutty | SuperPutty configuration files |
| TokenGroups | The current token's local and domain groups |
| WindowsCredentialFiles | Windows credential DPAPI blobs |
| WindowsVault | Credentials saved in the Windows Vault (i.e. logins from Internet Explorer and Edge). |


### misc

Runs all miscellaneous checks.

Executed with: `Seatbelt.exe -group=misc`

| Command | Description |
| ----------- | ----------- |
| ChromiumBookmarks | Parses any found Chrome/Edge/Brave/Opera bookmark files |
| ChromiumHistory | Parses any found Chrome/Edge/Brave/Opera history files |
| ExplicitLogonEvents | Explicit Logon events (Event ID 4648) from the security event log. Default of 7 days, argument == last X days. |
| FileInfo | Information about a file (version information, timestamps, basic PE info, etc. argument(s) == file path(s) |
| FirefoxHistory | Parses any found FireFox history files |
| InstalledProducts | Installed products via the registry |
| InterestingFiles | "Interesting" files matching various patterns in the user's folder. Note: takes non-trivial time. |
| LogonEvents | Logon events (Event ID 4624) from the security event log. Default of 10 days, argument == last X days. |
| LOLBAS | Locates Living Off The Land Binaries and Scripts (LOLBAS) on the system. Note: takes non-trivial time. |
| McAfeeSiteList | Decrypt any found McAfee SiteList.xml configuration files. |
| MicrosoftUpdates | All Microsoft updates (via COM) |
| OutlookDownloads | List files downloaded by Outlook |
| PowerShellEvents | PowerShell script block logs (4104) with sensitive data. |
| Printers | Installed Printers (via WMI) |
| ProcessCreationEvents | Process creation logs (4688) with sensitive data. |
| ProcessOwners | Running non-session 0 process list with owners. For remote use. |
| RecycleBin | Items in the Recycle Bin deleted in the last 30 days - only works from a user context! |
| reg | Registry key values (HKLM\Software by default) argument == [Path] [intDepth] [Regex] [boolIgnoreErrors] |
| RPCMappedEndpoints | Current RPC endpoints mapped |
| ScheduledTasks | Scheduled tasks (via WMI) that aren't authored by 'Microsoft', "full" dumps all Scheduled tasks |
| SearchIndex | Query results from the Windows Search Index, default term of 'passsword'. (argument(s) == \<search path\> \<pattern1,pattern2,...\> |
| SecurityPackages | Enumerates the security packages currently available using EnumerateSecurityPackagesA() |
| SysmonEvents | Sysmon process creation logs (1) with sensitive data. |


### Additional Command Groups

Executed with: `Seatbelt.exe -group=GROUPNAME`

| Alias | Description |
| ----------- | ----------- |
| Slack | Runs modules that start with "Slack*" |
| Chromium | Runs modules that start with "Chromium*" |
| Remote | Runs the following modules (for use against a remote system): AMSIProviders, AntiVirus, AuditPolicyRegistry, ChromiumPresence, CloudCredentials, DNSCache, DotNet, DpapiMasterKeys, EnvironmentVariables, ExplicitLogonEvents, ExplorerRunCommands, FileZilla, Hotfixes, InterestingProcesses, KeePass, LastShutdown, LocalGroups, LocalUsers, LogonEvents, LogonSessions, LSASettings, MappedDrives, NetworkProfiles, NetworkShares, NTLMSettings, OptionalFeatures, OSInfo, PoweredOnEvents, PowerShell, ProcessOwners, PSSessionSettings, PuttyHostKeys, PuttySessions, RDPSavedConnections, RDPSessions, RDPsettings, Sysmon, WindowsDefender, WindowsEventForwarding, WindowsFirewall |


## Command Arguments

Command that accept arguments have it noted in their description. To pass an argument to a command, enclose the command an arguments in double quotes.

For example, the following command returns 4624 logon events for the last 30 days:

`Seatbelt.exe "LogonEvents 30"`

The following command queries a registry three levels deep, returning only keys/valueNames/values that match the regex `.*defini.*`, and ignoring any errors that occur.

`Seatbelt.exe "reg \"HKLM\SOFTWARE\Microsoft\Windows Defender\" 3 .*defini.* true"`


## Output

Seatbelt can redirect its output to a file with the `-outputfile="C:\Path\file.txt"` argument. If the file path ends in .json, the output will be structured json.

For example, the following command will output the results of system checks to a txt file:

`Seatbelt.exe -group=system -outputfile="C:\Temp\system.txt"`


## Remote Enumeration

Commands noted with a + in the help menu can be run remotely against another system. This is performed over WMI via queries for WMI classes and WMI's StdRegProv for registry enumeration.

To enumerate a remote system, supply `-computername=COMPUTER.DOMAIN.COM` - an alternate username and password can be specified with `-username=DOMAIN\USER -password=PASSWORD`

For example, the following command runs remote-focused checks against a remote system:

`Seatbelt.exe -group=remote -computername=192.168.230.209 -username=THESHIRE\sam -password="yum \"po-ta-toes\""`


## Building Your Own Modules

Seatbelt's structure is completely modular, allowing for additional command modules to be dropped into the file structure and loaded up dynamically.

There is a commented command module template at `.\Seatbelt\Commands\Template.cs` for reference. Once built, drop the module in the logical file location, include it in the project in the Visual Studio Solution Explorer, and compile.


## Compile Instructions

We are not planning on releasing binaries for Seatbelt, so you will have to compile yourself.

Seatbelt has been built against .NET 3.5 and 4.0 with C# 8.0 features and is compatible with [Visual Studio Community Edition](https://visualstudio.microsoft.com/downloads/). Simply open up the project .sln, choose "release", and build. To change the target .NET framework version, [modify the project's settings](https://github.com/GhostPack/Seatbelt/issues/27) and rebuild the project.


## Acknowledgments

Seatbelt incorporates various collection items, code C# snippets, and bits of PoCs found throughout research for its capabilities. These ideas, snippets, and authors are highlighted in the appropriate locations in the source code, and include:

* [@andrewchiles](https://twitter.com/andrewchiles)' [HostEnum.ps1](https://github.com/threatexpress/red-team-scripts/blob/master/HostEnum.ps1) script and [@tifkin\_](https://twitter.com/tifkin_)'s [Get-HostProfile.ps1](https://github.com/leechristensen/Random/blob/master/PowerShellScripts/Get-HostProfile.ps1) provided inspiration for many of the artifacts to collect.
* [Boboes' code concerning NetLocalGroupGetMembers](https://stackoverflow.com/questions/33935825/pinvoke-netlocalgroupgetmembers-runs-into-fatalexecutionengineerror/33939889#33939889)
* [ambyte's code for converting a mapped drive letter to a network path](https://gist.github.com/ambyte/01664dc7ee576f69042c)
* [Igor Korkhov's code to retrieve current token group information](https://stackoverflow.com/questions/2146153/how-to-get-the-logon-sid-in-c-sharp/2146418#2146418)
* [RobSiklos' snippet to determine if a host is a virtual machine](https://stackoverflow.com/questions/498371/how-to-detect-if-my-application-is-running-in-a-virtual-machine/11145280#11145280)
* [JGU's snippet on file/folder ACL right comparison](https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder/21996345#21996345)
* [Rod Stephens' pattern for recursive file enumeration](http://csharphelper.com/blog/2015/06/find-files-that-match-multiple-patterns-in-c/)
* [SwDevMan81's snippet for enumerating current token privileges](https://stackoverflow.com/questions/4349743/setting-size-of-token-privileges-luid-and-attributes-array-returned-by-gettokeni)
* [Jared Atkinson's PowerShell work on Kerberos ticket caches](https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1)
* [darkmatter08's Kerberos C# snippet](https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/)
* Numerous [PInvoke.net](https://www.pinvoke.net/) samples <3
* [Jared Hill's awesome CodeProject to use Local Security Authority to Enumerate User Sessions](https://www.codeproject.com/Articles/18179/Using-the-Local-Security-Authority-to-Enumerate-Us)
* [Fred's code on querying the ARP cache](https://social.technet.microsoft.com/Forums/lync/en-US/e949b8d6-17ad-4afc-88cd-0019a3ac9df9/powershell-alternative-to-arp-a?forum=ITCG)
* [ShuggyCoUk's snippet on querying the TCP connection table](https://stackoverflow.com/questions/577433/which-pid-listens-on-a-given-port-in-c-sharp/577660#577660)
* [yizhang82's example of using reflection to interact with COM objects through C#](https://gist.github.com/yizhang82/a1268d3ea7295a8a1496e01d60ada816)
* [@djhohnstein](https://twitter.com/djhohnstein)'s [SharpWeb project](https://github.com/djhohnstein/SharpWeb/blob/master/Edge/SharpEdge.cs)
* [@djhohnstein](https://twitter.com/djhohnstein)'s [EventLogParser project](https://github.com/djhohnstein/EventLogParser)
* [@cmaddalena](https://twitter.com/cmaddalena)'s [SharpCloud project](https://github.com/chrismaddalena/SharpCloud), BSD 3-Clause
* [@_RastaMouse](https://twitter.com/_RastaMouse)'s [Watson project](https://github.com/rasta-mouse/Watson/), GPL License
* [@_RastaMouse](https://twitter.com/_RastaMouse)'s [Work on AppLocker enumeration](https://rastamouse.me/2018/09/enumerating-applocker-config/)
* [@peewpw](https://twitter.com/peewpw)'s [Invoke-WCMDump project](https://github.com/peewpw/Invoke-WCMDump/blob/master/Invoke-WCMDump.ps1), GPL License
* TrustedSec's [HoneyBadger project](https://github.com/trustedsec/HoneyBadger/tree/master/modules/post/windows/gather), BSD 3-Clause
* CENTRAL Solutions's [Audit User Rights Assignment Project](https://www.centrel-solutions.com/support/tools.aspx?feature=auditrights), No license
* Collection ideas inspired from [@ukstufus](https://twitter.com/ukstufus)'s [Reconerator](https://github.com/stufus/reconerator)
* Office MRU locations and timestamp parsing information from Dustin Hurlbut's paper [Microsoft Office 2007, 2010 - Registry Artifacts](https://ad-pdf.s3.amazonaws.com/Microsoft_Office_2007-2010_Registry_ArtifactsFINAL.pdf)
* The [Windows Commands list](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/windows-commands), used for sensitive regex construction
* [Ryan Ries' code for enumeration mapped RPC endpoints](https://stackoverflow.com/questions/21805038/how-do-i-pinvoke-rpcmgmtepeltinqnext)
* [Chris Haas' post on EnumerateSecurityPackages()](https://stackoverflow.com/a/5941873)
* [darkoperator](carlos_perez)'s work [on the HoneyBadger project](https://github.com/trustedsec/HoneyBadger)
* [@airzero24](https://twitter.com/airzero24)'s work on [WMI Registry enumeration](https://github.com/airzero24/WMIReg)
* Alexandru's answer on [RegistryKey.OpenBaseKey alternatives](https://stackoverflow.com/questions/26217199/what-are-some-alternatives-to-registrykey-openbasekey-in-net-3-5)
* Tomas Vera's [post on JavaScriptSerializer](http://www.tomasvera.com/programming/using-javascriptserializer-to-parse-json-objects/)
* Marc Gravell's [note on recursively listing files/folders](https://stackoverflow.com/a/929418)
* [@mattifestation](https://twitter.com/mattifestation)'s [Sysmon rule parser](https://github.com/mattifestation/PSSysmonTools/blob/master/PSSysmonTools/Code/SysmonRuleParser.ps1#L589-L595)
* Some inspiration from spolnik's [Simple.CredentialsManager project](https://github.com/spolnik/Simple.CredentialsManager), Apache 2 license
* [This post on Credential Guard settings](https://www.tenforums.com/tutorials/68926-verify-if-device-guard-enabled-disabled-windows-10-a.html)
* [This thread](https://social.technet.microsoft.com/Forums/windows/en-US/b0e13a16-51a6-4aca-8d44-c85e097f882b/nametype-in-nla-information-for-a-network-profile) on network profile information
* Mark McKinnon's post on [decoding the DateCreated and DateLastConnected SSID values](http://cfed-ttf.blogspot.com/2009/08/decoding-datecreated-and.html)
* This Specops [post on group policy caching](https://specopssoft.com/blog/things-work-group-policy-caching/)
* sa_ddam213's StackOverflow post on [enumerating items in the Recycle Bin](https://stackoverflow.com/questions/18071412/list-filenames-in-the-recyclebin-with-c-sharp-without-using-any-external-files)
* Kirill Osenkov's [code for managed assembly detection](https://stackoverflow.com/a/15608028)
* The [Mono project](https://github.com/mono/linux-packaging-mono/blob/d356d2b7db91d62b80a61eeb6fbc70a402ac3cac/external/corefx/LICENSE.TXT) for the SecBuffer/SecBufferDesc classes
* [Elad Shamir](https://twitter.com/elad_shamir) and his [Internal-Monologue](https://github.com/eladshamir/Internal-Monologue/) project, [Vincent Le Toux](https://twitter.com/mysmartlogon) for his [DetectPasswordViaNTLMInFlow](https://github.com/vletoux/DetectPasswordViaNTLMInFlow/) project, and Lee Christensen for this [GetNTLMChallenge](https://github.com/leechristensen/GetNTLMChallenge/) project. All of these served as inspiration int he SecPackageCreds command.
* @leftp and @eksperience's [Gopher project](https://github.com/EncodeGroup/Gopher) for inspiration for the FileZilla and SuperPutty commands
* @funoverip for the original McAfee SiteList.xml decryption code

We've tried to do our due diligence for citations, but if we've left someone/something out, please let us know!
