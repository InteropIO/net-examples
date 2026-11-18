using System;
using System.Collections.Generic;
using DOT.AGM.Services;
using Tick42;

namespace WPFApp
{
    public class IOCDAction
    {
        public string Action { get; set; }
        public string CustomerId { get; set; }
        public string RelationId { get; set; }

        public Dictionary<string, object> Arguments { get; set; }
    }
    
    [ServiceContract]
    public interface INotificationHandler : IDisposable
    {
        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void AcceptNotification(string customerId, double customerPrice, IOCDAction action);

        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void RejectNotification(string customerId, double customerPrice, IOCDAction action);

        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void NotificationRoutingDetail([AGMServiceOptions] IServiceOptions options = null);
    }
}