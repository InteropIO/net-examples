using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tick42;
using Tick42.Contexts;
using Tick42.Windows;

namespace WPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var gwOptions = App.Glue.GlueWindows?.GetStartupOptions() ?? new GlueWindowOptions();
            gwOptions.WithType(GlueWindowType.Tab);
            gwOptions.WithTitle("Example Window");

            // register the window and save the result
            App.Glue.GlueWindows?.RegisterWindow(this, gwOptions)?.ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    GlueWindow = t.Result;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public IGlueWindow GlueWindow { get; set; }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlueWindow == null)
            {
                return;
            }
            GlueWindow.IsVisible = false;
            Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => { GlueWindow.IsVisible = true; });
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlueWindow != null)
            {
                GlueWindow.Title = "Changed Title";
            }
        }

        private void ToggleChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlueWindow != null)
            {
                GlueWindow.ChannelSupport = !GlueWindow.ChannelSupport;
            }
        }

        private void ChangeChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if ((GlueWindow != null) && GlueWindow.ChannelSupport)
            {
                var channels = App.Glue.Channels?.GetChannels();
                if ((channels == null) || (channels.Length == 0))
                {
                    return;
                }
                var random = new Random(Environment.TickCount);
                var channel = channels[random.Next(channels.Length)];
                GlueWindow.Channel = channel.Name;
            }
        }
    }
}
