using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Seatbelt.Util;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    internal class CertificateCommand : CommandBase
    {
        public override string Command => "Certificates";
        public override string Description => "Finds user and machine certificate files";
        public override CommandGroup[] Group => new[] { CommandGroup.User, CommandGroup.System};
        public override bool SupportRemote => false;

        public CertificateCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            foreach (Enum storeLocation in new Enum[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
            {

                X509Store store = new X509Store(StoreName.My, (System.Security.Cryptography.X509Certificates.StoreLocation)storeLocation);
                store.Open(OpenFlags.ReadOnly);
                List<X509Certificate2> cresult = new List<X509Certificate2>();

                foreach (X509Certificate2 certificate in store.Certificates)
                {
                    string? template = "";
                    List<string>? enhancedKeyUsages = new List<string>();
                    bool? keyExportable = false;

                    try
                    {
                        var priv = certificate.PrivateKey.ToXmlString(true);
                        keyExportable = true;
                    }
                    catch (Exception e) {
                        //Console.WriteLine("e.Message: {0}", e.Message);
                        if(e.Message.Contains("not valid for use in specified state"))
                        {
                            // message means private key is not exportable
                            keyExportable = false;
                        }
                        else
                        {
                            // likely exportable
                            keyExportable = true;
                        }
                    }

                    foreach (var ext in certificate.Extensions)
                    {
                        if (ext.Oid.FriendlyName == "Enhanced Key Usage")
                        {
                            var extUsages = ((X509EnhancedKeyUsageExtension)ext).EnhancedKeyUsages;

                            if (extUsages.Count > 0)
                            {
                                foreach (var extUsage in extUsages)
                                {
                                    enhancedKeyUsages.Add(extUsage.FriendlyName);
                                }
                            }
                        }
                        else if (ext.Oid.FriendlyName == "Certificate Template Name")
                        {
                            template = ext.Format(false);
                        }
                        else if (ext.Oid.FriendlyName == "Certificate Template Information")
                        {
                            template = ext.Format(false);
                        }
                    }

                    if (!Runtime.FilterResults || (Runtime.FilterResults && (DateTime.Compare(certificate.NotAfter, DateTime.Now) >= 0)))
                    {
                        yield return new CertificateDTO()
                        {
                            StoreLocation = String.Format("{0}", storeLocation),
                            Issuer = certificate.Issuer,
                            Subject = certificate.Subject,
                            ValidDate = certificate.NotBefore,
                            ExpiryDate = certificate.NotAfter,
                            HasPrivateKey = certificate.HasPrivateKey,
                            KeyExportable = keyExportable,
                            Template = template,
                            Thumbprint = certificate.Thumbprint,
                            EnhancedKeyUsages = enhancedKeyUsages
                        };
                    }
                }
            }
        }

        internal class CertificateDTO : CommandDTOBase
        {
            public string? StoreLocation { get; set; }
            public string? Issuer { get; set; }
            public string? Subject { get; set; }
            public DateTime? ValidDate { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public bool? HasPrivateKey { get; set; }
            public bool? KeyExportable { get; set; }
            public string? Thumbprint { get; set; }
            public string? Template { get; set; }
            public List<string>? EnhancedKeyUsages { get; set; }
        }

        [CommandOutputType(typeof(CertificateDTO))]
        internal class CertificateFormatter : TextFormatterBase
        {
            public CertificateFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (CertificateDTO)result;

                WriteLine("  StoreLocation      : {0}", dto.StoreLocation);
                WriteLine("  Issuer             : {0}", dto.Issuer);
                WriteLine("  Subject            : {0}", dto.Subject);
                WriteLine("  ValidDate          : {0}", dto.ValidDate);
                WriteLine("  ExpiryDate         : {0}", dto.ExpiryDate);
                WriteLine("  HasPrivateKey      : {0}", dto.HasPrivateKey);
                WriteLine("  KeyExportable      : {0}", dto.KeyExportable);
                WriteLine("  Thumbprint         : {0}", dto.Thumbprint);

                if (!String.IsNullOrEmpty(dto.Template))
                {
                    WriteLine("  Template           : {0}", dto.Template);
                }
                
                if (dto.EnhancedKeyUsages.Count > 0)
                {
                    WriteLine("  EnhancedKeyUsages  :");
                    foreach(var eku in dto.EnhancedKeyUsages)
                    {
                        WriteLine("       {0}", eku);
                        if(eku == "Client Authentication")
                        {
                            Console.WriteLine("         [!] Certificate is used for client authentication!");
                        }
                    }
                }
                WriteLine();
            }
        }
    }
}
