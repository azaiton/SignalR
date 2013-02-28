﻿using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Compression
{
    public class PayloadCompressionModule : HubPipelineModule
    {
        private IPayloadCompressor _compressor;
        private IPayloadDescriptorProvider _provider;

        public PayloadCompressionModule()
            : this(compressor: null, provider: null)
        {
        }

        public PayloadCompressionModule(IPayloadCompressor compressor, IPayloadDescriptorProvider provider)
        {
            _compressor = compressor;
            _provider = provider;
        }

        public override Func<IHubIncomingInvokerContext, Task<object>> BuildIncoming(Func<IHubIncomingInvokerContext, Task<object>> invoke)
        {
            return base.BuildIncoming((context) =>
            {
                var result = invoke(context);
                var type = result.GetType();

                return result.Then(r => _compressor.Compress(r));
            });
        }

        public override Func<IHubOutgoingInvokerContext, Task> BuildOutgoing(Func<IHubOutgoingInvokerContext, Task> send)
        {
            return base.BuildOutgoing((context) => {
                var args = context.Invocation.Args;
                string[] contracts = new string[args.Length];

                for(var i = 0; i < args.Length; i++)
                {
                    long contractId = -1;
                    bool enumerable = false;
                    Type argType = args[i].GetType();
                    PayloadDescriptor descriptor;

                    if (argType.IsEnumerable())
                    {
                        enumerable = true;
                        descriptor = _provider.GetPayload(argType.GetEnumerableType());
                    }
                    else
                    {
                        descriptor = _provider.GetPayload(args[i].GetType());
                    }

                    // If there's a descriptor for the given arg we can compress it
                    if (descriptor != null)
                    {
                        args[i] = _compressor.Compress(args[i]);
                        contractId = descriptor.ID;
                    }
                    else
                    {
                        // Don't want to send down any unnecessary data even if the current object is enumerable
                        enumerable = false;
                    }

                    contracts[i] = (enumerable) ? contractId.ToString() + "[]" : contractId.ToString();
                }

                context.Invocation.Args = args;

                context.Invocation = new ContractedClientHubInvocation(context.Invocation)
                {
                    ContractIds = contracts
                };

                return send(context);
            });
        }
    }
}
