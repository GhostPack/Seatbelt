using System;
using System.Diagnostics;
using System.Linq;
using Seatbelt.IO;


namespace Seatbelt
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            // Check args have been provided. if not show logo and usage
            if (args == null || args.Any() == false)
            {
                Console.Write(Info.Logo());
                Console.Write(Info.Usage());
                return;
            }

            // get the output target: Console / File
            var output = ArgumentsParser.GetOutputTarget(args);

            // print the Logo
            output.Write(Info.Logo());

            // Get all the available probes
            var availableProbes = new AvailableProbes();

            // Parse the command line
            var requestedOperations = ArgumentsParser.Parse(args, output, availableProbes);

            var timer = Stopwatch.StartNew();

            // run all the operation as requested in the command line arguments           
            foreach (var operation in requestedOperations)
                operation.Invoke();

            timer.Stop();

            output.WriteLine($"\r\n\r\n[*] Completed Safety Checks in {(timer.ElapsedMilliseconds / 1000)} seconds\r\n");

            // Print to the console if the output is going to the File, to let the user know it's finished
            if (output is FileOutput)
                Console.WriteLine($"\r\n\r\n[*] Completed Safety Checks in {(timer.ElapsedMilliseconds / 1000)} seconds\r\n");

            output.FlushAndClose();


#if DEBUG
            foreach (var probeThatRan in availableProbes.GetProbesDone())
                Console.WriteLine("DONE: " + probeThatRan);
#endif


        }
    }

}


// https://github.com/pauldotknopf/WindowsSDK7-Samples/blob/master/security/authorization/klist/KList.c#L585
// currently not working :(
//public static void ListKerberosTicketDataCurrentUser()
//{
//    // adapted partially from Vincent LE TOUX' work
//    //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
//    // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
//    // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

//    Console.WriteLine("\r\n\r\n=== Kerberos Ticket Data (Current User) ===\r\n");

//    //try
//    //{
//    string name = "kerberos";
//    LSA_STRING_IN LSAString;
//    LSAString.Length = (ushort)name.Length;
//    LSAString.MaximumLength = (ushort)(name.Length + 1);
//    LSAString.Buffer = name;

//    IntPtr ticketPointer = IntPtr.Zero;
//    IntPtr ticketsPointer = IntPtr.Zero;
//    int authPack;
//    int returnBufferLength = 0;
//    int protocalStatus = 0;
//    IntPtr lsaHandle;
//    int retCode;

//    // If we want to look at tickets from a session other than our own
//    // then we need to use LsaRegisterLogonProcess instead of LsaConnectUntrusted
//    retCode = LsaConnectUntrusted(out lsaHandle);

//    // obtains the unique identifier for the kerberos authentication package.
//    retCode = LsaLookupAuthenticationPackage(lsaHandle, ref LSAString, out authPack);

//    UNICODE_STRING targetName = new UNICODE_STRING("krbtgt/TESTLAB.LOCAL");
//    UNICODE_STRING target = new UNICODE_STRING();

//    KERB_RETRIEVE_TKT_RESPONSE CacheResponse = new KERB_RETRIEVE_TKT_RESPONSE();

//    // LMEM_ZEROINIT -> 0x0040
//    IntPtr temp = LocalAlloc(0x0040, (uint)(targetName.Length + Marshal.SizeOf(typeof(KERB_RETRIEVE_TKT_REQUEST))));

//    IntPtr unmanagedAddr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(KERB_RETRIEVE_TKT_REQUEST)));
//    //Marshal.StructureToPtr(managedObj, unmanagedAddr, true);
//    KERB_RETRIEVE_TKT_REQUEST_UNI CacheRequest = (KERB_RETRIEVE_TKT_REQUEST_UNI)Marshal.PtrToStructure(temp, typeof(KERB_RETRIEVE_TKT_REQUEST_UNI));
//    CacheRequest.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveEncodedTicketMessage;

//    // KERB_RETRIEVE_TKT_REQUEST_UNI
//    IntPtr CacheRequestPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(KERB_RETRIEVE_TKT_REQUEST)));
//    Marshal.StructureToPtr(CacheRequest, CacheRequestPtr, false);
//    target.buffer = (IntPtr)(CacheRequestPtr.ToInt64() + 1);
//    target.Length = targetName.Length;
//    target.MaximumLength = targetName.MaximumLength;

//    CopyMemory(target.buffer, targetName.buffer, targetName.Length);

//    CacheRequest.TargetName = target;

//    IntPtr responsePointer = IntPtr.Zero;
//    int returnBufferLength2 = 0;
//    // query LSA, specifying we want the the specified ticket data
//    retCode = LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT_UNI(lsaHandle, authPack, ref CacheRequest, Marshal.SizeOf(CacheRequest) + targetName.Length, out responsePointer, out returnBufferLength2, out protocalStatus);
//    Console.WriteLine("LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT_UNI retCode: {0}", retCode);
//    Console.WriteLine("returnBufferLength: {0}", returnBufferLength2);
//    Console.WriteLine("responsePointer: {0}\r\n", responsePointer);
//    Console.WriteLine("protocalStatus: {0}\r\n", (uint)protocalStatus);
//    Console.Out.Flush();


//    //string clientName = Marshal.PtrToStringUni(CacheResponse.Ticket.ClientName, CacheResponse.Ticket.ClientName.L / 2);
//    DateTime startTime = DateTime.FromFileTime(CacheResponse.Ticket.StartTime);
//    DateTime endTime = DateTime.FromFileTime(CacheResponse.Ticket.EndTime);
//    Console.WriteLine("startTime: {0}", startTime);
//    Console.WriteLine("endTime: {0}", endTime);

//    //// query LSA, specifying we want the ticket cache
//    //retCode = LsaCallAuthenticationPackage(lsaHandle, authPack, ref tQuery, Marshal.SizeOf(tQuery), out ticketPointer, out returnBufferLength, out protocalStatus);

//    //// parse the returned pointer into our initial KERB_QUERY_TKT_CACHE_RESPONSE structure
//    //tickets = (KERB_QUERY_TKT_CACHE_EX_RESPONSE)Marshal.PtrToStructure((System.IntPtr)ticketPointer, typeof(KERB_QUERY_TKT_CACHE_EX_RESPONSE));
//    //int count = tickets.CountOfTickets;
//    //Console.WriteLine("  [*] Returned {0} tickets\r\n", count);

//    //// get the size of the structures we're iterating over
//    //Int32 dataSize = Marshal.SizeOf(typeof(KERB_TICKET_CACHE_INFO_EX));

//    //for (int i = 0; i < count; i++)
//    //{
//    //    // iterate through the structures
//    //    IntPtr currTicketPtr = (IntPtr)(long)((ticketPointer.ToInt64() + (int)(8 + i * dataSize)));

//    //    // parse the new ptr to the appropriate structure
//    //    ticket = (KERB_TICKET_CACHE_INFO_EX)Marshal.PtrToStructure(currTicketPtr, typeof(KERB_TICKET_CACHE_INFO_EX));

//    //    // extract our fields
//    //    string clientName = Marshal.PtrToStringUni(ticket.ClientName.Buffer, ticket.ClientName.Length / 2);
//    //    string clientRealm = Marshal.PtrToStringUni(ticket.ClientRealm.Buffer, ticket.ClientRealm.Length / 2);
//    //    string serverName = Marshal.PtrToStringUni(ticket.ServerName.Buffer, ticket.ServerName.Length / 2);
//    //    string serverRealm = Marshal.PtrToStringUni(ticket.ServerRealm.Buffer, ticket.ServerRealm.Length / 2);
//    //    Console.WriteLine("clientName: {0}", clientName);
//    //    Console.WriteLine("clientRealm: {0}", clientRealm);
//    //    Console.WriteLine("serverName: {0}", serverName);
//    //    Console.WriteLine("serverRealm: {0}", serverRealm);
//    //    DateTime startTime = DateTime.FromFileTime(ticket.StartTime);
//    //    DateTime endTime = DateTime.FromFileTime(ticket.EndTime);
//    //    DateTime renewTime = DateTime.FromFileTime(ticket.RenewTime);
//    //    string encryptionType = ((KERB_ENCRYPTION_TYPE)ticket.EncryptionType).ToString();
//    //    string ticketFlags = ((KERB_TICKET_FLAGS)ticket.TicketFlags).ToString();

//    //KERB_RETRIEVE_TKT_REQUEST ticketQuery = new KERB_RETRIEVE_TKT_REQUEST();
//    //KERB_RETRIEVE_TKT_RESPONSE response = new KERB_RETRIEVE_TKT_RESPONSE();

//    //// input object for querying the ticket cache
//    ////ticketQuery.LogonId = new LUID();
//    //ticketQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveEncodedTicketMessage;
//    //// indicate we want kerb creds yo'
//    //ticketQuery.CacheOptions = KERB_CACHE_OPTIONS.KERB_RETRIEVE_TICKET_AS_KERB_CRED;
//    //ticketQuery.TicketFlags = ticket.TicketFlags;
//    ////ticketQuery.TargetName = ticket.ServerName;

//    //string targetName2 = "krbtgt/TESTLAB.LOCAL";
//    //LSA_STRING_IN LSAString2;
//    //LSAString2.Length = (ushort)targetName2.Length;
//    //LSAString2.MaximumLength = (ushort)(targetName2.Length + 1);
//    //LSAString2.Buffer = targetName2;
//    //ticketQuery.TargetName = LSAString2;

//    //Console.WriteLine("flags: {0}\r\n", ticket.TicketFlags.ToString("X2"));

//    //IntPtr responsePointer = IntPtr.Zero;
//    //int returnBufferLength2 = 0;
//    //// query LSA, specifying we want the the specified ticket data
//    //retCode = LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(lsaHandle, authPack, ref ticketQuery, Marshal.SizeOf(ticketQuery), out responsePointer, out returnBufferLength2, out protocalStatus);
//    //Console.WriteLine("LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT retCode: {0}", retCode);
//    //Console.WriteLine("returnBufferLength: {0}", returnBufferLength2);
//    //Console.WriteLine("responsePointer: {0}\r\n", responsePointer);
//    //// parse the returned pointer into our initial KERB_RETRIEVE_TKT_RESPONSE structure

//    //response = (KERB_RETRIEVE_TKT_RESPONSE)Marshal.PtrToStructure((System.IntPtr)responsePointer, typeof(KERB_RETRIEVE_TKT_RESPONSE));

//    //KERB_EXTERNAL_NAME serviceNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ServiceName, typeof(KERB_EXTERNAL_NAME));
//    //string serviceName = Marshal.PtrToStringUni(serviceNameStruct.Names.Buffer, serviceNameStruct.Names.Length / 2).Trim();

//    //string targetName = "";
//    //if (response.Ticket.TargetName != IntPtr.Zero)
//    //{
//    //    KERB_EXTERNAL_NAME targetNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.TargetName, typeof(KERB_EXTERNAL_NAME));
//    //    targetName = Marshal.PtrToStringUni(targetNameStruct.Names.Buffer, targetNameStruct.Names.Length / 2).Trim();
//    //}

//    //KERB_EXTERNAL_NAME clientNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ClientName, typeof(KERB_EXTERNAL_NAME));
//    ////string clientName = Marshal.PtrToStringUni(clientNameStruct.Names.Buffer, clientNameStruct.Names.Length / 2).Trim();

//    //string domainName = Marshal.PtrToStringUni(response.Ticket.DomainName.Buffer, response.Ticket.DomainName.Length / 2).Trim();
//    //string targetDomainName = Marshal.PtrToStringUni(response.Ticket.TargetDomainName.Buffer, response.Ticket.TargetDomainName.Length / 2).Trim();
//    //string altTargetDomainName = Marshal.PtrToStringUni(response.Ticket.AltTargetDomainName.Buffer, response.Ticket.AltTargetDomainName.Length / 2).Trim();

//    //// extract the session key
//    //KERB_ENCRYPTION_TYPE sessionKeyType = (KERB_ENCRYPTION_TYPE)response.Ticket.SessionKey.KeyType;
//    //Int32 sessionKeyLength = response.Ticket.SessionKey.Length;
//    //byte[] sessionKey = new byte[sessionKeyLength];
//    //Marshal.Copy(response.Ticket.SessionKey.Value, sessionKey, 0, sessionKeyLength);
//    //string base64SessionKey = Convert.ToBase64String(sessionKey);

//    //DateTime keyExpirationTime = DateTime.FromFileTime(response.Ticket.KeyExpirationTime);
//    //DateTime startTime2 = DateTime.FromFileTime(response.Ticket.StartTime);
//    //DateTime endTime2 = DateTime.FromFileTime(response.Ticket.EndTime);
//    //DateTime renewUntil = DateTime.FromFileTime(response.Ticket.RenewUntil);
//    //Int64 timeSkew = response.Ticket.TimeSkew;
//    //Int32 encodedTicketSize = response.Ticket.EncodedTicketSize;

//    //string ticketFlags2 = ((KERB_TICKET_FLAGS)response.Ticket.TicketFlags).ToString();

//    //// extract the ticket and base64 encode it
//    //byte[] encodedTicket = new byte[encodedTicketSize];
//    //Marshal.Copy(response.Ticket.EncodedTicket, encodedTicket, 0, encodedTicketSize);
//    //string base64Ticket = Convert.ToBase64String(encodedTicket);

//    //Console.WriteLine("  ServiceName              : {0}", serviceName);
//    //Console.WriteLine("  TargetName               : {0}", targetName);
//    //Console.WriteLine("  ClientName               : {0}", clientName);
//    //Console.WriteLine("  DomainName               : {0}", domainName);
//    //Console.WriteLine("  TargetDomainName         : {0}", targetDomainName);
//    //Console.WriteLine("  AltTargetDomainName      : {0}", altTargetDomainName);
//    //Console.WriteLine("  SessionKeyType           : {0}", sessionKeyType);
//    //Console.WriteLine("  Base64SessionKey         : {0}", base64SessionKey);
//    //Console.WriteLine("  KeyExpirationTime        : {0}", keyExpirationTime);
//    //Console.WriteLine("  TicketFlags              : {0}", ticketFlags2);
//    //Console.WriteLine("  StartTime                : {0}", startTime2);
//    //Console.WriteLine("  EndTime                  : {0}", endTime2);
//    //Console.WriteLine("  RenewUntil               : {0}", renewUntil);
//    //Console.WriteLine("  EncodedTicketSize        : {0}", encodedTicketSize);
//    //Console.WriteLine("  Base64EncodedTicket      :\r\n");
//    //// display the TGT, columns of 80 chararacters
//    //foreach (string line in Split(base64Ticket, 80))
//    //{
//    //    Console.WriteLine("    {0}", line);
//    //}
//    //Console.WriteLine();
//    //}

//    // disconnect from LSA
//    LsaDeregisterLogonProcess(lsaHandle);
//    //}
//    //catch (Exception ex)
//    //{
//    //    Console.WriteLine("  [X] Exception: {0}", ex.Message);
//    //}
//}
