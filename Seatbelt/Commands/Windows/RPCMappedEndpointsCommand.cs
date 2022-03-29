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

        private const uint RPC_X_NO_MORE_ENTRIES = 1772;

        public RPCMappedEndpointsCommand(Runtime runtime) : base(runtime)
        {
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            // Ref - https://stackoverflow.com/questions/21805038/how-do-i-pinvoke-rpcmgmtepeltinqnext

            uint retCode; // RPC_S_OK      
            uint status; // RPC_S_OK      


            var ifId = new RPC_IF_ID();
            string host = null;

            if (args.Length >= 1)
            {
                // if we're specifying a remote host via arguments
                host = args[0];
            }

            // built the RPC binding string we're going to use
            retCode = RpcStringBindingCompose(null, "ncacn_ip_tcp", host, null, null, out var stringBinding);
            if (retCode != 0)
            {
                WriteError($"Bad return value from RpcStringBindingCompose : {retCode}");
                yield break;
            }

            // create the actual RPC binding (from the binding string)
            retCode = RpcBindingFromStringBinding(stringBinding, out var bindingHandle);
            if (retCode != 0)
            {
                WriteError($"Bad return value from RpcBindingFromStringBinding : {retCode}");
                yield break;
            }

            // create an inquiry context for viewing the elements in an endpoint map
            retCode = RpcMgmtEpEltInqBegin(bindingHandle, 0, 0, 0, 0, out var inquiryContext);
            if (retCode != 0)
            {
                WriteError($"Bad return value from RpcMgmtEpEltInqBegin : {retCode}");
                yield break;
            }

            do
            {
                // iterate through all of the elements in the RPC endpoint map
                status = RpcMgmtEpEltInqNext(inquiryContext, ref ifId, out var elementBinding, 0, out var elementAnnotation);

                if (status == RPC_X_NO_MORE_ENTRIES)
                {
                    break;
                }
                else if (status != 0)
                {
                    Console.WriteLine($"RpcMgmtEpEltInqNext failed. Error code: {status}");
                    break;
                }

                string annotation = elementAnnotation.ToString();
                string binding = elementBinding.ToString();

                yield return new RPCMappedEndpointsDTO(
                    ifId.Uuid,
                    annotation,
                    binding,
                    new Version(ifId.VersMajor, ifId.VersMinor)
                );
            }
            while (status == 0);
        }
    }

    public class RPCMappedEndpointsDTO : CommandDTOBase
    {
        public RPCMappedEndpointsDTO(Guid interfaceId, string annotation, string bindingString, Version version)
        {
            InterfaceId = interfaceId;
            Annotation = annotation;
            BindingString = bindingString;
            Version = version;
        }
        public Guid InterfaceId { get; set; }
        public string Annotation { get; set; }
        public string BindingString { get; set; }
        public Version Version { get; internal set; }
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

            WriteLine($"  {dto.InterfaceId} v{dto.Version} ({dto.Annotation}): {dto.BindingString}");
        }
    }
}
#nullable enable