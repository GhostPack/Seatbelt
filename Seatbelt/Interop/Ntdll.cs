using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace Seatbelt.Interop
{
    internal class Ntdll
    {
        #region Function Definitions
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        PROCESSINFOCLASS processInformationClass,
        ref PsProtection processInformation,
        int processInformationLength,
        out int returnLength);
    }
    #endregion

    #region Enum Definitions
    [Flags]
    public enum PROCESSINFOCLASS
    {
        ProcessProtectionInformation = 0x3D,
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PsProtection
    {
        public PsProtectedType Type;
        public PsProtectedSigner Signer;
        public PsProtectedAudit Audit;
    }
    [Flags]
    public enum PsProtectedType
    {
        PsProtectedTypeNone = 0x0,
        PsProtectedTypeProtectedLight = 0x1,
        PsProtectedTypeProtected = 0x2,
        PsProtectedTypeMax = 0x3,
    }
    [Flags]
    public enum PsProtectedSigner
    {
        PsProtectedSignerNone = 0x0,
        PsProtectedSignerAuthenticode = 0x1,
        PsProtectedSignerCodeGen = 0x2,
        PsProtectedSignerAntimalware = 0x3,
        PsProtectedSignerLsa = 0x4,
        PsProtectedSignerWindows = 0x5,
        PsProtectedSignerWinTcb = 0x6,
        PsProtectedSignerMax = 0x7,
    }

    public enum PsProtectedAudit
    {
        None = 0x0
    }

    [Flags]
    public enum ProtectionValueName
    {
        PsProtectedTypeNone = 0,
        PsProtectedSignerAuthenticodeLight = 11,
        PsProtectedSignerCodeGenLight = 21,
        PsProtectedSignerAntimalwareLight = 31,
        PsProtectedSignerLsaLight = 41,
        PsProtectedSignerWindowsLight = 51,
        PsProtectedSignerWinTcbLight = 61,
        PsProtectedSignerMaxLight = 71,
        PsProtectedSignerAuthenticode = 12,
        PsProtectedSignerCodeGen = 22,
        PsProtectedSignerAntimalware = 32,
        PsProtectedSignerLsa = 42,
        PsProtectedSignerWindows = 52,
        PsProtectedSignerWinTcb = 62,
        PsProtectedSignerMax = 72,
        PsProtectedSignerAuthenticodeMax = 13,
        PsProtectedSignerCodeGenMax = 23,
        PsProtectedSignerAntimalwareMax = 33,
        PsProtectedSignerLsaMax = 43,
        PsProtectedSignerWindowsMax = 53,
        PsProtectedSignerWinTcbMax = 63,
        PsProtectedSignerMaxMax = 73
    }
    #endregion
}


