using DOT.AGM.Transport;
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
        public static readonly DependencyProperty ConnectionStatusDescriptionProperty = DependencyProperty.Register("ConnectionStatusDescription", typeof(string), typeof(MainWindow));
        public static readonly DependencyProperty ConnectionStatusColorProperty = DependencyProperty.Register("ConnectionStatusColor", typeof(Brush), typeof(MainWindow));

        private Glue42 _glue;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Visibility = Visibility.Hidden;
            UpdateUI(false);
        }

        public IGlueWindow GlueWindow { get; set; }

        public string ConnectionStatusDescription
        {
            get
            {
                return GetValue(ConnectionStatusDescriptionProperty).ToString();
            }
            set
            {
                SetValue(ConnectionStatusDescriptionProperty, value);
            }
        }

        public Brush ConnectionStatusColor
        {
            get
            {
                return (Brush)GetValue(ConnectionStatusColorProperty);
            }
            set
            {
                SetValue(ConnectionStatusColorProperty, value);
            }
        }

        internal void RegisterGlue(Glue42 glue)
        {
            _glue = glue;
            UpdateUI(true);

            //bounds are optional. With them we will just set initial placement of the application
            var defaultBounds = new GlueWindowBounds()
            {
                X = (int)((SystemParameters.PrimaryScreenWidth / 2) - (Width / 2)),
                Y = (int)((SystemParameters.PrimaryScreenHeight / 2) - (Height / 2)),
                Width = (int)Width,
                Height = (int)Height
            };
            var gwOptions = glue.GetStartupWindowOptions(Title, defaultBounds);
            glue.GlueWindows?.RegisterWindow(this, gwOptions).ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    GlueWindow = t.Result;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlueWindow != null)
            {
                GlueWindow.IsVisible = false;
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => { GlueWindow.IsVisible = true; });
            }
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlueWindow != null)
            {
                GlueWindow.Title = $"Changed Title - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
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
                var channels = _glue?.Channels?.GetChannels();
                if ((channels == null) || (channels.Length == 0))
                {
                    return;
                }
                var random = new Random(Environment.TickCount);
                var channel = channels[random.Next(channels.Length)];
                GlueWindow.Channel = channel.Name;
            }
        }

        private void UpdateUI(bool isConnected)
        {
            var statusMessage = isConnected ? "Connected" : "Disconnected";
            var statusColor = isConnected ? Colors.LightGreen : Colors.LightPink;

            ConnectionStatusDescription = statusMessage;
            ConnectionStatusColor = new SolidColorBrush(statusColor);

            HideWindowButton.IsEnabled = isConnected;
            ChangeWindowTitleButton.IsEnabled = isConnected;
            ToggleChannelsButton.IsEnabled = isConnected;
            ChangeChannelButton.IsEnabled = isConnected;
        }
    }
}
