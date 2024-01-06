using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using static Seatbelt.Interop.Netapi32;

namespace Seatbelt.Commands.Windows
{
    internal class AzureADCommand : CommandBase
    {
        public override string Command => "azuread";
        public override string Description => "Return AzureAD info";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;
        public Runtime ThisRunTime;

        public AzureADCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            bool? sssoDomainTrusted = null;
            var sssoDomainTrustedValue = ThisRunTime.GetDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains\microsoftazuread-sso.com\autologon", "https");

            if (sssoDomainTrustedValue != null)
            {
                switch (sssoDomainTrustedValue)
                {
                    case 0:
                        sssoDomainTrusted = false;
                        break;
                    case 1:
                        sssoDomainTrusted = true;
                        break;
                    default:
                        sssoDomainTrusted = false;
                        break;
                }
            }

            var netAadJoinInfo = GetNetAadInfo();


            yield return new AzureADDTO(
                netAadJoinInfo,
                sssoDomainTrusted
               );
        }


        private NetAadJoinInfo? GetNetAadInfo()
        {
            //original code from https://github.com/ThomasKur/WPNinjas.Dsregcmd/blob/2cff7b273ad4d3fc705744f76c4bd0701b2c36f0/WPNinjas.Dsregcmd/DsRegCmd.cs

            var tenantId = "";
            var retValue = NetGetAadJoinInformation(tenantId, out var ptrJoinInfo);
            if (retValue == 0)
            {
                var joinInfo = (DSREG_JOIN_INFO)Marshal.PtrToStructure(ptrJoinInfo, typeof(DSREG_JOIN_INFO));
                var JType = (NetAadJoinInfo.JoinType)joinInfo.joinType;
                var did = new Guid(joinInfo.DeviceId);
                var tid = new Guid(joinInfo.TenantId);

                var data = Convert.FromBase64String(joinInfo.UserSettingSyncUrl);
                var UserSettingSyncUrl = Encoding.ASCII.GetString(data);
                var ptrUserInfo = joinInfo.pUserInfo;

                DSREG_USER_INFO? userInfo = null;
                var cresult = new List<X509Certificate2>();
                Guid? uid = null;

                if (ptrUserInfo != IntPtr.Zero)
                {
                    userInfo = (DSREG_USER_INFO)Marshal.PtrToStructure(ptrUserInfo, typeof(DSREG_USER_INFO));
                    uid = new Guid(userInfo?.UserKeyId);
                    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);

                    foreach (var certificate in store.Certificates)
                    {
                        if (certificate.Subject.Equals($"CN={did}"))
                        {
                            cresult.Add(certificate);
                        }
                    }

                    Marshal.Release(ptrUserInfo);
                }

                Marshal.Release(ptrJoinInfo);
                NetFreeAadJoinInformation(ptrJoinInfo);

                return new NetAadJoinInfo(
                    JType,
                    did,
                    joinInfo.IdpDomain,
                    tid,
                    joinInfo.JoinUserEmail,
                    joinInfo.TenantDisplayName,
                    joinInfo.MdmEnrollmentUrl,
                    joinInfo.MdmTermsOfUseUrl,
                    joinInfo.MdmComplianceUrl,
                    UserSettingSyncUrl,
                    cresult,
                    userInfo?.UserEmail,
                    uid,
                    userInfo?.UserKeyName
                );
            }

            return null;
        }
    }

    internal class AzureADDTO : CommandDTOBase
    {
        public AzureADDTO(NetAadJoinInfo? netAadJoinInfo, bool? seamlessSignOnDomainTrusted)
        {
            SeamlessSignOnDomainTrusted = seamlessSignOnDomainTrusted;
            NetAadJoinInfo = netAadJoinInfo;
        }

        public bool? SeamlessSignOnDomainTrusted { get; }
        public NetAadJoinInfo? NetAadJoinInfo { get;  }
    }

    internal class NetAadJoinInfo
    {
        public NetAadJoinInfo(JoinType jType, Guid deviceId, string idpDomain, Guid tenantId, string joinUserEmail, string tenantDisplayName, string mdmEnrollmentUrl, string mdmTermsOfUseUrl,
               string mdmComplianceUrl, string userSettingSyncUrl, List<X509Certificate2> certInfo, string? userEmail, Guid? userKeyId, string? userKeyname)
        {
            JType = jType;
            DeviceId = deviceId;
            IdpDomain = idpDomain;
            TenantId = tenantId;
            JoinUserEmail = joinUserEmail;
            TenantDisplayName = tenantDisplayName;
            MdmEnrollmentUrl = mdmEnrollmentUrl;
            MdmTermsOfUseUrl = mdmTermsOfUseUrl;
            MdmComplianceUrl = mdmComplianceUrl;
            UserSettingSyncUrl = userSettingSyncUrl;
            CertInfo = certInfo;
            UserEmail = userEmail;
            UserKeyId = userKeyId;
            UserKeyname = userKeyname;
        }
        public enum JoinType
        {
            DSREG_UNKNOWN_JOIN,
            DSREG_DEVICE_JOIN,
            DSREG_WORKPLACE_JOIN,
            DSREG_NO_JOIN
        }
        public JoinType JType { get; }
        public Guid DeviceId { get; }
        public string IdpDomain { get; }
        public Guid TenantId { get; }
        public string JoinUserEmail { get; }
        public string TenantDisplayName { get; }
        public string MdmEnrollmentUrl { get; }
        public string MdmTermsOfUseUrl { get; }
        public string MdmComplianceUrl { get; }
        public string UserSettingSyncUrl { get; }
        public List<X509Certificate2> CertInfo { get; }
        public string? UserEmail { get; }
        public Guid? UserKeyId { get; }
        public string? UserKeyname { get; }
    }

    [CommandOutputType(typeof(AzureADDTO))]
    internal class AzureADFormatter : TextFormatterBase
    {
        public AzureADFormatter(ITextWriter writer) : base(writer)
        {
            // nothing goes here
        }
        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (AzureADDTO)result;
            var netAadInfo = dto.NetAadJoinInfo;

            if (netAadInfo == null)
                WriteLine("    Could not enumerate NetAadJoinInfo");
            else
            {
                WriteLine($"    TenantDisplayName           : {netAadInfo.TenantDisplayName}");
                WriteLine($"    TenantId                    : {netAadInfo.TenantId}");
                WriteLine($"    IdpDomain                   : {netAadInfo.IdpDomain}");
                WriteLine($"    MdmEnrollmentUrl            : {netAadInfo.MdmEnrollmentUrl}");
                WriteLine($"    MdmTermsOfUseUrl            : {netAadInfo.MdmTermsOfUseUrl}");
                WriteLine($"    MdmComplianceUrl            : {netAadInfo.MdmComplianceUrl}");
                WriteLine($"    UserSettingSyncUrl          : {netAadInfo.UserSettingSyncUrl}");
                WriteLine($"    DeviceId                    : {netAadInfo.DeviceId}");
                WriteLine($"    JoinType                    : {netAadInfo.JType}");
                WriteLine($"    JoinUserEmail               : {netAadInfo.JoinUserEmail}");
                WriteLine($"    UserKeyId                   : {netAadInfo.UserKeyId}");
                WriteLine($"    UserEmail                   : {netAadInfo.UserEmail}");
                WriteLine($"    UserKeyname                 : {netAadInfo.UserKeyname}\n");

                foreach (var cert in netAadInfo.CertInfo)
                {
                    WriteLine($"    Thumbprint  : {cert.Thumbprint}");
                    WriteLine($"    Subject     : {cert.Subject}");
                    WriteLine($"    Issuer      : {cert.Issuer}");
                    WriteLine($"    Expiration  : {cert.GetExpirationDateString()}");
                }
            }

            var sssoMsg = "";
            if (dto.SeamlessSignOnDomainTrusted == true)
                sssoMsg = "(AzureAD Seamless Sign-on may be enabled!)";
            else
                sssoMsg = "(not configured)";

            WriteLine($"    SeamlessSignOnDomainTrusted : {dto.SeamlessSignOnDomainTrusted}{sssoMsg}");
        }
    }
}
