using System;
using DOT.AGM;
using DOT.AGM.Core;
using DOT.AGM.Services;
using Tick42;

namespace FDC3ChannelsClientProfileDemo.AGM
{
    // used to modify an invocation
    public class CRMServiceOptions : IServiceOptions
    {
        public CRMServiceOptions(IInstance target)
        {
            Target = target;
        }

        public IInstance Target { get; }

        public IServerAGMOptions ServerOptions => throw new NotImplementedException();

        AgmInvocationContext IServiceOptions.InvocationContext => throw new NotImplementedException();
    }

    public class CRMServiceOptionsAdapter : IAGMServiceOptionsAdapter<IServiceOptions>
    {
        public IClientAGMOptions AdaptClientOptions(IAGMProxyService proxyService, IServiceOptions options)
        {
            var opt = options as CRMServiceOptions;
            return new ClientServiceAGMOptions(additionalSettings: new AdditionalSettings
            {
                // if Target is set, ensure we only call that instance
                InvocationPreProcessor = builder => builder.AddServers(
                    new LambdaInstanceRestriction(
                        i => opt?.Target?.InstanceId?.Equals(i.InstanceId) != false))
            });
        }

        public IServiceOptions AdaptServerOptions(IAGMService service, IServerAGMOptions serviceOptions)
        {
            return null;
        }
    }
}