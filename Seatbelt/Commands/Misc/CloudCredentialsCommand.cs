#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace Seatbelt.Commands
{
    internal class CloudCredentialsCommand : CommandBase
    {
        public override string Command => "CloudCredentials";
        public override string Description => "AWS/Google/Azure cloud credential files";
        public override CommandGroup[] Group => new[] {CommandGroup.User};
        public override bool SupportRemote => false; // though I *really* want to figure an effective way to do this one remotely

        public CloudCredentialsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // checks for various cloud credential files (AWS, Microsoft Azure, and Google Compute)
            // adapted from https://twitter.com/cmaddalena's SharpCloud project (https://github.com/chrismaddalena/SharpCloud/)

            var userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
            var dirs = Directory.GetDirectories(userFolder);

            foreach (var dir in dirs)
            {
                if (dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") ||
                    dir.EndsWith("All Users"))
                {
                    continue;
                }

                var awsKeyFile = $"{dir}\\.aws\\credentials";
                if (File.Exists(awsKeyFile))
                {
                    var lastAccessed = File.GetLastAccessTime(awsKeyFile);
                    var lastModified = File.GetLastWriteTime(awsKeyFile);
                    var size = new FileInfo(awsKeyFile).Length;

                    yield return new CloudCredentialsDTO()
                    {
                        Type = "AWS",
                        FileName = awsKeyFile,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }

                var computeCredsDb = $"{dir}\\AppData\\Roaming\\gcloud\\credentials.db";
                if (File.Exists(computeCredsDb))
                {
                    var lastAccessed = File.GetLastAccessTime(computeCredsDb);
                    var lastModified = File.GetLastWriteTime(computeCredsDb);
                    var size = new FileInfo(computeCredsDb).Length;

                    yield return new CloudCredentialsDTO()
                    {
                        Type = "Google",
                        FileName = computeCredsDb,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }

                var computeLegacyCreds = $"{dir}\\AppData\\Roaming\\gcloud\\legacy_credentials";
                if (File.Exists(computeLegacyCreds))
                {
                    var lastAccessed = File.GetLastAccessTime(computeLegacyCreds);
                    var lastModified = File.GetLastWriteTime(computeLegacyCreds);
                    var size = new FileInfo(computeLegacyCreds).Length;

                    yield return new CloudCredentialsDTO()
                    {
                        Type = "Google",
                        FileName = computeLegacyCreds,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }

                var computeAccessTokensDb = $"{dir}\\AppData\\Roaming\\gcloud\\access_tokens.db";
                if (File.Exists(computeAccessTokensDb))
                {
                    var lastAccessed = File.GetLastAccessTime(computeAccessTokensDb);
                    var lastModified = File.GetLastWriteTime(computeAccessTokensDb);
                    var size = new FileInfo(computeAccessTokensDb).Length;

                    yield return new CloudCredentialsDTO()
                    {
                        Type = "Google",
                        FileName = computeAccessTokensDb,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }

                var azureTokens = $"{dir}\\.azure\\accessTokens.json";
                if (File.Exists(azureTokens))
                {
                    var lastAccessed = File.GetLastAccessTime(azureTokens);
                    var lastModified = File.GetLastWriteTime(azureTokens);
                    var size = new FileInfo(azureTokens).Length;

                    yield return new CloudCredentialsDTO()
                    {
                        Type = "Azure",
                        FileName = azureTokens,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }

                var azureProfile = $"{dir}\\.azure\\azureProfile.json";
                if (File.Exists(azureProfile))
                {
                    var lastAccessed = File.GetLastAccessTime(azureProfile);
                    var lastModified = File.GetLastWriteTime(azureProfile);
                    var size = new FileInfo(azureProfile).Length;

                    yield return new CloudCredentialsDTO()
                    {
                        Type = "Azure",
                        FileName = azureProfile,
                        LastAccessed = lastAccessed,
                        LastModified = lastModified,
                        Size = size
                    };
                }
            }
        }

        internal class CloudCredentialsDTO : CommandDTOBase
        {
            public string Type { get; set; }

            public string FileName { get; set; }

            public DateTime LastAccessed { get; set; }

            public DateTime LastModified { get; set; }

            public long Size { get; set; }
        }
    }
}
#nullable enable