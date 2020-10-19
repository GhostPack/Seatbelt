using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Seatbelt.Interop;

namespace Seatbelt.Commands
{
    class MTPuTTYConfig
    {
        public enum MTPuTTYConType
        {
            None = 0,
            Raw = 1,
            Telnet = 2,
            Rlogin = 3,
            SSH = 4,
            Serial = 5
        }

        public MTPuTTYConfig(string DisplayName, string ServerName, string UserName, string Password, string PasswordDelay, string CLParams, string ScriptDelay, MTPuTTYConType ConnType = MTPuTTYConType.SSH, int Port = 0)
        {
            this.DisplayName = DisplayName;
            this.ServerName = ServerName;
            this.ConnType = ConnType;
            this.Port = Port;
            this.UserName = UserName;
            this.Password = Password;
            this.PasswordDelay = PasswordDelay;
            this.CLParams = CLParams;
            this.ScriptDelay = ScriptDelay;
        }


        public string DisplayName { get; set; }
        public string ServerName { get; set; }
        public MTPuTTYConType ConnType { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PasswordDelay { get; set; }
        public string CLParams { get; set; }
        public string ScriptDelay { get; set; }

    }
    internal class MTPuTTYCommand : CommandBase
    {
        public override string Command => "MTPuTTY";
        public override string Description => "MTPuTTY configuration files";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;

        public MTPuTTYCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users"))
                    continue;

                var parts = dir.Split('\\');
                var userName = parts[parts.Length - 1];
                var configs = new List<MTPuTTYConfig>();

                string[] paths = { $"{dir}\\AppData\\Roaming\\TTYPlus\\mtputty.xml" };

                foreach (var path in paths)
                {
                    if (!File.Exists(path))
                        continue;


                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(path);

                    var sessions = xmlDoc.GetElementsByTagName("Node");

                    if (sessions.Count == 0)
                        continue;

                    foreach (XmlNode session in sessions)
                    {
                        if (session.Attributes["Type"].Value == "1"){
                            var displayName =session["DisplayName"].InnerText;
                            var serverName = session["ServerName"].InnerText;
                            var conType = int.Parse(session["PuttyConType"].InnerText);
                            var port = int.Parse(session["Port"].InnerText);
                            var username = session["UserName"].InnerText;
                            var encpassword = session["Password"].InnerText;
                            var passwordDelay = session["PasswordDelay"].InnerText;
                            var clParams = session["CLParams"].InnerText;
                            var scriptDelay = session["ScriptDelay"].InnerText;
                            
                            // 1 + username + displayname to decrypt
                            byte[] decryptKey = Encoding.UTF8.GetBytes($"1{username.Trim()}{displayName.Trim()}");
                            string password = DecryptUserPassword(decryptKey, encpassword);

                            var config = new MTPuTTYConfig(displayName, 
                                serverName, 
                                username, 
                                password, 
                                passwordDelay, 
                                clParams, 
                                scriptDelay, 
                                (MTPuTTYConfig.MTPuTTYConType)conType, 
                                port);

                            configs.Add(config);
                        }
                    }
                }

                if (configs.Count > 0)
                {
                    yield return new MTPuTTYDTO(
                        userName,
                        configs
                    );
                }
            }
        }

        private string DecryptUserPassword(byte[] pass, string encPass)
        {
            //Password Decryption code based on code from @ykoster https://gist.github.com/ykoster/0a475e4f09e8e5c714ae741933ab21a2
            string decryptedPass = "";
            IntPtr hProv = IntPtr.Zero;
            IntPtr hHash = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;
            uint CRYPT_VERIFY_CONTEXT = 0xF0000000;
            uint CALG_SHA = 0x00008004;
            uint CALG_RC2 = 0x00006602;
            byte[] cipherText = Convert.FromBase64String(encPass);
            uint cipherTextLength = (uint)cipherText.Length;

            if (Advapi32.CryptAcquireContext(ref hProv, "", "", 1, CRYPT_VERIFY_CONTEXT))
            {
                if (Advapi32.CryptCreateHash(hProv, CALG_SHA, IntPtr.Zero, 0, ref hHash))
                {
                    if (Advapi32.CryptHashData(hHash, pass, (uint)pass.Length, 0))
                    {
                        if (Advapi32.CryptDeriveKey(hProv, CALG_RC2, hHash, 0, ref hKey))
                        {
                            if (Advapi32.CryptDecrypt(hKey, IntPtr.Zero, true, 0, cipherText, ref cipherTextLength))
                            {
                                cipherText = cipherText.Skip(0).Take((int)cipherTextLength).ToArray();
                                if (cipherTextLength > 2 && cipherText[1] == 0x00)
                                {
                                    decryptedPass = Encoding.Unicode.GetString(cipherText);
                                }
                                else
                                {
                                    decryptedPass = Encoding.UTF8.GetString(cipherText);
                                }
                            }
                        }
                    }
                }
            }
            

            if (String.IsNullOrEmpty(decryptedPass))
            {
                if(Marshal.GetLastWin32Error() == 0)
                    decryptedPass = "[EMPTY]";
                else
                    decryptedPass = $"CouldNotDecrypt: {encPass} ({Marshal.GetLastWin32Error()})";
            }

            return decryptedPass;
        }

        internal class MTPuTTYDTO : CommandDTOBase
        {
            public MTPuTTYDTO(string userName, List<MTPuTTYConfig> configs)
            {
                UserName = userName;
                Configs = configs;
            }
            public string UserName { get; set; }
            public List<MTPuTTYConfig> Configs { get; set; }
        }

     [CommandOutputType(typeof(MTPuTTYDTO))]
        internal class MTPuTTYFormatter : TextFormatterBase
        {
            public MTPuTTYFormatter(ITextWriter writer) : base(writer)
            {
            }

            public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
            {
                var dto = (MTPuTTYDTO)result;

                WriteLine($"  MTPuTTY Configs ({dto.UserName}):\n");

                foreach (var config in dto.Configs)
                {
                    WriteLine($"    DisplayName    : {config.DisplayName}");
                    WriteLine($"    ServerName     : {config.ServerName}");
                    WriteLine($"    PuttyConType   : {config.ConnType}");
                    WriteLine($"    Port           : {config.Port}");
                    WriteLine($"    UserName       : {config.UserName}");
                    WriteLine($"    Password       : {config.Password}");
                    WriteLine($"    PasswordDelay  : {config.PasswordDelay}");
                    WriteLine($"    CLParams       : {config.CLParams}");
                    WriteLine($"    ScriptDelay    : {config.ScriptDelay}");
                    WriteLine();
                }
                WriteLine();
            }           
        }
    }

}
