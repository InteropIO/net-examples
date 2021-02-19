using Glue.Channels;
using Glue.Contexts;
using GlueForNetCore;
using GlueForNetCore.AppManager;
using GlueForNetCore.GDStarting;
using GlueForNetCore.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MultiWindowWPFDemo
{
    /// <summary>
    /// Interaction logic for ClientPortfolioView.xaml
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
            var color = (Color)ColorConverter.ConvertFromString(colorAsString);
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

        private void ColorSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var currSelection = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;

            if (currSelection != null)
            {
                var rect = (Rectangle)currSelection;
                ColorRectangle.Fill = rect.Fill;
            }
        }

        public void Initialize(MainWindow context, State state, Glue42 glue, GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            var colorAsString = state?.RectangleColor ?? "#FFFFFF";
            var color = (Color)ColorConverter.ConvertFromString(colorAsString);
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
            return this.Dispatcher.Invoke(() =>
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

        public void HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel, T42Contact data)
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

        public class State
        {
            public string RectangleColor { get; set; }
        }
    }
}
