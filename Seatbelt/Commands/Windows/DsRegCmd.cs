using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using static Seatbelt.Interop.Netapi32;

namespace Seatbelt.Commands.Windows
{
    internal class DSregcmdCommand : CommandBase
    {
        public override string Command => "Dsregcmd";
        public override string Description => "Return Tenant information - Replacement for Dsregcmd /status";
        public override CommandGroup[] Group => new[] { CommandGroup.User };              // either CommandGroup.System, CommandGroup.User, or CommandGroup.Misc
        public override bool SupportRemote => false;                             // set to true if you want to signal that your module supports remote operations

        public DSregcmdCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            //original code from https://github.com/ThomasKur/WPNinjas.Dsregcmd/blob/2cff7b273ad4d3fc705744f76c4bd0701b2c36f0/WPNinjas.Dsregcmd/DsRegCmd.cs

            var tenantId = "";
            var retValue = NetGetAadJoinInformation(tenantId, out var ptrJoinInfo);
            if (retValue == 0)
            {
                var joinInfo = (DSREG_JOIN_INFO)Marshal.PtrToStructure(ptrJoinInfo, typeof(DSREG_JOIN_INFO));
                var JType = (DSregcmdDTO.JoinType)joinInfo.joinType;
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
                    userInfo = (DSREG_USER_INFO) Marshal.PtrToStructure(ptrUserInfo, typeof(DSREG_USER_INFO));
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

                yield return new DSregcmdDTO(
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
            else
            {
                WriteError("Unable to collect. No relevant information were returned");
                yield break;
            }

            NetFreeAadJoinInformation(ptrJoinInfo);

        }
    }

    internal class DSregcmdDTO : CommandDTOBase
    {
        public DSregcmdDTO(JoinType jType, Guid deviceId, string idpDomain, Guid tenantId, string joinUserEmail, string tenantDisplayName, string mdmEnrollmentUrl, string mdmTermsOfUseUrl,
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

    [CommandOutputType(typeof(DSregcmdDTO))]
    internal class DSregcmdFormatter : TextFormatterBase
    {
        public DSregcmdFormatter(ITextWriter writer) : base(writer)
        {
            // nothing goes here
        }
        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (DSregcmdDTO)result;

            WriteLine($"    TenantDisplayName  : {dto.TenantDisplayName}");
            WriteLine($"    TenantId           : {dto.TenantId}");
            WriteLine($"    IdpDomain          : {dto.IdpDomain}");
            WriteLine($"    MdmEnrollmentUrl   : {dto.MdmEnrollmentUrl}");
            WriteLine($"    MdmTermsOfUseUrl   : {dto.MdmTermsOfUseUrl}");
            WriteLine($"    MdmComplianceUrl   : {dto.MdmComplianceUrl}");
            WriteLine($"    UserSettingSyncUrl : {dto.UserSettingSyncUrl}");
            WriteLine($"    DeviceId           : {dto.DeviceId}");
            WriteLine($"    JoinType           : {dto.JType}");
            WriteLine($"    JoinUserEmail      : {dto.JoinUserEmail}");
            WriteLine($"    UserKeyId          : {dto.UserKeyId}");
            WriteLine($"    UserEmail          : {dto.UserEmail}");
            WriteLine($"    UserKeyname        : {dto.UserKeyname}\n");

            foreach (var cert in dto.CertInfo)
            {
                WriteLine($"    Thumbprint  : {cert.Thumbprint}");
                WriteLine($"    Subject     : {cert.Subject}");
                WriteLine($"    Issuer      : {cert.Issuer}");
                WriteLine($"    Expiration  : {cert.GetExpirationDateString()}");
            }
        }
    }
}
