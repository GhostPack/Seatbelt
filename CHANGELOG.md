# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1] - 2022-07-11

### Added

* Status message in the "LSASettings" module when TokenLeakDetectDelaySecs is set
 

## [1.2.0] - 2022-05-18

### Added

* Added the following commands:
    * SecureBoot - Secure Boot configuration
    * OneNote - List cached OneNote files (@tijme)
    * WifiProfile - WIFI profile enumeration including passwords if possible (@guervild)
    * WMI - Runs a specified WMI query
    * OptionalFeatures - List Optional Features/Roles (via WMI)
    * CertificateThumbprints - Finds thumbprints for all certificate store certs on the system
    * Certificates - Finds user and machine certificate files
    * KeePass - Finds KeePass configuration files
    * Dsregcmd - Tenant information - replacement for Dsregcmd /status (@leftp)
    * CloudSyncProvider - Configured Office 365 endpoints (tenants and teamsites) which are synchronised by OneDrive (@stufus)
    * OracleSQLDeveloper - Finds Oracle SQLDeveloper connections.xml files
* Added "type" to NetworkShares command
* Added ability to specify regex for PowerShell scraping commands
* Added ability to specify regex for ProcessCreationEvents and SysmonEvents commands
* Added support for getting directory information through the fileinfo command
* Additional error handling in various places
* Added hardware ID + client directory to RDPSessions
* Added ParentProcessID to Processes command
* (Optional) Command versioning
* Added filtering to Services command
* Added ability to exclude specific commands from command groups (@mgeeky)
* Process information (if possible) to NamedPipes command (@Wra7h)

### Changed

* @dtmsecurity's rdpsession improvements
* Upgraded to C# 9 support
* ARPTable refactor
* Misc cleanup/refactoring
* Updated license

### Fixed

* Corrected support for PowerShell Core versions in associated commands
* Corrected MemoryStream output method
* Split sysmon output across several lines
* Removed console output from arp command
* PSSessionSettings bugs
* Vault pointer size fix
* Office version handling
* More nullable type issues
* Fix for ChromeHistory if file is opened already


## [1.1.1] - 2020-11-06

### Added

* Added remote support to the following commands:
    * PowerShell, DotNet
    * FirefoxPresence, FirefoxHistory
    * ChromePresence/ChromeHistory/ChromeBookmarks
    * InternetExplorerFavorites, IEUrls
    * SlackDownloads, SlackPresence, SlackWorkspaces
    * CloudCredentials, FileZilla, OutlookDownloads, RDCManFiles
    * SuperPutty, LocalUsers, LocalGroups, PowerShellHistory
    * Credguard, InstalledProducts, AppLocker, AuditPolicyRegistry
    * DNSCache, PSSessionSettings, OSInfo, EnvironmentVariables, DpapiMasterKeys

* Implemented remote event log support:
    * ExplicitLogonEvents, LogonEvents, PoweredOnEvents, PowerShellEvents, ProcessCreationEvents, SysmonEvents

* Chrome* modules now converted to Chromium support:
    * Chrome, Edge, Brave, Opera

* Added IBM Bluemix enumeration to CloudCredentials


### Fixed

* Better error handling in various modules
* OS version number collection on Windows 10
* McAfeeSiteList null pointer exception
* Interpretation of uac/tokenfilter/filteradmintoken values
* Nullable type issues
* WindowsFirewall filtering


## [1.1.0] - 2020-09-30

### Added

* Added the following commands:
    * Hotfixes - installed hotfixes (via WMI)
    * MicrosoftUpdates - all Microsoft updates (via COM)
    * HuntLolbas - hunt for living-off-the-land binaries (from @NotoriousRebel)
    * PowerShellHistory - searches PowerShell console history files for sensitive regex matches (adapted from @NotoriousRebel)
    * RDPSettings - Remote Desktop Server/Client Settings
    * SecPackageCreds - obtains credentials from security packages (InternalMonologue for the current user)
    * FileZilla - files user FileZilla configuration files/passwords
    * SuperPutty - files user SuperPutty configuration files
    * McAfeeSiteList - finds/decrypts McAfee SiteList.xml files
    * McAfeeConfigs- finds McAfee configuration files

### Changed

* Added CLR version enumeration to "DotNet" and "PowerShell" commands
* Updated LSASettings to detect restricted admin mode
* Added ZoneMapKey & Auth settings to "InternetSettings" (Francis Lacoste)
* Added support for ByteArrays in "WindowsVault"
* Redid assembly detection to (hopefully) prevent image load events
* Added version/description fields to processes and services
* Added ASR rules to "WindowsDefender" command

### Fixed

* Big fix for event log searching
* Fix for sensitive command line scraping
* Code cleanup/dead code removal
* Allow empty companyname the Services command
* Better exception handling
* Various fixes/expansions for the "WindowsVault" command
* Added disposing of output sinks
* Other misc. bug fixes


## [1.0.0] - 2020-05-26

### Added

* Added the following commands:
    * NTLMSettings, SCCM, WSUS, UserRightAssignments, IdleTime, FileInfo, NamedPipes, NetworkProfile
    * AMSIProviders, RPCMappedEndpoints, LocalUsers, CredGuard, LocalGPOs, OutlookDownloads
    * AppLocker (thanks @_RastaMouse! https://github.com/GhostPack/Seatbelt/pull/15)
    * InstalledProducts and Printers commands, with DACLs included for printers
    * SearchIndex - module to search the Windows Search Indexer
    * WMIEventFilter/WMIEventConsumer/WMIEventConsumer commands
    * ScheduledTasks command (via WMI for win8+)
    * AuditPolicies/AuditSettings - classic and advanced audit policy settings
    * EnvironmentPath - %ENV:PATH% folder enumeration, along with DACLs
    * ProcessCreation - from @djhohnstein's EventLogParser project. Expanded sensitive regexes.
    * CredEnum - use CredEnumerate() to enumerate the credentials from the user's credential set (thanks @djhohnstein and @peewpw)
    * SecurityPackages - uses EnumerateSecurityPackages() to enumerate available security packages
    * WindowsDefender - exclusions for paths/extensions/processes for Windows Defender
    * DotNet - detects .NET versions and whether AMSI is enabled/can by bypassed (similar to 'PowerShell')
    * ProcessOwners - simplified enumeration of non-session 0 processes/owners that can function remotely
    * dir
        * Allows recursively enumerating directories and searching for files based on a regex
        * Lists user folders by default
        * Usage:   "dir [path] [depth] [searchRegex] [ignoreErrors? true/false]"
        * Default: "dir C:\users\ 2 \\(Documents|Downloads|Desktop) false"
            * Shows files in users' documents/downloads/desktop folders 
    * reg
        * Allows recursively listing and searching for registry values on the current machine and remotely (if remote registry is enabled).
    * Added additional defensive process checks thanks to @swarleysez, @Ne0nd0g, and @leechristensen. See https://github.com/GhostPack/Seatbelt/pull/17 and https://github.com/GhostPack/Seatbelt/pull/19.
    * Added Xen virtual machine detections thanks to @rasta-mouse. See https://github.com/GhostPack/Seatbelt/pull/18
* Added the following command aliases:
    * "Remote" for common commands to run remotely
    * "Slack" to run Slack-specific modules
    * "Chrome" to run Chrome-specific modules
* Added in ability to give commands arguments (to be expanded in the future). Syntax: `Seatbelt.exe "PoweredOnEvents 30"`
* Added remote support for WMI/registry enumeration modules that are marked with a +
    * Usage: computername=COMPUTER.DOMAIN.COM [username=DOMAIN\USER password=PASSWORD]
* Added the "-q" command-line flag to not print the logo
* Added ability to output to a file with the the "-o <file>" parameter
    * Providing a file that ends in .json produces JSON-structured output!
* Added in the architecture for different output sinks. Still need to convert a lot of cmdlets to the new format.
* Added a module template.
* Added CHANGELOG.md.


### Changed

* Externalized all commands into their own class/file
* Cleaned up some of the registry querying code
* Commands can now be case-insensitive
* Seatbelt's help message is now dynamically created
* Renamed RebootSchedule to PoweredOnEvents
    * Now enumerates events for system startup/shutdown, unexpected shutdown, and sleeping/awaking.
* Modified the output of the Logon and ExplicitLogon event commands to be easier to read/analyze
* LogonEvents, ExplicitLogonEvents, and PoweredOnEvents take an argument of how many days back to collect logs for. Example: Seatbelt.exe "LogonEvents 50"
* Added Added timezone, locale information, MachineGuid, Build number and UBR (if present) to OSInfo command
* Refactored registry enumeration code
* Putty command now lists if agent forwarding is enabled
* Renamed BasicOSInfo to OSInfo
* Simplified IsLocalAdmin code
* Added the member type to localgroupmembership output
* Simplified the RDPSavedConnections code
* Formatted the output of RDPSavedConnections to be prettier
* Formatted the output of RecentFiles to be prettier
* Modified logonevents default so that it only outputs the past day on servers
* Re-wrote the PowerShell command. Added AMSI information and hints for bypassing.
* Add NTLM/Kerberos informational alerts to the LogonEvents command
* Changed the output format of DpapiMasterKeys
* Re-wrote the Registry helper code
* Refactored the helper code
* Incorprated [@mark-s's](https://github.com/mark-s) code to speed up the interestingfiles command. See [#16](https://github.com/GhostPack/Seatbelt/pull/16)
* Added SDDL to the "fileinfo" command
* Added MRUs for all office applications to the RecentFiles command
* RecentFiles now has a paramater that restricts how old the documents are. "RecentFiles 20" - Shows files accessed in the last 20 days.
* Renamed RegistryValue command to "reg"
* Search terms in the "reg" command now match keys, value names, and values.
* Updated the "reg" commands arguments.
    * Usage: "reg <HIVE[\PATH\TO\KEY]> [depth] [searchTerm] [ignoreErrors]"
    * Defaults: "reg HKLM\Software 1 default true"
* Added generic GetSecurityInfos command into SecurityUtil
* Formatting tweak for DPAPIMasterkeys
* WindowsVaults output filtering
* Renamed RecentFiles to ExplorerMRUs, broke out functionality for ExplorerMRUs and OfficeMRUs
* Broke IETriage command into IEUrls and IEFavorites
* Changed FirefoxCommand to FirefoxHistory
* Changed ChromePresence and FirefoxPresence to display last modified timestamps for the history/cred/etc. files
* Split ChromeCommand into ChromeHistoryCommand and ChromeBookmarksCommand
* Broke PuttyCommand into PuttyHostKeys and PuttySessions
* Added SDDL field to InterestingFiles command
* Modified IdleTime to display the current user and time in h:m:s:ms format
* Moved Firewall enumeration to the registry (instead of the COM object). Thanks @Max_68!
* Changed TokenGroups output formatting
* Renamed localgroupmemberships to localgroups
* Changed network firewall enumeration to display "non-builtin" rules instead of deny. Added basic filtering.
* Added IsDotNet property to the FileInfo command
* Renamed "NonstandardProcesses" and "NonstandardServices" to "Processes" and "Services", respectively
* LocalGroups now enumerates all (by default non-empty) local groups and memberships, along with comments
* Added a "modules" argument to the "Processes" command to display non-Microsoft loaded processes
* Notify operator when LSA Protected Mode is enabled (RunAsPPL)
* Updated the EnvironmentVariables command to distinguish between user/system/current process/volatile variables
* Added a user filter to ExplicitLogonEvents. Usage: `ExplicitLogonEvents <days> <targetUserRegex>`
* Added version check for Chrome (v80+)
* Added analysis messages for the logonevents command
* Rewrote and expanded README.md


### Fixed

* Some timestamp converting code in the ticket extraction section
* Fixed Chrome bookmark command (threw an exception with folders)
* Fixed reboot schedule (xpath query wasn't precise enough, leading to exceptions)
* Fixed an exception that was being thrown in the CloudCredential command
* NonstandardServices command
    * Fixed a bug that occurred during enumeration
    * Added ServiceDll and User fields
    * Partially fixed path parsing in NonstandardServices with some help from OJ (@TheColonial)! See https://github.com/GhostPack/Seatbelt/pull/14
    * Cleaned up the code
* Fixed a bug in localgroupmembership
* Check if it's a Server before running the AntiVirus check (the WMI class isn't on servers)
* Fixed a bug in WindowsCredentialFiles so it wouldn't output null bytes
* Fixed a null reference bug in the PowerShell command
* Fixed the OS version comparisons in WindowsVault command
* Fixed a DWORD parsing bug in the registry util class for big (i.e. negative int) values
* ARPTable bug fix/error handling
* Fixed PuttySession HKCU v. HKU bug
* Fixed a terminating exception bug in the Processes command when obtaining file version info
* More additional bug fixes than we can count >_<


### Removed

* Removed the UserFolder command (replaced by DirectoryList command)


## [0.2.0] - 2018-08-20

### Added
* @djhohnstein's vault enumeration


### Changed
* @ClementNotin/@cnotin's various fixes


## [0.1.0] - 2018-07-24

* Initial release
