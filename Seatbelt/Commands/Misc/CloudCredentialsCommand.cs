using System;
using System.Collections.Generic;
using System.IO;

namespace Seatbelt.Commands
{
    internal class CloudCredentialsCommand : CommandBase
    {
        public override string Command => "CloudCredentials";
        public override string Description => "AWS/Google/Azure/Bluemix cloud credential files";
        public override CommandGroup[] Group => new[] {CommandGroup.User, CommandGroup.Remote};
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public CloudCredentialsCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // checks for various cloud credential files (AWS, Microsoft Azure, and Google Compute)
            // adapted from https://twitter.com/cmaddalena's SharpCloud project (https://github.com/chrismaddalena/SharpCloud/)

            var dirs = ThisRunTime.GetDirectories("\\Users\\");

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

                    yield return new CloudCredentialsDTO(
                        "AWS",
                        awsKeyFile,
                        lastAccessed,
                        lastModified,
                        size
                    );
                }

                string[] googleCredLocations = {    $"{dir}\\AppData\\Roaming\\gcloud\\credentials.db",
                                                    $"{dir}\\AppData\\Roaming\\gcloud\\legacy_credentials",
                                                    $"{dir}\\AppData\\Roaming\\gcloud\\access_tokens.db"};
                
                foreach(var googleCredLocation in googleCredLocations)
                {
                    if (!File.Exists(googleCredLocation)) continue;

                    var lastAccessed = File.GetLastAccessTime(googleCredLocation);
                    var lastModified = File.GetLastWriteTime(googleCredLocation);
                    var size = new FileInfo(googleCredLocation).Length;

                    yield return new CloudCredentialsDTO(
                        "Google",
                        googleCredLocation,
                        lastAccessed,
                        lastModified,
                        size
                    );
                }

                string[] azureCredLocations = {     $"{dir}\\.azure\\azureProfile.json",
                                                    $"{dir}\\.azure\\TokenCache.dat",
                                                    $"{dir}\\.azure\\AzureRMContext.json",
                                                    $"{dir}\\AppData\\Roaming\\Windows Azure Powershell\\TokenCache.dat",
                                                    $"{dir}\\AppData\\Roaming\\Windows Azure Powershell\\AzureRMContext.json" };

                foreach (var azureCredLocation in azureCredLocations)
                {
                    if (!File.Exists(azureCredLocation)) continue;

                    var lastAccessed = File.GetLastAccessTime(azureCredLocation);
                    var lastModified = File.GetLastWriteTime(azureCredLocation);
                    var size = new FileInfo(azureCredLocation).Length;

                    yield return new CloudCredentialsDTO(
                        "Azure",
                        azureCredLocation,
                        lastAccessed,
                        lastModified,
                        size
                    );
                }

                string[] bluemixCredLocations = {   $"{dir}\\.bluemix\\config.json",
                                                    $"{dir}\\.bluemix\\.cf\\config.json"};

                foreach (var bluemixCredLocation in bluemixCredLocations)
                {
                    if (!File.Exists(bluemixCredLocation)) continue;

                    var lastAccessed = File.GetLastAccessTime(bluemixCredLocation);
                    var lastModified = File.GetLastWriteTime(bluemixCredLocation);
                    var size = new FileInfo(bluemixCredLocation).Length;

                    yield return new CloudCredentialsDTO(
                        "Bluemix",
                        bluemixCredLocation,
                        lastAccessed,
                        lastModified,
                        size
                    );
                }
            }
        }

        internal class CloudCredentialsDTO : CommandDTOBase
        {
            public CloudCredentialsDTO(string type, string fileName, DateTime lastAccessed, DateTime lastModified, long size)
            {
                Type = type;
                FileName = fileName;
                LastAccessed = lastAccessed;
                LastModified = lastModified;
                Size = size;    
            }
            public string Type { get; }

            public string FileName { get; }

            public DateTime LastAccessed { get; }

            public DateTime LastModified { get; }

            public long Size { get; }
        }
    }
}