using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class KerberosTGTData : IProbe
    {
        public static string ProbeName => "KerberosTGTData";

        public string List()
        {
            var sb = new StringBuilder();

            try
            {
                if (Helpers.IsHighIntegrity())
                    ListKerberosTGTDataAllUsers(sb);
                else
                    ListKerberosTGTDataCurrentUser(sb);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

        public static void ListKerberosTGTDataAllUsers(StringBuilder sb)
        {
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            sb.AppendProbeHeaderLine("Kerberos TGT Data (All Users)");

            var hLsa = Helpers.LsaRegisterLogonProcessHelper();
            var totalTicketCount = 0;

            // if the original call fails then it is likely we don't have SeTcbPrivilege
            // to get SeTcbPrivilege we can Impersonate a NT AUTHORITY\SYSTEM Token
            if (hLsa == IntPtr.Zero)
            {
                Helpers.GetSystem();
                // should now have the proper privileges to get a Handle to LSA
                hLsa = Helpers.LsaRegisterLogonProcessHelper();
                // we don't need our NT AUTHORITY\SYSTEM Token anymore so we can revert to our original token
                NativeMethods.RevertToSelf();
            }


            // first return all the logon sessions

            var systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate
            UInt64 count;
            var luidPtr = IntPtr.Zero;
            var iter = luidPtr;

            var ret = NativeMethods.LsaEnumerateLogonSessions(out count, out luidPtr);  // get an array of pointers to LUIDs

            for (ulong i = 0; i < count; i++)
            {
                IntPtr sessionData;
                ret = NativeMethods.LsaGetLogonSessionData(luidPtr, out sessionData);
                var data = (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                // if we have a valid logon
                if (data.PSiD != IntPtr.Zero)
                {
                    // user session data
                    var username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();
                    var sid = new SecurityIdentifier(data.PSiD);
                    var domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();
                    var authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();
                    var logonType = (SECURITY_LOGON_TYPE)data.LogonType;
                    var logonTime = systime.AddTicks((long)data.LoginTime);
                    var logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();
                    var dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();
                    var upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                    // now we want to get the tickets for this logon ID
                    var name = "kerberos";
                    LSA_STRING_IN LSAString;
                    LSAString.Length = (ushort)name.Length;
                    LSAString.MaximumLength = (ushort)(name.Length + 1);
                    LSAString.Buffer = name;

                    var responsePointer = IntPtr.Zero;
                    int authPack;
                    var returnBufferLength = 0;
                    var protocalStatus = 0;
                    int retCode;

                    var tQuery = new KERB_RETRIEVE_TKT_REQUEST();
                    var response = new KERB_RETRIEVE_TKT_RESPONSE();

                    // obtains the unique identifier for the kerberos authentication package.
                    retCode = NativeMethods.LsaLookupAuthenticationPackage(hLsa, ref LSAString, out authPack);

                    // input object for querying the TGT for a specific logon ID (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_retrieve_tkt_request)
                    LUID userLogonID = new LUID();
                    userLogonID.LowPart = data.LoginID.LowPart;
                    userLogonID.HighPart = 0;
                    tQuery.LogonId = userLogonID;
                    tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveTicketMessage;
                    // indicate we want kerb creds yo'
                    tQuery.CacheOptions = KERB_CACHE_OPTIONS.KERB_RETRIEVE_TICKET_AS_KERB_CRED;

                    // query LSA, specifying we want the the TGT data
                    retCode = NativeMethods.LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(hLsa, authPack, ref tQuery, Marshal.SizeOf(tQuery), out responsePointer, out returnBufferLength, out protocalStatus);

                    if ((retCode) == 0 && (responsePointer != IntPtr.Zero))
                    {
                        sb.AppendLine();
                        sb.AppendLine("  UserName                 : " + username);
                        sb.AppendLine("  Domain                   : " + domain);
                        sb.AppendLine("  LogonId                  : " + data.LoginID.LowPart);
                        sb.AppendLine("  UserSID                  : " + sid.AccountDomainSid);
                        sb.AppendLine("  AuthenticationPackage    : " + authpackage);
                        sb.AppendLine("  LogonType                : " + logonType);
                        sb.AppendLine("  LogonType                : " + logonTime);
                        sb.AppendLine("  LogonServer              : " + logonServer);
                        sb.AppendLine("  LogonServerDNSDomain     : " + dnsDomainName);
                        sb.AppendLine("  UserPrincipalName        : " + upn);

                        // parse the returned pointer into our initial KERB_RETRIEVE_TKT_RESPONSE structure
                        response = (KERB_RETRIEVE_TKT_RESPONSE)Marshal.PtrToStructure(responsePointer, typeof(KERB_RETRIEVE_TKT_RESPONSE));

                        KERB_EXTERNAL_NAME serviceNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ServiceName, typeof(KERB_EXTERNAL_NAME));
                        var serviceName = Marshal.PtrToStringUni(serviceNameStruct.Names.Buffer, serviceNameStruct.Names.Length / 2).Trim();

                        var targetName = "";
                        if (response.Ticket.TargetName != IntPtr.Zero)
                        {
                            KERB_EXTERNAL_NAME targetNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.TargetName, typeof(KERB_EXTERNAL_NAME));
                            targetName = Marshal.PtrToStringUni(targetNameStruct.Names.Buffer, targetNameStruct.Names.Length / 2).Trim();
                        }

                        KERB_EXTERNAL_NAME clientNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ClientName, typeof(KERB_EXTERNAL_NAME));
                        var clientName = Marshal.PtrToStringUni(clientNameStruct.Names.Buffer, clientNameStruct.Names.Length / 2).Trim();

                        var domainName = Marshal.PtrToStringUni(response.Ticket.DomainName.Buffer, response.Ticket.DomainName.Length / 2).Trim();
                        var targetDomainName = Marshal.PtrToStringUni(response.Ticket.TargetDomainName.Buffer, response.Ticket.TargetDomainName.Length / 2).Trim();
                        var altTargetDomainName = Marshal.PtrToStringUni(response.Ticket.AltTargetDomainName.Buffer, response.Ticket.AltTargetDomainName.Length / 2).Trim();

                        // extract the session key
                        KERB_ENCRYPTION_TYPE sessionKeyType = (KERB_ENCRYPTION_TYPE)response.Ticket.SessionKey.KeyType;
                        Int32 sessionKeyLength = response.Ticket.SessionKey.Length;
                        var sessionKey = new byte[sessionKeyLength];
                        Marshal.Copy(response.Ticket.SessionKey.Value, sessionKey, 0, sessionKeyLength);
                        var base64SessionKey = Convert.ToBase64String(sessionKey);

                        var keyExpirationTime = DateTime.FromFileTime(response.Ticket.KeyExpirationTime);
                        var startTime = DateTime.FromFileTime(response.Ticket.StartTime);
                        var endTime = DateTime.FromFileTime(response.Ticket.EndTime);
                        var renewUntil = DateTime.FromFileTime(response.Ticket.RenewUntil);
                        Int64 timeSkew = response.Ticket.TimeSkew;
                        Int32 encodedTicketSize = response.Ticket.EncodedTicketSize;

                        string ticketFlags = ((KERB_TICKET_FLAGS)response.Ticket.TicketFlags).ToString();

                        // extract the TGT and base64 encode it
                        var encodedTicket = new byte[encodedTicketSize];
                        Marshal.Copy(response.Ticket.EncodedTicket, encodedTicket, 0, encodedTicketSize);
                        var base64TGT = Convert.ToBase64String(encodedTicket);

                        sb.AppendLine("  ServiceName              : " + serviceName);
                        sb.AppendLine("  TargetName               : " + targetName);
                        sb.AppendLine("  ClientName               : " + clientName);
                        sb.AppendLine("  DomainName               : " + domainName);
                        sb.AppendLine("  TargetDomainName         : " + targetDomainName);
                        sb.AppendLine("  AltTargetDomainName      : " + altTargetDomainName);
                        sb.AppendLine("  SessionKeyType           : " + sessionKeyType);
                        sb.AppendLine("  Base64SessionKey         : " + base64SessionKey);
                        sb.AppendLine("  KeyExpirationTime        : " + keyExpirationTime);
                        sb.AppendLine("  TicketFlags              : " + ticketFlags);
                        sb.AppendLine("  StartTime                : " + startTime);
                        sb.AppendLine("  EndTime                  : " + endTime);
                        sb.AppendLine("  RenewUntil               : " + renewUntil);
                        sb.AppendLine("  TimeSkew                 : " + timeSkew);
                        sb.AppendLine("  EncodedTicketSize        : " + encodedTicketSize);
                        sb.AppendLine("  Base64EncodedTicket      :").AppendLine();

                        // display the TGT, columns of 100 chararacters
                        foreach (var line in Helpers.Split(base64TGT, 100, sb))
                        {
                            sb.AppendLine("    " + line);
                        }
                        sb.AppendLine();
                        totalTicketCount++;
                    }
                }
                luidPtr = (IntPtr)(luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                //move the pointer forward
                NativeMethods.LsaFreeReturnBuffer(sessionData);
                //free the SECURITY_LOGON_SESSION_DATA memory in the struct
            }
            NativeMethods.LsaFreeReturnBuffer(luidPtr);       //free the array of LUIDs

            // disconnect from LSA
            NativeMethods.LsaDeregisterLogonProcess(hLsa);

            sb.AppendLine().AppendLine();
            sb.AppendLine("  [*] Extracted {totalTicketCount} total tickets");
            sb.AppendLine();

        }

        private static void ListKerberosTGTDataCurrentUser(StringBuilder sb)
        {
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            sb.AppendProbeHeaderLine("Kerberos TGT Data (Current User)");


            var name = "kerberos";
            LSA_STRING_IN LSAString;
            LSAString.Length = (ushort)name.Length;
            LSAString.MaximumLength = (ushort)(name.Length + 1);
            LSAString.Buffer = name;

            var responsePointer = IntPtr.Zero;
            int authPack;
            var returnBufferLength = 0;
            var protocalStatus = 0;
            IntPtr lsaHandle;
            int retCode;

            // If we want to look at tickets from a session other than our own
            // then we need to use LsaRegisterLogonProcess instead of LsaConnectUntrusted
            retCode = NativeMethods.LsaConnectUntrusted(out lsaHandle);

            KERB_RETRIEVE_TKT_REQUEST tQuery = new KERB_RETRIEVE_TKT_REQUEST();
            KERB_RETRIEVE_TKT_RESPONSE response = new KERB_RETRIEVE_TKT_RESPONSE();

            // obtains the unique identifier for the kerberos authentication package.
            retCode = NativeMethods.LsaLookupAuthenticationPackage(lsaHandle, ref LSAString, out authPack);

            // input object for querying the TGT (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_retrieve_tkt_request)
            tQuery.LogonId = new LUID();
            tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveTicketMessage;
            // indicate we want kerb creds yo'
            //tQuery.CacheOptions = KERB_CACHE_OPTIONS.KERB_RETRIEVE_TICKET_AS_KERB_CRED;

            // query LSA, specifying we want the the TGT data
            retCode = NativeMethods.LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(lsaHandle, authPack, ref tQuery, Marshal.SizeOf(tQuery), out responsePointer, out returnBufferLength, out protocalStatus);

            // parse the returned pointer into our initial KERB_RETRIEVE_TKT_RESPONSE structure
            response = (KERB_RETRIEVE_TKT_RESPONSE)Marshal.PtrToStructure(responsePointer, typeof(KERB_RETRIEVE_TKT_RESPONSE));

            KERB_EXTERNAL_NAME serviceNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ServiceName, typeof(KERB_EXTERNAL_NAME));
            var serviceName = Marshal.PtrToStringUni(serviceNameStruct.Names.Buffer, serviceNameStruct.Names.Length / 2).Trim();

            var targetName = "";
            if (response.Ticket.TargetName != IntPtr.Zero)
            {
                KERB_EXTERNAL_NAME targetNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.TargetName, typeof(KERB_EXTERNAL_NAME));
                targetName = Marshal.PtrToStringUni(targetNameStruct.Names.Buffer, targetNameStruct.Names.Length / 2).Trim();
            }

            KERB_EXTERNAL_NAME clientNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ClientName, typeof(KERB_EXTERNAL_NAME));
            var clientName = Marshal.PtrToStringUni(clientNameStruct.Names.Buffer, clientNameStruct.Names.Length / 2).Trim();

            var domainName = Marshal.PtrToStringUni(response.Ticket.DomainName.Buffer, response.Ticket.DomainName.Length / 2).Trim();
            var targetDomainName = Marshal.PtrToStringUni(response.Ticket.TargetDomainName.Buffer, response.Ticket.TargetDomainName.Length / 2).Trim();
            var altTargetDomainName = Marshal.PtrToStringUni(response.Ticket.AltTargetDomainName.Buffer, response.Ticket.AltTargetDomainName.Length / 2).Trim();

            // extract the session key
            KERB_ENCRYPTION_TYPE sessionKeyType = (KERB_ENCRYPTION_TYPE)response.Ticket.SessionKey.KeyType;
            Int32 sessionKeyLength = response.Ticket.SessionKey.Length;
            var sessionKey = new byte[sessionKeyLength];
            Marshal.Copy(response.Ticket.SessionKey.Value, sessionKey, 0, sessionKeyLength);
            var base64SessionKey = Convert.ToBase64String(sessionKey);

            var keyExpirationTime = DateTime.FromFileTime(response.Ticket.KeyExpirationTime);
            var startTime = DateTime.FromFileTime(response.Ticket.StartTime);
            var endTime = DateTime.FromFileTime(response.Ticket.EndTime);
            var renewUntil = DateTime.FromFileTime(response.Ticket.RenewUntil);
            Int64 timeSkew = response.Ticket.TimeSkew;
            Int32 encodedTicketSize = response.Ticket.EncodedTicketSize;

            string ticketFlags = ((KERB_TICKET_FLAGS)response.Ticket.TicketFlags).ToString();

            // extract the TGT and base64 encode it
            var encodedTicket = new byte[encodedTicketSize];
            Marshal.Copy(response.Ticket.EncodedTicket, encodedTicket, 0, encodedTicketSize);
            var base64TGT = Convert.ToBase64String(encodedTicket);

            sb.AppendLine("  ServiceName              : " + serviceName);
            sb.AppendLine("  TargetName               : " + targetName);
            sb.AppendLine("  ClientName               : " + clientName);
            sb.AppendLine("  DomainName               : " + domainName);
            sb.AppendLine("  TargetDomainName         : " + targetDomainName);
            sb.AppendLine("  AltTargetDomainName      : " + altTargetDomainName);
            sb.AppendLine("  SessionKeyType           : " + sessionKeyType);
            sb.AppendLine("  Base64SessionKey         : " + base64SessionKey);
            sb.AppendLine("  KeyExpirationTime        : " + keyExpirationTime);
            sb.AppendLine("  TicketFlags              : " + ticketFlags);
            sb.AppendLine("  StartTime                : " + startTime);
            sb.AppendLine("  EndTime                  : " + endTime);
            sb.AppendLine("  RenewUntil               : " + renewUntil);
            sb.AppendLine("  TimeSkew                 : " + timeSkew);
            sb.AppendLine("  EncodedTicketSize        : " + encodedTicketSize);
            sb.AppendLine("  Base64EncodedTicket      :");
            sb.AppendLine();
            // display the TGT, columns of 100 chararacters
            foreach (var line in Helpers.Split(base64TGT, 100, sb))
            {
                sb.AppendLine("    " + line);
            }
            sb.AppendLine();

            // disconnect from LSA
            NativeMethods.LsaDeregisterLogonProcess(lsaHandle);


        }

    }
}
