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
using Tick42.StartingContext;
using Tick42.Windows;

namespace MultiWindowDemo
{
    // The window implements the IGlueApp interface and indicates the shape of the state which will be used and the context which in this case is the MainWindow
    public partial class ChartWindow : Window, IGlueApp<ChartWindow.SymbolState, MainWindow>
    {
        private readonly string SymbolOneName = "VOD.L";
        private readonly string SymbolTwoName = "BARC";

        // The shape of the state which will be used when the window is being saved or restored
        public class SymbolState
        {
            public string ActiveSymbol { get; set; }
        }

        public ChartWindow()
        {
            InitializeComponent();
            Symbol.Text = SymbolOneName;
        }

        public async Task<SymbolState> GetState()
        {
            // Returning the state which will be saved when the window is saved in a layout
            // The state of the app is the currently selected symbol
            return Dispatcher.Invoke(() =>
            {
                var state = new SymbolState()
                {
                    ActiveSymbol = Symbol.Text
                };

                return state;
            });
        }

        public void Initialize(MainWindow context, SymbolState state, Glue42 glue, GDStartingContext startingContext, IGlueWindow glueWindow)
        {
            // Invoked when the window is restored
            Dispatcher.Invoke(() =>
            {
                Symbol.Text = state?.ActiveSymbol ?? SymbolOneName;
            });
        }

        public void Shutdown()
        {
            ChartControl.Dispose();
            Close();
        }

        private void Switch_Click(object sender, RoutedEventArgs e)
        {
            Symbol.Text = Symbol.Text == SymbolOneName ? SymbolTwoName : SymbolOneName;
        }
    }
}
