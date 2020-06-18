#nullable disable
using Seatbelt.Commands;
using Seatbelt.Interop;
using Seatbelt.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Seatbelt.Output.Sinks;
using System.Management;
using Microsoft.Win32;

namespace Seatbelt
{
    internal class Runtime
    {
        public List<CommandBase> AllCommands { get; private set; } = new List<CommandBase>();
        public readonly IOutputSink OutputSink;
        private IEnumerable<string> Commands { get; set; }
        private IEnumerable<string> CommandGroups { get; set; }
        public bool FilterResults { get; }
        public string ComputerName { get; }  // for remote connections
        private string UserName { get; }     // for remote connections
        private string Password { get; }     // for remote connections
        private ManagementClass wmiRegProv { get; }

        public Runtime(IOutputSink outputSink, IEnumerable<string> commands, IEnumerable<string> commandGroups, bool filter)
            : this(outputSink, commands, commandGroups, filter, "", "", "")
        {
        }

        public Runtime(IOutputSink outputSink, IEnumerable<string> commands, IEnumerable<string> commandGroups, bool filter, string computerName)
            : this(outputSink, commands, commandGroups, filter, computerName, "", "")
        {
        }

        public Runtime(IOutputSink outputSink, IEnumerable<string> commands, IEnumerable<string> commandGroups, bool filter, string computerName, string userName, string password)
        {
            OutputSink = outputSink;
            Commands = commands;
            CommandGroups = commandGroups;
            FilterResults = filter;
            ComputerName = computerName;
            UserName = userName;
            Password = password;

            // test a remote connection first if a remote system is specified
            if (!string.IsNullOrEmpty(computerName))
            {
                try
                {
                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                    {
                        OutputSink.WriteHost($"[*] Running commands remotely against the host '{computerName}' with credentials -> user:{UserName} , password:{Password}\r\n");

                        var options = new ConnectionOptions();
                        options.Username = UserName;
                        options.Password = Password;
                        options.Impersonation = ImpersonationLevel.Impersonate;
                        options.EnablePrivileges = true;

                        var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2", options);
                        scope.Connect();
                    }
                    else
                    {
                        OutputSink.WriteHost($"[*] Running commands remotely against the host '{computerName}' with current user credentials\r\n");

                        var scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
                        scope.Connect();
                    }
                    InitializeCommands();
                }
                catch (Exception e)
                {
                    OutputSink.WriteError($"Error connecting to \"{computerName}\" : {e.Message}");
                    Environment.Exit(1);
                }

                wmiRegProv = WMIUtil.WMIRegConnection(computerName, userName, password);
            }
            else
            {
                InitializeCommands();
            }
        }

        public ManagementObjectSearcher GetManagementObjectSearcher(string nameSpace, string query)
        {

            // helper that takes the current configuration for a remote management and builds the proper ManagementObjectSearcher object
            // used for WMI searching

            if (string.IsNullOrEmpty(ComputerName))
                return new ManagementObjectSearcher(nameSpace, query);

            try
            {
                if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                {
                    var options = new ConnectionOptions
                    {
                        Username = UserName,
                        Password = Password,
                        Impersonation = ImpersonationLevel.Impersonate,
                        EnablePrivileges = true
                    };

                    var scope = new ManagementScope($"\\\\{ComputerName}\\{nameSpace}", options);
                    scope.Connect();
                    var queryObj = new ObjectQuery(query);
                    return new ManagementObjectSearcher(scope, queryObj);
                }
                else
                {
                    var scope = new ManagementScope($"\\\\{ComputerName}\\{nameSpace}");
                    scope.Connect();
                    var queryObj = new ObjectQuery(query);
                    return new ManagementObjectSearcher(scope, queryObj);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error connecting to \"{ComputerName}\" : {e.Message}");
            }
        }

        public string[]? GetSubkeyNames(RegistryHive hive, string path)
        {
            if (!string.IsNullOrEmpty(ComputerName))
            {
                return RegistryUtil.GetSubkeyNames(hive, path, wmiRegProv);
            }

            return RegistryUtil.GetSubkeyNames(hive, path);
        }

        public string? GetStringValue(RegistryHive hive, string path, string value)
        {
            if (!string.IsNullOrEmpty(ComputerName))
            {
                return RegistryUtil.GetStringValue(hive, path, value, wmiRegProv);
            }

            return RegistryUtil.GetStringValue(hive, path, value);
        }

        public uint? GetDwordValue(RegistryHive hive, string path, string value)
        {
            if (!string.IsNullOrEmpty(ComputerName))
            {
                return RegistryUtil.GetDwordValue(hive, path, value, wmiRegProv);
            }

            return RegistryUtil.GetDwordValue(hive, path, value);
        }


        public byte[]? GetBinaryValue(RegistryHive hive, string path, string value)
        {
            if (!string.IsNullOrEmpty(ComputerName))
            {
                return RegistryUtil.GetBinaryValue(hive, path, value, wmiRegProv);
            }

            return RegistryUtil.GetBinaryValue(hive, path, value);
        }

        public Dictionary<string, object> GetValues(RegistryHive hive, string path)
        {
            if (!string.IsNullOrEmpty(ComputerName))
            {
                return RegistryUtil.GetValues(hive, path, wmiRegProv);
            }

            return RegistryUtil.GetValues(hive, path);
        }

        public string[] GetUserSIDs()
        {
            if (!string.IsNullOrEmpty(ComputerName))
            {
                return RegistryUtil.GetUserSIDs(wmiRegProv);
            }

            return RegistryUtil.GetUserSIDs();
        }

        public bool ISRemote()
        {
            return !string.IsNullOrEmpty(ComputerName);
        }

        private void InitializeCommands()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsSubclassOf(typeof(CommandBase)) || type.IsAbstract)
                {
                    continue;
                }

                var instance = (CommandBase)Activator.CreateInstance(type, new object[] { this });

#if DEBUG
                if (instance.Command != "TEMPLATE")
                {
                    AllCommands.Add(instance);
                }
#else
                AllCommands.Add(instance);
#endif
            }

            AllCommands = AllCommands.OrderBy(c => c.Command).ToList();
        }


        public void Execute()
        {
            foreach (var group in CommandGroups)
            {
                if (!ProcessGroup(group))
                {
                    OutputSink.WriteError($"Invalid command group \"{group}\"");
                }
            }

            foreach (var command in Commands)
            {
                try
                {
                    if (!ProcessCommand(command))
                    {
                        OutputSink.WriteError($"Error running command \"{command}\"");
                    }
                }
                catch (Exception e)
                {
                    OutputSink.WriteError($"Error running {command}: {e}");

                }
            }
        }


        private bool ProcessGroup(string command)
        {
            var commandGroupStrings = Enum.GetNames(typeof(CommandGroup)).ToList().Select(g=> g.ToLower());

            if (!commandGroupStrings.Contains(command.ToLower()))
                return false;

            List<CommandBase> toExecute;

            switch (command.ToLower())
            {
                case "all":
                    toExecute = AllCommands;
                    break;

                default:
                    CommandGroup commandGroup;
                    try
                    {
                        var groupName = Enum.GetNames(typeof(CommandGroup)).FirstOrDefault(c => c.ToLower() == command.ToLower());
                        commandGroup = (CommandGroup)Enum.Parse(typeof(CommandGroup), groupName);
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }

                    toExecute = AllCommands.Where(g => g.Group.Contains(commandGroup)).ToList();
                    break;
            }

            toExecute.ForEach(c =>
            {
                ExecuteCommand(c, new string[] { });
            });

            return true;
        }


        private bool ProcessCommand(string commandLine)
        {
            var args = Shell32.CommandLineToArgs(commandLine);

            var commandName = args[0];
            var command = AllCommands.FirstOrDefault(c => c.Command.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

            if (command == null)
            {
                return false;
            }

            var commandArgs = new string[] { };
            if (args.Length > 1)
            {
                commandArgs = args.SubArray(1, args.Length - 1);
            }

            ExecuteCommand(command, commandArgs);

            return true;
        }


        private void ExecuteCommand(CommandBase? command, string[] commandArgs)
        {
            try
            {
                OutputSink.WriteOutput(new HostDTO($"====== {command.Command} ======\n"));
                var results = command.Execute(commandArgs);
                if (results != null)
                {
                    // OutputSink.BeginOutput();
                    foreach (var result in results)
                    {
                        OutputSink.WriteOutput(result);
                    }
                    // OutputSink.EndOutput();
                }
            }
            catch (Exception e)
            {
                // TODO: Return an error DTO
                OutputSink.WriteError($"  [!] Terminating exception running command '{command.Command}': " + e);
            }
        }
    }
}
#nullable enable