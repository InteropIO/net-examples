using System;
using System.Threading.Tasks;
using DOT.Core.Extensions;
using Tick42;
using Tick42.AppManager;
using Tick42.Channels;
using Tick42.Contexts;
using Tick42.StartingContext;
using Tick42.Windows;

namespace io_connect_anything
{
    public class LambdaApp<TState> : IGlueAppHandle<TState, object>, IGlueChannelEventHandler
    {
        private readonly IntPtr handle_;
        public Action<IGlueChannelContext, IGlueChannel, IGlueChannel> ChannelChanged;
        public Action<IGlueChannelContext, IGlueChannel, ContextUpdatedEventArgs> ChannelUpdate;
        public Func<Task<TState>> GetState;
        private IGlueWindow glueWindow_;
        public Action<TState> Init;
        public Action Shutdown;

        public LambdaApp(IntPtr handle)
        {
            handle_ = handle;
        }

        public IntPtr GetHandle() => handle_;

        void IGlueApp.Shutdown()
        {
            Shutdown?.Invoke();
        }

        void IGlueApp<TState, object>.Initialize(object context, TState state, Glue42 glue,
            GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            glueWindow_ = glueWindow;
            glueWindow_.ChannelContext?.Subscribe(this);
            Init?.Invoke(state);
        }

        Task<TState> IGlueApp<TState, object>.GetState()
        {
            return GetState?.Invoke() ?? default(TState).AsCompletedTask();
        }

        void IGlueChannelEventHandler.HandleChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel,
            IGlueChannel prevChannel)
        {
            ChannelChanged?.Invoke(channelContext, newChannel, prevChannel);
        }

        void IGlueChannelEventHandler.HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel,
            ContextUpdatedEventArgs updateArgs)
        {
            ChannelUpdate?.Invoke(channelContext, channel, updateArgs);
        }
    }
}