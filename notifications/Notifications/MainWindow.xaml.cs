using DOT.AGM;
using DOT.AGM.Client;
using DOT.Logging;
using GnsDesktopManager.Model;
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
using Tick42.StickyWindows;

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

            var swOptions = App.Glue.StickyWindows?.GetStartupOptions() ?? new SwOptions();
            swOptions.WithType(SwWindowType.Tab);
            swOptions.WithTitle("Example Window");

            // register the window 
            App.Glue.StickyWindows?.RegisterWindow(this, swOptions);           
        }

        private void OnSendNotificationClick(object sender, RoutedEventArgs e)
        {
            var paramaters = new List<GlueMethodParameter>()
            {
                new GlueMethodParameter("customerId", new GnsValue("11"))
            };

            var actions = new List<GlueRoutingMethod>()
            {
                new GlueRoutingMethod("Accept", Description: "Accept", Parameters: paramaters),
                new GlueRoutingMethod("Reject", Description: "Reject")
            };

            var notifaction = new DesktopNotification(Title.Text,
                NotificationSeverity.Low,
                "type",
                Description.Text,
                "category",
                "source",
                "NotificationClickedHandler",              
                actions
            );

            App.Glue.Notifications.Publish(notifaction);
        }
    }
}
