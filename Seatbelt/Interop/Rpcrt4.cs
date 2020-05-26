using System;
using System.Runtime.InteropServices;

namespace Seatbelt.Interop
{
    internal class Rpcrt4
    {
        // Ref - https://github.com/microsoft/WindowsProtocolTestSuites/blob/e4f325ce2ecbdecaa1db7190c37a7941a28eb0e5/ProtoSDK/Common/Rpc/RpcNativeMethods.cs#L326-L527

        // REGION: setup
        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcStringBindingCompose(
            string ObjUuid,
            string Protseq,
            string NetworkAddr,
            string Endpoint,
            string Options,
            out IntPtr StringBinding);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcBindingFromStringBinding(
            IntPtr StringBinding,
            out IntPtr Binding);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcBindingToStringBinding(
            IntPtr Binding,
            out IntPtr StringBinding);


        // REGION: enumeration

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcMgmtEpEltInqBegin(
            IntPtr EpBinding,
            int InquiryType, // 0x00000000 = RPC_C_EP_ALL_ELTS
            int IfId, // going to be 0/NULL, so we don't care about "ref RPC_IF_ID IfId"
            int VersOption,
            int ObjectUuid, // going to be 0/NULL, so we don't care about "ref RPC_IF_ID IfId"
            out IntPtr InquiryContext);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcMgmtEpEltInqNext(
            IntPtr InquiryContext,
            ref RPC_IF_ID IfId,
            out IntPtr Binding,
            int ObjectUuid, // going to be 0/NULL, so we don't care about "ref RPC_IF_ID IfId"
            out IntPtr Annotation
        );


        // REGION: cleanup

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcStringFree(
            ref IntPtr String);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcMgmtEpEltInqDone(
            ref IntPtr InquiryContext);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcBindingFree(
            ref IntPtr Binding);


        // REGION: structures

        public struct RPC_IF_ID
        {
            public Guid Uuid;
            public ushort VersMajor;
            public ushort VersMinor;
        }
    }
}
