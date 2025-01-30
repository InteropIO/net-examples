using System;
using System.Threading.Tasks;
using Glue;
using Microsoft.AspNetCore.Components;

namespace GlueBlazor.Data
{
    internal class AspNetDispatcher : IGlueDispatcher
    {
        private readonly Dispatcher dispatcher_;

        public AspNetDispatcher(Dispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        public Task<T> InvokeAsync<T>(Func<T> callback)
        {
            return dispatcher_.InvokeAsync(callback);
        }

        public void Dispatch(Func<Task> taskAction)
        {
            dispatcher_.InvokeAsync(taskAction);
        }

        public T Invoke<T>(Func<T> action)
        {
            return dispatcher_.InvokeAsync(action).Result;
        }

        public void EnsureStarted()
        {
        }

        public void Dispatch(Action action)
        {
            dispatcher_.InvokeAsync(action);
        }

        public int DispatcherThreadId => -1;

        public string Name => dispatcher_.ToString();

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}