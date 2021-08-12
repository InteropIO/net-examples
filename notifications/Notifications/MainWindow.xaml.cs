using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GnsDesktopManager.Model;
using Tick42;
using Tick42.Windows;

namespace WPFApp
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotificationHandler
    {
        private Glue42 glue_;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            Visibility = Visibility.Hidden;
            UpdateUI(false);
        }

        public void AcceptNotification(string customerId)
        {
            //accept handler with notification object state
        }

        public void RejectNotification(string customerId)
        {
            //reject handler with notification object state
        }

        public void Dispose()
        {
        }

        internal void RegisterGlue(Glue42 glue)
        {
            glue_ = glue;
            UpdateUI(true);

            //bounds are optional. With them we will just set initial placement of the application
            var defaultBounds = new GlueWindowBounds
            {
                X = (int) ((SystemParameters.PrimaryScreenWidth / 2) - (Width / 2)),
                Y = (int) ((SystemParameters.PrimaryScreenHeight / 2) - (Height / 2)),
                Width = (int) Width,
                Height = (int) Height
            };
            var gwOptions = glue.GetStartupWindowOptions("Notification Publisher", defaultBounds);
            gwOptions.WithType(GlueWindowType.Tab);

            //register the window
            glue.GlueWindows?.RegisterWindow(this, gwOptions);

            //register notification service
            glue.Interop.RegisterService<INotificationHandler>(this);
        }

        private void OnSendNotificationClick(object sender, RoutedEventArgs e)
        {
            if (glue_ == null)
            {
                //glue is not initialized
                return;
            }

            //object state
            var parameters = new List<GlueMethodParameter>
            {
                new GlueMethodParameter("customerId", new GnsValue("11"))
            };

            var actions = new List<GlueRoutingMethod>
            {
                new GlueRoutingMethod("AcceptNotification", Description: "Accept", Parameters: parameters),
                new GlueRoutingMethod("RejectNotification", Description: "Reject")
            };

            var notification = new DesktopNotification(Title.Text,
                (NotificationSeverity) Enum.Parse(typeof(NotificationSeverity), Severity.Text),
                "type",
                Description.Text,
                "category",
                "source",
                "AcceptedHandler",
                actions
            );

            glue_.Notifications.Publish(notification)
                .ContinueWith(r =>
                {
                    if (r.Status != TaskStatus.RanToCompletion)
                    {
                    }
                });
        }

        private void UpdateUI(bool isConnected)
        {
            var statusMessage = isConnected ? "Connected" : "Disconnected";
            var statusColor = isConnected ? Colors.LightGreen : Colors.LightPink;

            ConnectionStatusDescription = statusMessage;
            ConnectionStatusColor = new SolidColorBrush(statusColor);

            Title.IsEnabled = isConnected;
            Description.IsEnabled = isConnected;
            Severity.IsEnabled = isConnected;
            ButtonNotify.IsEnabled = isConnected;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ConnectionStatusDescriptionProperty =
            DependencyProperty.Register("ConnectionStatusDescription", typeof(string), typeof(MainWindow));

        public static readonly DependencyProperty ConnectionStatusColorProperty =
            DependencyProperty.Register("ConnectionStatusColor", typeof(Brush), typeof(MainWindow));

        public string ConnectionStatusDescription
        {
            get => GetValue(ConnectionStatusDescriptionProperty).ToString();
            set => SetValue(ConnectionStatusDescriptionProperty, value);
        }

        public Brush ConnectionStatusColor
        {
            get => (Brush) GetValue(ConnectionStatusColorProperty);
            set => SetValue(ConnectionStatusColorProperty, value);
        }

        #endregion
    }
}