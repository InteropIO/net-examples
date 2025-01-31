using System.Globalization;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Glue;
using Glue.GDStarting;
using Glue.Logging;

namespace GlueSeek
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GlueInstanceDiscoveryMonitor glueMonitor_;

        public MainWindow()
        {
            InitializeComponent();

            glueMonitor_ = new GlueInstanceDiscoveryMonitor(DebugLoggerFactory.Instance);
            glueMonitor_.OnInstancesChange += GlueMonitorOnInstancesChange;
            glueMonitor_.StartListening();
        }

        private async void GlueMonitorOnInstancesChange(object? sender, EventArgs e)
        {
            var r = await glueMonitor_.Discover(new AdvancedOptions());

            Dispatcher.BeginInvoke(() => GlueGrid.ItemsSource = r?.Select(gdsc => gdsc.GDInstanceConfig));
        }

        public static async Task<bool> TryPortAsync(GDInstanceConfig gdic)
        {
            if (gdic == null)
            {
                return false;
            }

            try
            {
                using var client = new TcpClient();
                var uri = new Uri(gdic.GwUrl);
                await client.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PortColumn_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2 || sender is not TextBlock { DataContext: GDInstanceConfig gdic })
            {
                return;
            }

            var converter = (GDInstanceConverter)FindResource("GDICValueConverter");

            converter.SetPortStatus(gdic, GDInstanceConverter.PortStatus.Checking);
            GlueGrid.Items.Refresh();
            TryPortAsync(gdic).ContinueWith(r =>
            {
                converter.SetPortStatus(gdic,
                    r.Result ? GDInstanceConverter.PortStatus.Open : GDInstanceConverter.PortStatus.Closed);
                GlueGrid.Items.Refresh();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    public enum ConverterValue
    {
        Pid,
        Port,
        PidColor,
        PortColor,
    }

    public class GDInstanceConverter : IValueConverter
    {
        public enum PortStatus
        {
            Checking,
            Open,
            Closed
        }

        private readonly Dictionary<object, PortStatus?> portStatuses_ = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not GDInstanceConfig item)
            {
                return "N/A";
            }

            PortStatus? port = portStatuses_.GetValueOrDefault(item, null);
            return (parameter as ConverterValue?) switch
            {
                ConverterValue.Pid => item.TryPid() ? "OK" : "MISSING",
                ConverterValue.Port => port != null ? port.Value : "DBLCLCK TO CHECK",
                ConverterValue.PidColor => item.TryPid() ? Brushes.LightGreen : Brushes.Red,
                ConverterValue.PortColor => port != null
                    ? port.Value == PortStatus.Open ? Brushes.LightGreen :
                    port.Value == PortStatus.Closed ? Brushes.Red : Brushes.Wheat
                    : Brushes.DarkGray,
                _ => null
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public void SetPortStatus(object dataItem, PortStatus newValue) => portStatuses_[dataItem] = newValue;
    }
}