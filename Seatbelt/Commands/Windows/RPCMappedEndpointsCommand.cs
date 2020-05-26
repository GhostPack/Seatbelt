#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Seatbelt.Output.Formatters;
using static Seatbelt.Interop.Rpcrt4;
using Seatbelt.Output.TextWriters;


namespace Seatbelt.Commands.Windows
{
    internal class RPCMappedEndpointsCommand : CommandBase
    {
        public override string Command => "RPCMappedEndpoints";
        public override string Description => "Current RPC endpoints mapped";
        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false;

        public RPCMappedEndpointsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // Ref - https://stackoverflow.com/questions/21805038/how-do-i-pinvoke-rpcmgmtepeltinqnext

            uint retCode; // RPC_S_OK      
            uint status; // RPC_S_OK      

            var bindingHandle = IntPtr.Zero;
            var inquiryContext = IntPtr.Zero;
            var ifId = new RPC_IF_ID();
            string host = null;

            if (args.Length >= 1)
            {
                // if we're specifying a remote host via arguments
                host = args[0];
            }

            try
            {
                // built the RPC binding string we're going to use
                retCode = RpcStringBindingCompose(null, "ncacn_ip_tcp", host, null, null, out var stringBinding);
                if (retCode != 0)
                {
                    WriteError($"Bad return value from RpcStringBindingCompose : {retCode}");
                    yield break;
                }

                // create the actual RPC binding (from the binding string)
                retCode = RpcBindingFromStringBinding(stringBinding, out bindingHandle);
                if (retCode != 0)
                {
                    WriteError($"Bad return value from RpcBindingFromStringBinding : {retCode}");
                    yield break;
                }

                // create an inquiry context for viewing the elements in an endpoint map
                retCode = RpcMgmtEpEltInqBegin(bindingHandle, 0, 0, 0, 0, out inquiryContext);
                if (retCode != 0)
                {
                    WriteError($"Bad return value from RpcMgmtEpEltInqBegin : {retCode}");
                    yield break;
                }

                var prev = new Guid();
                var result = new RPCMappedEndpointsDTO();
                result.Elements = new List<string>();

                do
                {
                    // iterate through all of the elements in the RPC endpoint map
                    status = RpcMgmtEpEltInqNext(inquiryContext, ref ifId, out var elementBindingHandle, 0, out var elementAnnotation);

                    if (status == 0)
                    {
                        if (ifId.Uuid != prev)
                        {
                            if (prev != new Guid())
                            {

                                var result2 = new RPCMappedEndpointsDTO();
                                result2 = result;
                                result = new RPCMappedEndpointsDTO();
                                result.Elements = new List<string>();

                                yield return result2;
                            }

                            var annotation = Marshal.PtrToStringAuto(elementAnnotation);
                            result.UUID = ifId.Uuid;
                            result.Annotation = annotation;

                            if (!String.IsNullOrEmpty(annotation))
                            {
                                RpcStringFree(ref elementAnnotation);
                            }

                            prev = ifId.Uuid;
                        }
                        if (elementBindingHandle != IntPtr.Zero)
                        {
                            var stringBinding2 = IntPtr.Zero;
                            status = RpcBindingToStringBinding(elementBindingHandle, out stringBinding2);

                            if (status == 0)
                            {
                                var stringBindingStr = Marshal.PtrToStringAuto(stringBinding2);
                                result.Elements.Add(stringBindingStr);

                                RpcStringFree(ref stringBinding2);
                                RpcBindingFree(ref elementBindingHandle);
                            }
                            else
                            {
                                // throw new Exception("[X] RpcBindingToStringBinding: " + retCode);
                            }
                        }
                    }
                }
                while (status == 0);

                yield return result;
            }
            finally
            {
                retCode = RpcMgmtEpEltInqDone(ref inquiryContext);
                if (retCode != 0)
                {
                    WriteError($"Bad return value from RpcMgmtEpEltInqDone : {retCode}");
                }

                retCode = RpcBindingFree(ref bindingHandle);
                if (retCode != 0)
                {
                    WriteError($"Bad return value from RpcBindingFree : {retCode}");
                }
            }
        }
    }

    internal class RPCMappedEndpointsDTO : CommandDTOBase
    {
        public Guid UUID { get; set; }

        public string Annotation { get; set; }

        public List<string> Elements { get; set; }
    }

    [CommandOutputType(typeof(RPCMappedEndpointsDTO))]
    internal class RPCMappedEndpointsTextFormatter : TextFormatterBase
    {
        public RPCMappedEndpointsTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (RPCMappedEndpointsDTO)result;

            if (!String.IsNullOrEmpty(dto.Annotation))
            {
                WriteLine(" UUID: {0} ({1})", dto.UUID, dto.Annotation);
            }
            else
            {
                WriteLine(" UUID: {0}", dto.UUID);
            }

            foreach (var element in dto.Elements)
            {
                WriteLine("     {0}", element);
            }
        }
    }
}
#nullable enable