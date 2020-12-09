using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Tick42;
using Tick42.AppManager;
using Tick42.Channels;
using Tick42.StartingContext;
using Tick42.Windows;

namespace MultiWindowDemo
{
    // The window implements the IGlueApp interface and indicates the state shape which will be used for saving and restoring and the context which in this case is the MainWindow
    public partial class ColorWindow : Window, IGlueApp<ColorWindow.State, MainWindow>
    {

        // The shape of the state which will be used when the window is saved or restored
        public class State
        {
            public string RectangleColor { get; set; }
        }

        public ColorWindow()
        {
            InitializeComponent();
            ColorSelector.SelectionChanged += ColorSelector_SelectionChanged;
        }

        public ColorWindow(string colorAsString)
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

        public void Initialize(MainWindow context, State state, Glue42 glue, GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            // The method is invoked when the window is restored
            var colorAsString = state?.RectangleColor ?? "#FFFFFF";
            var color = (Color)ColorConverter.ConvertFromString(colorAsString);
            var items = ColorSelector.Items;

            Dispatcher.Invoke(() =>
            {
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
            });
        }

        public async Task<State> GetState()
        {
            // Returning the current state of the application when GlueDesktop requires it (e.g when the window is being saved)
            // The state in this example is the color of the rectangle
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

        private void ColorSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var currSelection = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;

            if (currSelection != null)
            {
                var rect = (Rectangle)currSelection;
                ColorRectangle.Fill = rect.Fill;
            }
        }
    }
}
