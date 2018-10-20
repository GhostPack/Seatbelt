using System;

namespace Seatbelt.WindowsInterop
{
    public enum ArpEntryType
    {
        Other = 1,
        Invalid = 2,
        Dynamic = 3,
        Static = 4,
    }

    public enum TokenInformation
    {
        TokenUser = 1,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin
    }
    
    [Flags]
    public enum FirewallProfiles : int
    {
        DOMAIN = 1,
        PRIVATE = 2,
        PUBLIC = 4,
        ALL = 2147483647
    }

    [Flags]
    public enum LuidAttributes : uint
    {
        DISABLED = 0x00000000,
        SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001,
        SE_PRIVILEGE_ENABLED = 0x00000002,
        SE_PRIVILEGE_REMOVED = 0x00000004,
        SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000
    }

    public enum SID_NAME_USE
    {
        SidTypeUser = 1,
        SidTypeGroup,
        SidTypeDomain,
        SidTypeAlias,
        SidTypeWellKnownGroup,
        SidTypeDeletedAccount,
        SidTypeInvalid,
        SidTypeUnknown,
        SidTypeComputer
    }
    
    public enum WTS_CONNECTSTATE_CLASS
    {
        Active,
        Connected,
        ConnectQuery,
        Shadow,
        Disconnected,
        Idle,
        Listen,
        Reset,
        Down,
        Init
    }

    public enum WTS_INFO_CLASS
    {
        WTSInitialProgram = 0,
        WTSApplicationName = 1,
        WTSWorkingDirectory = 2,
        WTSOEMId = 3,
        WTSSessionId = 4,
        WTSUserName = 5,
        WTSWinStationName = 6,
        WTSDomainName = 7,
        WTSConnectState = 8,
        WTSClientBuildNumber = 9,
        WTSClientName = 10,
        WTSClientDirectory = 11,
        WTSClientProductId = 12,
        WTSClientHardwareId = 13,
        WTSClientAddress = 14,
        WTSClientDisplay = 15,
        WTSClientProtocolType = 16,
        WTSIdleTime = 17,
        WTSLogonTime = 18,
        WTSIncomingBytes = 19,
        WTSOutgoingBytes = 20,
        WTSIncomingFrames = 21,
        WTSOutgoingFrames = 22,
        WTSClientInfo = 23,
        WTSSessionInfo = 24,
        WTSSessionInfoEx = 25,
        WTSConfigInfo = 26,
        WTSValidationInfo = 27,
        WTSSessionAddressV4 = 28,
        WTSIsRemoteSession = 29
    }

    public enum TCP_TABLE_CLASS : int
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    public enum UDP_TABLE_CLASS : int
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    public enum ScServiceTagQueryType
    {
        ServiceNameFromTagInformation = 1,
        ServiceNamesReferencingModuleInformation = 2,
        ServiceNameTagMappingInformation = 3
    }

    public enum MIB_TCP_STATE
    {
        CLOSED = 1,
        LISTEN = 2,
        SYN_SENT = 3,
        SYN_RCVD = 4,
        ESTAB = 5,
        FIN_WAIT1 = 6,
        FIN_WAIT2 = 7,
        CLOSE_WAIT = 8,
        CLOSING = 9,
        LAST_ACK = 10,
        TIME_WAIT = 11,
        DELETE_TCB = 12
    }
    
    public enum KERB_PROTOCOL_MESSAGE_TYPE : UInt32
    {
        KerbDebugRequestMessage = 0,
        KerbQueryTicketCacheMessage = 1,
        KerbChangeMachinePasswordMessage = 2,
        KerbVerifyPacMessage = 3,
        KerbRetrieveTicketMessage = 4,
        KerbUpdateAddressesMessage = 5,
        KerbPurgeTicketCacheMessage = 6,
        KerbChangePasswordMessage = 7,
        KerbRetrieveEncodedTicketMessage = 8,
        KerbDecryptDataMessage = 9,
        KerbAddBindingCacheEntryMessage = 10,
        KerbSetPasswordMessage = 11,
        KerbSetPasswordExMessage = 12,
        KerbVerifyCredentialsMessage = 13,
        KerbQueryTicketCacheExMessage = 14,
        KerbPurgeTicketCacheExMessage = 15,
        KerbRefreshSmartcardCredentialsMessage = 16,
        KerbAddExtraCredentialsMessage = 17,
        KerbQuerySupplementalCredentialsMessage = 18,
        KerbTransferCredentialsMessage = 19,
        KerbQueryTicketCacheEx2Message = 20,
        KerbSubmitTicketMessage = 21,
        KerbAddExtraCredentialsExMessage = 22,
        KerbQueryKdcProxyCacheMessage = 23,
        KerbPurgeKdcProxyCacheMessage = 24,
        KerbQueryTicketCacheEx3Message = 25,
        KerbCleanupMachinePkinitCredsMessage = 26,
        KerbAddBindingCacheEntryExMessage = 27,
        KerbQueryBindingCacheMessage = 28,
        KerbPurgeBindingCacheMessage = 29,
        KerbQueryDomainExtendedPoliciesMessage = 30,
        KerbQueryS4U2ProxyCacheMessage = 31
    }

    public enum KERB_ENCRYPTION_TYPE : UInt32
    {
        reserved0 = 0,
        des_cbc_crc = 1,
        des_cbc_md4 = 2,
        des_cbc_md5 = 3,
        reserved1 = 4,
        des3_cbc_md5 = 5,
        reserved2 = 6,
        des3_cbc_sha1 = 7,
        dsaWithSHA1_CmsOID = 9,
        md5WithRSAEncryption_CmsOID = 10,
        sha1WithRSAEncryption_CmsOID = 11,
        rc2CBC_EnvOID = 12,
        rsaEncryption_EnvOID = 13,
        rsaES_OAEP_ENV_OID = 14,
        des_ede3_cbc_Env_OID = 15,
        des3_cbc_sha1_kd = 16,
        aes128_cts_hmac_sha1_96 = 17,
        aes256_cts_hmac_sha1_96 = 18,
        aes128_cts_hmac_sha256_128 = 19,
        aes256_cts_hmac_sha384_192 = 20,
        rc4_hmac = 23,
        rc4_hmac_exp = 24,
        camellia128_cts_cmac = 25,
        camellia256_cts_cmac = 26,
        subkey_keymaterial = 65
    }

    [Flags]
    public enum KERB_CACHE_OPTIONS : UInt64
    {
        KERB_RETRIEVE_TICKET_DEFAULT = 0x0,
        KERB_RETRIEVE_TICKET_DONT_USE_CACHE = 0x1,
        KERB_RETRIEVE_TICKET_USE_CACHE_ONLY = 0x2,
        KERB_RETRIEVE_TICKET_USE_CREDHANDLE = 0x4,
        KERB_RETRIEVE_TICKET_AS_KERB_CRED = 0x8,
        KERB_RETRIEVE_TICKET_WITH_SEC_CRED = 0x10,
        KERB_RETRIEVE_TICKET_CACHE_TICKET = 0x20,
        KERB_RETRIEVE_TICKET_MAX_LIFETIME = 0x40,
    }

    // TODO: double check these flags...
    // https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_external_ticket
    [Flags]
    public enum KERB_TICKET_FLAGS : UInt32
    {
        reserved = 2147483648,
        forwardable = 0x40000000,
        forwarded = 0x20000000,
        proxiable = 0x10000000,
        proxy = 0x08000000,
        may_postdate = 0x04000000,
        postdated = 0x02000000,
        invalid = 0x01000000,
        renewable = 0x00800000,
        initial = 0x00400000,
        pre_authent = 0x00200000,
        hw_authent = 0x00100000,
        ok_as_delegate = 0x00040000,
        name_canonicalize = 0x00010000,
        //cname_in_pa_data = 0x00040000,
        enc_pa_rep = 0x00010000,
        reserved1 = 0x00000001
    }

    public enum SECURITY_LOGON_TYPE : uint
    {
        Interactive = 2,        // logging on interactively.
        Network,                // logging using a network.
        Batch,                  // logon for a batch process.
        Service,                // logon for a service account.
        Proxy,                  // Not supported.
        Unlock,                 // Tattempt to unlock a workstation.
        NetworkCleartext,       // network logon with cleartext credentials
        NewCredentials,         // caller can clone its current token and specify new credentials for outbound connections
        RemoteInteractive,      // terminal server session that is both remote and interactive
        CachedInteractive,      // attempt to use the cached credentials without going out across the network
        CachedRemoteInteractive,// same as RemoteInteractive, except used internally for auditing purposes
        CachedUnlock            // attempt to unlock a workstation
    }

}
