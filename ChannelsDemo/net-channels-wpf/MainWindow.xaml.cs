using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using DOT.AGM;
using Tick42;
using Tick42.Channels;
using Tick42.Contexts;
using Tick42.Windows;

namespace net_channels_wpf
{
    public partial class MainWindow : Window, IGlueChannelEventHandler
    {
        private Glue42 glue;
        private IGlueWindow glueWindow = null;
        private IGlueChannelContext icContext  = null;
        private List<IGlueChannel> availableChannels = new List<IGlueChannel>();
        private bool glueIsConnecting = false;

        public MainWindow()
        {
            InitializeComponent();
            UpdateControlsInitial();
            _ = InitializeGlue();
        }

        private async Task InitializeGlue()
        {
            if (glue is object || glueIsConnecting) return;
            var initOpt = new Tick42.StartingContext.InitializeOptions()
            {
                ApplicationName = "net-channels-wpf",
            };
            try
            {
                glueIsConnecting = true;

                // initialize Glue
                glue = await Glue42.InitializeGlue(initOpt);

                // get independent channel context and subscribe for events on the channel
                icContext = glue.Channels.JoinChannel(null);
                icContext.Subscribe(new LambdaGlueChannelEventHandler(HandleIndependentChannelUpdate, HandleIndependentChannelChanged));

                // subscribe for channel discovery
                glue.Channels.Subscribe(onChannеlDiscovered);

                UpdateControlsGlueConnected();
            }
            catch (Exception ex)
            {
                SetStatusText($"Error:{ex.Message}");
                MessageBox.Show(ex.Message, "An error occurred while initializing Glue");
            }
            finally
            {
                glueIsConnecting = false;
            }
        }

        private async Task RegisterGlueWindow()
        {
            if (glueWindow is object)
            {
                MessageBox.Show("Glue window is already registered", "Info");
                return;
            }
            if (glue is null)
            {
                MessageBox.Show("Glue not yet initialized", "Info");
                return;
            }

            try
            {
                var gwOptions = glue.GlueWindows.GetStartupOptions() ?? new GlueWindowOptions();
                gwOptions.WithChannelSupport(true);
                if(string.IsNullOrWhiteSpace(gwOptions.Title))
                {
                    gwOptions.WithTitle(Title);
                }
                string preSelectedChannel = (ComboWCSelector.SelectedItem as string) ?? "None";
                if( preSelectedChannel is string && !preSelectedChannel.Equals("None"))
                {
                    gwOptions.WithChannel(preSelectedChannel);
                }
                glueWindow = await glue.GlueWindows?.RegisterWindow(this, gwOptions);

                // subscribe for window channel events
                glueWindow.ChannelContext.Subscribe(this);
                glueWindow.ChannelContext.Subscribe(new LambdaGlueChannelEventHandler<Value>((context, info, arg3) =>
                {
                    System.Diagnostics.Trace.WriteLine(arg3);
                }));

                UpdateControlsWindowRegistered();
            }
            catch (Exception ex)
            {
                SetStatusText($"Error:{ex.Message}");
                MessageBox.Show(ex.Message, "An error occurred while registering a Glue window");
            }
            finally
            {
            }
        }

        #region Channel Events
        // Discovery callback
        private void onChannеlDiscovered(IGlueChannel channel)
        {
            availableChannels.Add(channel);
            UpdateAvailableChannels();
        }


        // Variant 1: channel events handled by implementing the IGlueChannelContext interface
        // IGlueChannelEventHandler::HandleChannelChanged implementation
        public void HandleChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel, IGlueChannel prevChannel)
        {
            var reason = $"ChannelChanged: {prevChannel?.Name ?? "None"}=>{newChannel?.Name ?? "None"}";
            var text = GetChannelInfoText(channelContext, newChannel, reason);
            SetWindowChannelText(text);
            UpdateWCChannelSelection();
        }

        // IGlueChannelEventHandler::HandleUpdate implementation
        public void HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel, ContextUpdatedEventArgs updateArgs)
        {
            var text = GetChannelInfoText(channelContext, channel, "Channel Data Updated");
            SetWindowChannelText(text);
        }

        // Variant 2: channel events handled by a LambdaGlueChannelEventHandler which implements the IGlueChannelContext interface
        // channel changed handler
        public void HandleIndependentChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel, IGlueChannel prevChannel)
        {
            var reason = $"ChannelChanged: {prevChannel?.Name ?? "None"}=>{newChannel?.Name ?? "None"}";
            var text = GetChannelInfoText(channelContext, newChannel, reason);
            SetIndependentChannelText(text);
        }

        // channel data updated handler
        public void HandleIndependentChannelUpdate(IGlueChannelContext channelContext, IGlueChannel channel, ContextUpdatedEventArgs updateArgs)
        {
            var text = GetChannelInfoText(channelContext, channel, "Channel Data Updated");
            SetIndependentChannelText(text);
        }
        #endregion
    }
}
