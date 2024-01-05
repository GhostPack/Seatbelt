namespace Seatbelt.Interop
{
    // Defined at https://msdn.microsoft.com/en-us/library/cc231199.aspx
    // Only list error codes that are actually used in our code
    internal class Win32Error
    {
        public const int InvalidHandle        = -1;
        public const int Success              = 0;
        public const int NERR_Success         = 0;
        public const int AccessDenied         = 0x0000005;
        public const int NotEnoughMemory      = 0x00000008;
        public const int InsufficientBuffer   = 0x0000007A;
        public const int MoreData             = 0x00000EA;
        public const int NoSuchAlias          = 0x0000560;
        public const int RpcServerUnavailable = 0x0006BA;
        public const int NERR_GroupNotFound   = 0x00008AC;
        public const int NERR_InvalidComputer = 0x000092F;
    }
}
