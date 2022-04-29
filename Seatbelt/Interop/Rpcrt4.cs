using Microsoft.Win32.SafeHandles;
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
            out SafeRpcStringHandle StringBinding);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcBindingFromStringBinding(
            SafeRpcStringHandle StringBinding,
            out SafeRpcBindingHandle Binding);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern int RpcBindingToStringBinding(IntPtr Binding, out SafeRpcStringHandle StringBinding);


        // REGION: enumeration

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcMgmtEpEltInqBegin(
            SafeRpcBindingHandle EpBinding,
            int InquiryType, // 0x00000000 = RPC_C_EP_ALL_ELTS
            int IfId, // going to be 0/NULL, so we don't care about "ref RPC_IF_ID IfId"
            int VersOption,
            int ObjectUuid, // going to be 0/NULL, so we don't care about "ref RPC_IF_ID IfId"
            out SafeRpcInquiryHandle InquiryContext);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Unicode)]
        public static extern uint RpcMgmtEpEltInqNext(
            SafeRpcInquiryHandle InquiryContext,
            ref RPC_IF_ID IfId,
            out SafeRpcBindingHandle Binding,
            int ObjectUuid, // going to be 0/NULL, so we don't care about "ref RPC_IF_ID IfId"
            out SafeRpcStringHandle Annotation
        );


        // REGION: cleanup

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcStringFree(ref IntPtr String);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcMgmtEpEltInqDone(ref IntPtr InquiryContext);

        [DllImport("rpcrt4.dll", CharSet = CharSet.Auto)]
        public static extern uint RpcBindingFree(ref IntPtr Binding);


        // REGION: structures

        public struct RPC_IF_ID
        {
            public Guid Uuid;
            public ushort VersMajor;
            public ushort VersMinor;
        }

        // REGION: classes


        // From https://github.com/googleprojectzero/sandbox-attacksurface-analysis-tools/blob/c02ed8ba04324e54a0a188ab9877ee6aa372dfac/NtApiDotNet/Win32/SafeHandles/SafeRpcInquiryHandle.cs
        public class SafeRpcInquiryHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeRpcInquiryHandle() : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return Rpcrt4.RpcMgmtEpEltInqDone(ref handle) == 0;
            }
        }

        // From https://github.com/googleprojectzero/sandbox-attacksurface-analysis-tools/blob/c02ed8ba04324e54a0a188ab9877ee6aa372dfac/NtApiDotNet/Win32/SafeHandles/SafeRpcStringHandle.cs
        public class SafeRpcStringHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeRpcStringHandle() : base(true)
            {
            }

            public SafeRpcStringHandle(IntPtr handle, bool owns_handle) : base(owns_handle)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return Rpcrt4.RpcStringFree(ref handle) == 0;
            }

            public override string ToString()
            {
                if (!IsInvalid && !IsClosed)
                {
                    return Marshal.PtrToStringUni(handle);
                }
                return string.Empty;
            }
        }

        // From https://github.com/googleprojectzero/sandbox-attacksurface-analysis-tools/blob/c02ed8ba04324e54a0a188ab9877ee6aa372dfac/NtApiDotNet/Win32/SafeHandles/SafeRpcBindingHandle.cs
        public class SafeRpcBindingHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeRpcBindingHandle() : base(true)
            {
            }

            public SafeRpcBindingHandle(IntPtr handle, bool owns_handle) : base(owns_handle)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return Rpcrt4.RpcBindingFree(ref handle) == 0;
            }

            public override string ToString()
            {
                if (!IsInvalid && !IsClosed)
                {
                    if (Rpcrt4.RpcBindingToStringBinding(handle, out SafeRpcStringHandle str) == 0)
                    {
                        using (str)
                        {
                            return str.ToString();
                        }
                    }
                }
                return string.Empty;
            }

            public static SafeRpcBindingHandle Null => new SafeRpcBindingHandle(IntPtr.Zero, false);
        }
    }
}
