#nullable disable
using System;
using System.Collections.Generic;
using static Seatbelt.Interop.Secur32;
using System.Runtime.InteropServices;

namespace Seatbelt.Commands.Windows
{
    internal class SecurityPackagesCommand : CommandBase
    {
        public override string Command => "SecurityPackages"; 
        public override string Description => "Enumerates the security packages currently available using EnumerateSecurityPackagesA()"; 
        public override CommandGroup[] Group => new[] {CommandGroup.Misc};
        public override bool SupportRemote => false;

        public SecurityPackagesCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // this code was partially adapted from Chris Haas' post at https://stackoverflow.com/a/5941873

            WriteHost("Security Packages\n\n");

            var securityPackages = new List<SecurityPackagesDTO>();

            var ret = EnumerateSecurityPackages(out var pcPackages, out var ppPackageInfo);

            var ppPackageInfoItr = ppPackageInfo;
            
            for (ulong i = 0; i < pcPackages; i++)
            {
                var packageInfo = (SecPkgInfo)Marshal.PtrToStructure(ppPackageInfoItr, typeof(SecPkgInfo));

                var securityPackage = new SecurityPackagesDTO()
                {
                    Name = packageInfo.Name.ToString(),
                    Comment = packageInfo.Comment.ToString(),
                    Capabilities = packageInfo.fCapabilities,
                    MaxToken = packageInfo.cbMaxToken,
                    RPCID = packageInfo.wRPCID,
                    Version = packageInfo.wVersion
                };

                securityPackages.Add(securityPackage);

                ppPackageInfoItr = (IntPtr)((long)ppPackageInfoItr.ToInt64() + Marshal.SizeOf(typeof(SecPkgInfo)));
            }

            foreach (var securityPackage in securityPackages)
            {
                yield return securityPackage;
            }

            FreeContextBuffer(ppPackageInfo);
        }

        internal class SecurityPackagesDTO : CommandDTOBase
        {
            public string Name { get; set; }
            public string Comment { get; set; }
            public SECPKG_FLAGS Capabilities { get; set; }
            public uint MaxToken { get; set; }
            public short RPCID { get; set; }
            public short Version { get; set; }
        }
    }
}
#nullable enable