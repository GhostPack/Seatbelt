namespace Seatbelt.Commands
{
    public class CommandDTOBase
    {
        private string CommandVersion { get; set; }

        protected CommandDTOBase()
        {
            CommandVersion = "";
        }

        public void SetCommandVersion(string commandVersion)
        {
            CommandVersion = commandVersion;
        }
        
        public string GetCommandVersion()
        {
            return CommandVersion;
        }
    }
}