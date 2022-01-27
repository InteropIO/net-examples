﻿using System;
using System.Threading.Tasks;
using Glue.AppManager;
using Microsoft.AspNetCore.Components;

namespace GlazorWebAssembly
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

        public void BeginInvoke(Action action)
        {
            dispatcher_.InvokeAsync(action);
        }

        public T Invoke<T>(Func<T> action)
        {
            return dispatcher_.InvokeAsync(action).Result;
        }

        public void Invoke(Action action)
        {
            dispatcher_.InvokeAsync(action);
        }

        public int DispatcherThreadId => -1;
    }
}