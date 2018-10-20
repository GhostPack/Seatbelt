using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class KerberosTickets : IProbe
    {
        public static string ProbeName => "KerberosTickets";

        public string List()
        {
            var sb = new StringBuilder();

            if (Helpers.IsHighIntegrity())
                ListKerberosTicketsAllUsers(sb);
            else
                ListKerberosTicketsCurrentUser(sb);

            return sb.ToString();
        }
        
        private static void ListKerberosTicketsAllUsers(StringBuilder sb)
        {
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            sb.AppendProbeHeaderLine("Kerberos Tickets (All Users)");


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

            try
            {
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

                        var ticketPointer = IntPtr.Zero;
                        var ticketsPointer = IntPtr.Zero;
                        var sysTime = new DateTime(1601, 1, 1, 0, 0, 0, 0);
                        int authPack;
                        var returnBufferLength = 0;
                        var protocalStatus = 0;
                        int retCode;

                        var tQuery = new KERB_QUERY_TKT_CACHE_REQUEST();
                        var tickets = new KERB_QUERY_TKT_CACHE_RESPONSE();
                        KERB_TICKET_CACHE_INFO ticket;

                        // obtains the unique identifier for the kerberos authentication package.
                        retCode = NativeMethods.LsaLookupAuthenticationPackage(hLsa, ref LSAString, out authPack);

                        // input object for querying the ticket cache for a specific logon ID
                        var userLogonID = new LUID();
                        userLogonID.LowPart = data.LoginID.LowPart;
                        userLogonID.HighPart = 0;
                        tQuery.LogonId = userLogonID;
                        tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbQueryTicketCacheMessage;

                        // query LSA, specifying we want the ticket cache
                        retCode = NativeMethods.LsaCallAuthenticationPackage(hLsa, authPack, ref tQuery, Marshal.SizeOf(tQuery), out ticketPointer, out returnBufferLength, out protocalStatus);

                        sb.AppendLine();

                        sb.AppendLine($"  UserName                 : {username}");
                        sb.AppendLine($"  Domain                   : {domain}");
                        sb.AppendLine($"  LogonId                  : {data.LoginID.LowPart}");
                        sb.AppendLine($"  UserSID                  : {sid.AccountDomainSid}");
                        sb.AppendLine($"  AuthenticationPackage    : {authpackage}");
                        sb.AppendLine($"  LogonType                : {logonType}");
                        sb.AppendLine($"  LogonType                : {logonTime}");
                        sb.AppendLine($"  LogonServer              : {logonServer}");
                        sb.AppendLine($"  LogonServerDNSDomain     : {dnsDomainName}");
                        sb.AppendLine($"  UserPrincipalName        : {upn}");

                        sb.AppendLine();

                        if (ticketPointer != IntPtr.Zero)
                        {
                            // parse the returned pointer into our initial KERB_QUERY_TKT_CACHE_RESPONSE structure
                            tickets = (KERB_QUERY_TKT_CACHE_RESPONSE)Marshal.PtrToStructure(ticketPointer, typeof(KERB_QUERY_TKT_CACHE_RESPONSE));
                            var count2 = tickets.CountOfTickets;

                            if (count2 != 0)
                            {
                                sb.AppendLine($"    [*] Enumerated {count2} ticket(s):").AppendLine();
                                totalTicketCount += count2;
                                // get the size of the structures we're iterating over
                                var dataSize = Marshal.SizeOf(typeof(KERB_TICKET_CACHE_INFO));

                                for (var j = 0; j < count2; j++)
                                {
                                    // iterate through the structures
                                    var currTicketPtr = (IntPtr)(ticketPointer.ToInt64() + (8 + j * dataSize));

                                    // parse the new ptr to the appropriate structure
                                    ticket = (KERB_TICKET_CACHE_INFO)Marshal.PtrToStructure(currTicketPtr, typeof(KERB_TICKET_CACHE_INFO));

                                    // extract our fields
                                    var serverName = Marshal.PtrToStringUni(ticket.ServerName.Buffer, ticket.ServerName.Length / 2);
                                    var realmName = Marshal.PtrToStringUni(ticket.RealmName.Buffer, ticket.RealmName.Length / 2);
                                    var startTime = DateTime.FromFileTime(ticket.StartTime);
                                    var endTime = DateTime.FromFileTime(ticket.EndTime);
                                    var renewTime = DateTime.FromFileTime(ticket.RenewTime);
                                    var encryptionType = ((KERB_ENCRYPTION_TYPE)ticket.EncryptionType).ToString();
                                    var ticketFlags = ((KERB_TICKET_FLAGS)ticket.TicketFlags).ToString();

                                    sb.AppendLine($"    ServerName         :  {serverName}");
                                    sb.AppendLine($"    RealmName          :  {realmName}");
                                    sb.AppendLine($"    StartTime          :  {startTime}");
                                    sb.AppendLine($"    EndTime            :  {endTime}");
                                    sb.AppendLine($"    RenewTime          :  {renewTime}");
                                    sb.AppendLine($"    EncryptionType     :  {encryptionType}");
                                    sb.AppendLine($"    TicketFlags        :  {ticketFlags}");

                                    sb.AppendLine();

                                }
                            }
                        }
                    }
                    // move the pointer forward
                    luidPtr = (IntPtr)(luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                    NativeMethods.LsaFreeReturnBuffer(sessionData);
                }
                NativeMethods.LsaFreeReturnBuffer(luidPtr);

                // disconnect from LSA
                NativeMethods.LsaDeregisterLogonProcess(hLsa);

                sb.AppendLine()
                    .AppendLine()
                    .AppendLine($"  [*] Enumerated {totalTicketCount} total tickets")
                    .AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

        }

        private static void ListKerberosTicketsCurrentUser(StringBuilder sb)
        {
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            sb.AppendProbeHeaderLine("Kerberos Tickets (Current User)");

            try
            {
                var name = "kerberos";
                LSA_STRING_IN LSAString;
                LSAString.Length = (ushort)name.Length;
                LSAString.MaximumLength = (ushort)(name.Length + 1);
                LSAString.Buffer = name;

                var ticketPointer = IntPtr.Zero;
                var ticketsPointer = IntPtr.Zero;
                var sysTime = new DateTime(1601, 1, 1, 0, 0, 0, 0);
                int authPack;
                var returnBufferLength = 0;
                var protocalStatus = 0;
                IntPtr lsaHandle;
                int retCode;

                // If we want to look at tickets from a session other than our own
                // then we need to use LsaRegisterLogonProcess instead of LsaConnectUntrusted
                retCode = NativeMethods.LsaConnectUntrusted(out lsaHandle);

                var tQuery = new KERB_QUERY_TKT_CACHE_REQUEST();
                var tickets = new KERB_QUERY_TKT_CACHE_RESPONSE();
                KERB_TICKET_CACHE_INFO ticket;

                // obtains the unique identifier for the kerberos authentication package.
                retCode = NativeMethods.LsaLookupAuthenticationPackage(lsaHandle, ref LSAString, out authPack);

                // input object for querying the ticket cache (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_query_tkt_cache_request)
                tQuery.LogonId = new LUID();
                tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbQueryTicketCacheMessage;

                // query LSA, specifying we want the ticket cache
                retCode = NativeMethods.LsaCallAuthenticationPackage(lsaHandle, authPack, ref tQuery, Marshal.SizeOf(tQuery), out ticketPointer, out returnBufferLength, out protocalStatus);

                // parse the returned pointer into our initial KERB_QUERY_TKT_CACHE_RESPONSE structure
                tickets = (KERB_QUERY_TKT_CACHE_RESPONSE)Marshal.PtrToStructure(ticketPointer, typeof(KERB_QUERY_TKT_CACHE_RESPONSE));
                var count = tickets.CountOfTickets;

                sb.AppendLine($"  [*] Returned {count} tickets\r\n");

                // get the size of the structures we're iterating over
                var dataSize = Marshal.SizeOf(typeof(KERB_TICKET_CACHE_INFO));

                for (var i = 0; i < count; i++)
                {
                    // iterate through the structures
                    var currTicketPtr = (IntPtr)(ticketPointer.ToInt64() + (8 + i * dataSize));

                    // parse the new ptr to the appropriate structure
                    ticket = (KERB_TICKET_CACHE_INFO)Marshal.PtrToStructure(currTicketPtr, typeof(KERB_TICKET_CACHE_INFO));

                    // extract our fields
                    var serverName = Marshal.PtrToStringUni(ticket.ServerName.Buffer, ticket.ServerName.Length / 2);
                    var realmName = Marshal.PtrToStringUni(ticket.RealmName.Buffer, ticket.RealmName.Length / 2);
                    var startTime = DateTime.FromFileTime(ticket.StartTime);
                    var endTime = DateTime.FromFileTime(ticket.EndTime);
                    var renewTime = DateTime.FromFileTime(ticket.RenewTime);
                    var encryptionType = ((KERB_ENCRYPTION_TYPE)ticket.EncryptionType).ToString();
                    var ticketFlags = ((KERB_TICKET_FLAGS)ticket.TicketFlags).ToString();

                    sb.AppendLine($"  ServerName         :  {serverName}");
                    sb.AppendLine($"  RealmName          :  {realmName}");
                    sb.AppendLine($"  StartTime          :  {startTime}");
                    sb.AppendLine($"  EndTime            :  {endTime}");
                    sb.AppendLine($"  RenewTime          :  {renewTime}");
                    sb.AppendLine($"  EncryptionType     :  {encryptionType}");
                    sb.AppendLine($"  TicketFlags        :  {ticketFlags}");
                }

                // disconnect from LSA
                NativeMethods.LsaDeregisterLogonProcess(lsaHandle);
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

        }

    }
}
