using System.Threading.Tasks;
using System.Windows;
using Tick42;
using Tick42.AppManager;
using Tick42.StartingContext;
using Tick42.Windows;

namespace BootstrappedMainApp
{
    /// <summary>
    ///     Interaction logic for Window1.xaml
    /// </summary>
    public partial class BootstrappedWindow1 : Window, IGlueApp<App.DummyState, App.DummyContext>
    {
        public BootstrappedWindow1()
        {
            InitializeComponent();
        }

        public void Shutdown()
        {
            Close();
        }

        public void Initialize(App.DummyContext context,
            App.DummyState state,
            Glue42 glue,
            GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
        }

        public Task<App.DummyState> GetState()
        {
            return Task.FromResult(new App.DummyState());
        }
    }
}