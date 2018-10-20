using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Seatbelt.WindowsInterop;

namespace Seatbelt.Probes.SystemChecks
{
    public class FirewallRules  :IProbe
    {
        public static string ProbeName => "FirewallRules";

        public string List()
        {
            var sb = new StringBuilder();

            
            // lists local firewall policies and rules
            //      by default, only "deny" result are output unless "full" is passed

            if (FilterResults.Filter)
                sb.AppendProbeHeaderLine("Firewall Rules (Deny)");
            else
                sb.AppendProbeHeaderLine("Firewall Rules (All)");

            try
            {
                // GUID for HNetCfg.FwPolicy2 COM object
                var firewall = Type.GetTypeFromCLSID(new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD"));
                var firewallObj = Activator.CreateInstance(firewall);
                var types = firewallObj.GetType().InvokeMember("CurrentProfileTypes", BindingFlags.GetProperty, null, firewallObj, null);

                sb.AppendLine($"  Current Profile(s)          : {(FirewallProfiles)Int32.Parse(types.ToString())}").AppendLine();

                // NET_FW_PROFILE2_DOMAIN = 1, NET_FW_PROFILE2_PRIVATE = 2, NET_FW_PROFILE2_PUBLIC = 4
                var enabledDomain = firewallObj.GetType().InvokeMember("FirewallEnabled", BindingFlags.GetProperty, null, firewallObj, new object[] { 1 });
                sb.AppendLine($"  FirewallEnabled (Domain)    : {enabledDomain}");

                var enabledPrivate = firewallObj.GetType().InvokeMember("FirewallEnabled", BindingFlags.GetProperty, null, firewallObj, new object[] { 2 });
                sb.AppendLine($"  FirewallEnabled (Private)   : {enabledPrivate}");

                var enabledPublic = firewallObj.GetType().InvokeMember("FirewallEnabled", BindingFlags.GetProperty, null, firewallObj, new object[] { 4 });
                sb.AppendLine($"  FirewallEnabled (Public)    : {enabledPublic}");
                sb.AppendLine();


                // now grab all the rules
                var rules = firewallObj.GetType().InvokeMember("Rules", BindingFlags.GetProperty, null, firewallObj, null);

                // manually get the enumerator() method
                var enumerator = (IEnumerator)rules.GetType().InvokeMember("GetEnumerator", BindingFlags.InvokeMethod, null, rules, null);

                // move to the first item
                enumerator.MoveNext();

                var currentItem = enumerator.Current;

                while (currentItem != null)
                {
                    // only display enabled rules
                    var enabled = currentItem.GetType().InvokeMember("Enabled", BindingFlags.GetProperty, null, currentItem, null);
                    if (enabled.ToString() == "True")
                    {
                        var action = currentItem.GetType().InvokeMember("Action", BindingFlags.GetProperty, null, currentItem, null);
                        if ((FilterResults.Filter && (action.ToString() == "0")) || !FilterResults.Filter)
                        {
                            // extract all of our fields
                            var name = currentItem.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, currentItem, null);
                            var description = currentItem.GetType().InvokeMember("Description", BindingFlags.GetProperty, null, currentItem, null);
                            var protocol = currentItem.GetType().InvokeMember("Protocol", BindingFlags.GetProperty, null, currentItem, null);
                            var applicationName = currentItem.GetType().InvokeMember("ApplicationName", BindingFlags.GetProperty, null, currentItem, null);
                            var localAddresses = currentItem.GetType().InvokeMember("LocalAddresses", BindingFlags.GetProperty, null, currentItem, null);
                            var localPorts = currentItem.GetType().InvokeMember("LocalPorts", BindingFlags.GetProperty, null, currentItem, null);
                            var remoteAddresses = currentItem.GetType().InvokeMember("RemoteAddresses", BindingFlags.GetProperty, null, currentItem, null);
                            var remotePorts = currentItem.GetType().InvokeMember("RemotePorts", BindingFlags.GetProperty, null, currentItem, null);
                            var direction = currentItem.GetType().InvokeMember("Direction", BindingFlags.GetProperty, null, currentItem, null);
                            var profiles = currentItem.GetType().InvokeMember("Profiles", BindingFlags.GetProperty, null, currentItem, null);

                            var ruleAction = "ALLOW";
                            if (action.ToString() != "1")
                            {
                                ruleAction = "DENY";
                            }

                            var ruleDirection = "IN";
                            if (direction.ToString() != "1")
                            {
                                ruleDirection = "OUT";
                            }

                            var ruleProtocol = "TCP";
                            if (protocol.ToString() != "6")
                            {
                                ruleProtocol = "UDP";
                            }
                            // TODO: other protocols!

                            sb.AppendLine($"  Name                 : {name}");
                            sb.AppendLine($"  Description          : {description}");
                            sb.AppendLine($"  ApplicationName      : {applicationName}");
                            sb.AppendLine($"  Protocol             : {ruleProtocol}");
                            sb.AppendLine($"  Action               : {ruleAction}");
                            sb.AppendLine($"  Direction            : {ruleDirection}");
                            sb.AppendLine($"  Profiles             : {(FirewallProfiles)Int32.Parse(profiles.ToString())}");
                            sb.AppendLine($"  Local Addr:Port      : {localAddresses}:{localPorts}");
                            sb.AppendLine($"  Remote Addr:Port     : {remoteAddresses}:{remotePorts}");
                            sb.AppendLine();
                        }
                    }
                    // manually move the enumerator
                    enumerator.MoveNext();
                    currentItem = enumerator.Current;
                }
                Marshal.ReleaseComObject(firewallObj);
                firewallObj = null;
            }
            catch (Exception ex)
            {
                sb.AppendExceptionLine(ex);
            }

            return sb.ToString();
        }

    }
}
