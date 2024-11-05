using System;
using DOT.AGM.Client;
using DOT.AGM.Services;
using Tick42;
using Tick42.Entities;

namespace FDC3ChannelsClientProfileDemo.AGM
{
    [ServiceContract(MethodNamespace = "T42.CRM.")]
    public interface IT42CRMService : IDisposable
    {
        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true, InvocationTargetType = MethodTargetType.All)]
        void SyncContact(T42Contact contact, [AGMServiceOptions] IServiceOptions options = null);
    }
}