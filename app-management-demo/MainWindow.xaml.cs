using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DOT.Logging;
using Tick42;
using Tick42.AppManager;
using Tick42.Windows;

namespace AppManagerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Dependency Properties

        public static readonly DependencyProperty ConnectionStatusDescriptionProperty = DependencyProperty.Register("ConnectionStatusDescription", typeof(string), typeof(MainWindow));
        public static readonly DependencyProperty ConnectionStatusColorProperty = DependencyProperty.Register("ConnectionStatusColor", typeof(Brush), typeof(MainWindow));

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

        #endregion

        private Glue42 _glue;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Visibility = Visibility.Hidden;
            UpdateUI(false);
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
            var gwOptions = glue.GetStartupWindowOptions("AppManager Demo", defaultBounds);
            gwOptions.WithType(GlueWindowType.Flat);
            glue.GlueWindows?.RegisterWindow(this, gwOptions);

            var appManager = glue.AppManager;
            if (appManager != null)
            {
                appManager.ApplicationAdded += OnApplicationAdded;
                appManager.ApplicationUpdated += OnApplicationUpdated;
                appManager.ApplicationRemoved += OnApplicationRemoved;
                appManager.ApplicationInstanceStarted += OnApplicationInstanceStarted;
                appManager.ApplicationInstanceStopped += OnApplicationInstanceStopped;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_glue != null && _glue.AppManager != null)
            {
                var appManager = _glue.AppManager;
                appManager.ApplicationAdded -= OnApplicationAdded;
                appManager.ApplicationRemoved -= OnApplicationRemoved;
                appManager.ApplicationInstanceStarted -= OnApplicationInstanceStarted;
                appManager.ApplicationInstanceStopped -= OnApplicationInstanceStopped;
            }
            base.OnClosed(e);
        }

        #region Applications

        private void OnApplicationAdded(object sender, AppManagerApplicationEventArgs e)
        {
            ExecuteAction(() => { applicationList.Items.Add(e.Application); });
        }

        private void OnApplicationUpdated(object sender, AppManagerApplicationEventArgs e)
        {
            ExecuteAction(() => { applicationList.Items.Refresh(); });
        }

        private void OnApplicationRemoved(object sender, AppManagerApplicationEventArgs e)
        {
            ExecuteAction(() => { applicationList.Items.Remove(e.Application); });
        }

        #endregion

        #region Instances

        private void OnApplicationInstanceStarted(object sender, AppManagerApplicationInstanceEventArgs e)
        {
            ExecuteAction(() => { applicationInstanceList.Items.Add(e.Instance); });
        }

        private void OnApplicationInstanceStopped(object sender, AppManagerApplicationInstanceEventArgs e)
        {
            ExecuteAction(() => { applicationInstanceList.Items.Remove(e.Instance); });
        }

        #endregion

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApplication = applicationList.SelectedItem as IAppManagerApplication;
            if (selectedApplication != null)
            {
                var context = AppManagerContext.CreateNew();
                context.Set("startedFrom", Process.GetCurrentProcess().Id);
                var task = selectedApplication.Start(context);
                task.ContinueWith(t =>
                {
                    if (t.Status != TaskStatus.RanToCompletion)
                    {
                        // Applcation failed to start
                    }
                    else
                    {
                        // Started application instance
                        var instance = t.Result;
                    }
                });
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApplicationInstance = applicationInstanceList.SelectedItem as IAppManagerApplicationInstance;
            if (selectedApplicationInstance != null)
            {
                selectedApplicationInstance.Stop();
            }
        }

        private void ExecuteAction(Action action)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(action, null);
            }
            else
            {
                action();
            }
        }

        private void UpdateUI(bool isConnected)
        {
            var statusMessage = isConnected ? "Connected" : "Disconnected";
            var statusColor = isConnected ? Colors.LightGreen : Colors.LightPink;

            ConnectionStatusDescription = statusMessage;
            ConnectionStatusColor = new SolidColorBrush(statusColor);

            startButton.IsEnabled = isConnected;
            stopButton.IsEnabled = isConnected;
            applicationList.IsEnabled = isConnected;
            applicationInstanceList.IsEnabled = isConnected;
        }
    }
}
