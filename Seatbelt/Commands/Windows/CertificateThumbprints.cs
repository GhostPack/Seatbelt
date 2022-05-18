using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    internal class CertificateThumbprintCommand : CommandBase
    {
        public override string Command => "CertificateThumbprints";
        public override string Description => "Finds thumbprints for all certificate store certs on the system";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.System };
        public override bool SupportRemote => false;

        public CertificateThumbprintCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            foreach (var storeName in new Enum[] { StoreName.Root, StoreName.CertificateAuthority, StoreName.AuthRoot, StoreName.TrustedPeople, StoreName.TrustedPublisher })
            {
                foreach (var storeLocation in new Enum[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
                {
                    var store = new X509Store((StoreName)storeName, (StoreLocation)storeLocation);
                    store.Open(OpenFlags.ReadOnly);

                    foreach (var certificate in store.Certificates)
                    {
                        if (!Runtime.FilterResults || (Runtime.FilterResults && (DateTime.Compare(certificate.NotAfter, DateTime.Now) >= 0)))
                        {
                            yield return new CertificateThumbprintDTO()
                            {
                                StoreName = $"{storeName}",
                                StoreLocation = $"{storeLocation}",
                                SimpleName = certificate.GetNameInfo(X509NameType.SimpleName, false),
                                Thumbprint = certificate.Thumbprint,
                                ExpiryDate = certificate.NotAfter,
                            };
                        }
                    }
                }
            }
        }

        internal class CertificateThumbprintDTO : CommandDTOBase
        {
            public string? StoreName { get; set; }
            public string? StoreLocation { get; set; }
            public string? SimpleName { get; set; }
            public string? Thumbprint { get; set; }
            public DateTime? ExpiryDate { get; set; }
        }

        [CommandOutputType(typeof(CertificateThumbprintDTO))]
        internal class CertificateThumbprintFormatter : TextFormatterBase
        {
            public CertificateThumbprintFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (CertificateThumbprintDTO)result;
                WriteLine($"{dto.StoreLocation}\\{dto.StoreName} - {dto.Thumbprint} ({dto.SimpleName}) {dto.ExpiryDate}");
            }
        }
    }
}
