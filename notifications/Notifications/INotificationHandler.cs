using System;
using DOT.AGM.Services;
using Tick42;

namespace WPFApp
{
    [ServiceContract]
    public interface INotificationHandler : IDisposable
    {
        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void AcceptNotification(string customerId, double customerPrice);

        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void RejectNotification(string customerId, double customerPrice);

        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void NotificationRoutingDetail([AGMServiceOptions] IServiceOptions options = null);
    }
}