using DOT.AGM.Services;
using System;

namespace WPFApp
{
    public class Notification
    {
        public string Title { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; } //: 'Your machine is going to be restarted in 30 seconds'
    }

    [ServiceContract(MethodNamespace = "T42.GNS.Publish")]
    public interface INotificationService : IDisposable
    {
        //[ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        //string GetState();

        //[ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        //void GetStateAsync([ServiceOperationResultHandler("state")] Action<string> handleResult);

        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void RaiseNotification(Notification notification);
    }
}
