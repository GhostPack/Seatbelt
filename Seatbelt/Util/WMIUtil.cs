using System;
using System.Management;

namespace Seatbelt.Util
{

    public class WMIUtil {

        public static ManagementClass WMIRegConnection()
        {
            return WMIRegConnection("");
        }

        public static ManagementClass WMIRegConnection(string computerName)
        {
            return WMIRegConnection(computerName, "", "");
        }

        public static ManagementClass WMIRegConnection(string computerName, string userName, string password)
        {
            ManagementScope scope;
            ConnectionOptions connection = new ConnectionOptions();
            connection.Impersonation = ImpersonationLevel.Impersonate;

            if (!String.IsNullOrEmpty(userName))
            {
                try
                {
                    if (userName.Contains("\\"))
                    {
                        string[] parts = userName.Split('\\');
                        connection.Username = parts[1];
                        connection.Authority = $"NTLMDOMAIN:{parts[0]}";
                    }
                    else
                    {
                        connection.Username = userName;
                    }
                    connection.Password = password;
                }
                catch
                {
                    // ?
                }
            }

            scope = new ManagementScope($"\\\\{computerName}\\root\\default", connection);
            scope.Connect();
            return new ManagementClass(scope, new ManagementPath("StdRegProv"), null);
        }
    }
}
