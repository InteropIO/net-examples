using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Glue;
using Glue.AppManager;
using Glue.Channels;
using Glue.Contexts;
using Glue.GDStarting;
using Glue.Windows;

namespace MultiWindowWPFDemo
{
    /// <summary>
    ///     Interaction logic for ClientPortfolioView.xaml
    /// </summary>
    public partial class ClientPortfolioView : Window, IGlueApp<ClientPortfolioView.State, MainWindow>,
        IGlueChannelEventHandler<T42Contact>
    {
        public ClientPortfolioView()
        {
            InitializeComponent();
            ColorSelector.SelectionChanged += ColorSelector_SelectionChanged;
        }

        public ClientPortfolioView(string colorAsString)
        {
            InitializeComponent();
            var color = (Color) ColorConverter.ConvertFromString(colorAsString);
            var items = ColorSelector.Items;

            for (int i = 0; i < items.Count; i++)
            {
                var rectangle = items[i] as Rectangle;
                var colorBrush = rectangle.Fill as SolidColorBrush;

                if (colorBrush.Color == color)
                {
                    ColorSelector.SelectedIndex = i;
                }
            }

            ColorRectangle.Fill = new SolidColorBrush(color);

            ColorSelector.SelectionChanged += ColorSelector_SelectionChanged;
        }

        public void Initialize(MainWindow context, State state, IGlue42Base glue, GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            var colorAsString = state?.RectangleColor ?? "#FFFFFF";
            var color = (Color) ColorConverter.ConvertFromString(colorAsString);
            var items = ColorSelector.Items;

            for (int i = 0; i < items.Count; i++)
            {
                var rectangle = items[i] as Rectangle;
                var colorBrush = rectangle.Fill as SolidColorBrush;

                if (colorBrush.Color == color)
                {
                    ColorSelector.SelectedIndex = i;
                }
            }

            //glueWindow.Title = $"Child {context.GlueWindowId ?? string.Empty}";
            ColorRectangle.Fill = new SolidColorBrush(color);
        }

        public async Task<State> GetState()
        {
            return Dispatcher.Invoke(() =>
            {
                var rectangleColor = (ColorRectangle.Fill as SolidColorBrush).Color.ToString();
                var state = new State
                {
                    RectangleColor = rectangleColor,
                };

                return state;
            });
        }

        public void Shutdown()
        {
            Close();
        }
        
        public void HandleUpdate(IGlueChannelContext channelContext, ChannelUpdateInfo updateInfo, T42Contact data)
        {
        }

        public void HandleChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel,
            IGlueChannel prevChannel)
        {
        }

        public void HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel,
            ContextUpdatedEventArgs updateArgs)
        {
        }

        private void ColorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var currSelection = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;

            if (currSelection != null)
            {
                var rect = (Rectangle) currSelection;
                ColorRectangle.Fill = rect.Fill;
            }
        }

        public class State
        {
            public string RectangleColor { get; set; }
        }
    }
}