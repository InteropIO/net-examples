using DOT.AGM.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFApp
{
    [ServiceContract()]
    public interface INotificationHandler : IDisposable
    {
        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void AcceptNotification(string data);

        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void RejectNotification(string data);
    }
}
