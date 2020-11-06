#nullable disable
using System;
using System.Security.AccessControl;
using System.Collections.Generic;
using System.IO;

namespace Seatbelt.Commands.Windows
{
    internal class EnvironmentPathCommand : CommandBase
    {
        public override string Command => "EnvironmentPath";
        public override string Description => "Current environment %PATH$ folders and SDDL information";
        public override CommandGroup[] Group => new[] {CommandGroup.System};
        public override bool SupportRemote => false; // doesn't make sense to implement as we have "EnvironmentVariables" for remote hosts

        public EnvironmentPathCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var pathString = Environment.GetEnvironmentVariable("Path");
            var paths = pathString.Split(';');

            foreach(var path in paths)
            {
                var SDDL = "";

                if(!String.IsNullOrEmpty(path.Trim()))
                {
                    try
                    {
                        var security = Directory.GetAccessControl(path, AccessControlSections.Owner | AccessControlSections.Access);
                        SDDL = security.GetSecurityDescriptorSddlForm(AccessControlSections.Owner | AccessControlSections.Access);
                    }
                    catch
                    {
                        // eat it
                    }

                    yield return new EnvironmentPathDTO()
                    {
                        Name = path.Trim(),
                        SDDL = SDDL
                    };
                }
            }
        }

        internal class EnvironmentPathDTO : CommandDTOBase
        {
            public string Name { get; set; }
            public string SDDL { get; set; }
        }
    }
}
#nullable enable