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

                string[] googleCredLocations = {    $"{dir}\\AppData\\Roaming\\gcloud\\credentials.db",
                                                    $"{dir}\\AppData\\Roaming\\gcloud\\legacy_credentials",
                                                    $"{dir}\\AppData\\Roaming\\gcloud\\access_tokens.db"};
                
                foreach(var googleCredLocation in googleCredLocations)
                {
                    if (File.Exists(googleCredLocation))
                    {
                        var lastAccessed = File.GetLastAccessTime(googleCredLocation);
                        var lastModified = File.GetLastWriteTime(googleCredLocation);
                        var size = new FileInfo(googleCredLocation).Length;

                        yield return new CloudCredentialsDTO()
                        {
                            Type = "Google",
                            FileName = googleCredLocation,
                            LastAccessed = lastAccessed,
                            LastModified = lastModified,
                            Size = size
                        };
                    }
                }

                string[] azureCredLocations = {     $"{dir}\\.azure\\azureProfile.json",
                                                    $"{dir}\\.azure\\TokenCache.dat",
                                                    $"{dir}\\.azure\\AzureRMContext.json",
                                                    $"{dir}\\AppData\\Roaming\\Windows Azure Powershell\\TokenCache.dat",
                                                    $"{dir}\\AppData\\Roaming\\Windows Azure Powershell\\AzureRMContext.json" };

                foreach (var azureCredLocation in azureCredLocations)
                {
                    if (File.Exists(azureCredLocation))
                    {
                        var lastAccessed = File.GetLastAccessTime(azureCredLocation);
                        var lastModified = File.GetLastWriteTime(azureCredLocation);
                        var size = new FileInfo(azureCredLocation).Length;

                        yield return new CloudCredentialsDTO()
                        {
                            Type = "Azure",
                            FileName = azureCredLocation,
                            LastAccessed = lastAccessed,
                            LastModified = lastModified,
                            Size = size
                        };
                    }
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