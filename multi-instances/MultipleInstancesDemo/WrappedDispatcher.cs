using System;
using System.Windows.Threading;
using DOT.Core.EventDispatcher;

namespace MultipleInstancesDemo
{
    public class WrappedDispatcher : IEventDispatcher
    {
        private readonly Dispatcher dispatcher_;

        public WrappedDispatcher(Dispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }

        public void Destroy()
        {
        }

        public void Dispatch(Action action)
        {
            dispatcher_.BeginInvoke(action);
        }

        public void Dispatch(IEvent @event)
        {
            Dispatch(@event.Execute);
        }

        public bool TimedDispatch(IEvent @event, double timeout)
        {
            return false;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public string Name => dispatcher_.Thread.Name;

        public int PendingMessageCount => -1;

        public bool Running
        {
            get => true;
            set { }
        }
    }
}