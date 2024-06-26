using System;
using System.Threading.Tasks;
using System.Windows;
using DOT.AGM;
using DOT.AGM.Services;
using Tick42;
using Tick42.StartingContext;
using Tick42.Windows;
using IServiceOptions = Tick42.IServiceOptions;

namespace MultipleInstancesDemo
{
    [ServiceContract(MethodNamespace = "io.multiple.")]
    public interface ISomethingService : IDisposable
    {
        [ServiceOperation(UnwrapCompositeReturnParameter = true, AsyncIfPossible = true)]
        void GetSomethingElse(MainWindow.Something something, Value stuff,
            [ServiceOperationField(IgnoreInSignature = true)] [ServiceOperationResultHandler("result")]
            Action<MainWindow.Something> handleResult,
            [AGMServiceOptions] IServiceOptions options);
    }

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ISomethingService
    {
        private ISomethingService caller_;
        private Glue42 glue_;
        private IGlueWindow wnd_;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }

        public void GetSomethingElse(Something something, Value stuff, Action<Something> handleResult,
            IServiceOptions options)
        {
            things.Items.Add("Getting " + something);
            var somethingElse = new Something
            {
                Thing = something.Thing + " Else",
                HappenedOn = DateTime.UtcNow,
                Price = something.Price * 2
            };
            handleResult(somethingElse);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                glue_?.Shutdown();
            }
            catch
            {
                // swallow
            }
        }

        public async void ProcessArguments(string[] args, RemoteConfigurationOptions rco)
        {
            var initializeOptions = new InitializeOptions
            {
                ApplicationName = "MultipleInstancesDemo",
                OnSaveState = value => Task.FromResult<object>(new
                {
                    things = things.Items.Count,
                    something = new Something
                    {
                        Thing = "Something",
                        Price = 100,
                        HappenedOn = DateTime.UtcNow
                    }
                }),
            };

            if (rco != null)
            {
                initializeOptions.ForcedRemoteConfiguration = rco;
            }

            var initializing = Glue42.InitializeGlue(initializeOptions);
            glue_ = await initializing;
            wnd_ = await glue_.GlueWindows.RegisterStartupWindow(this, initializeOptions.ApplicationName,
                w => w.WithChannelSupport(true));
            glue_.Interop.RegisterService<ISomethingService>(this,
                modifyServiceConfig: c => c.Dispatcher = new WrappedDispatcher(Dispatcher));
            caller_ = glue_.Interop.CreateServiceProxy<ISomethingService>();

            T GetRestoreState<T>(T _) => glue_.GetRestoreState<T>();

            var state = GetRestoreState(new
            {
                things = things.Items.Count,
                something = new Something
                {
                    Thing = "Something",
                    Price = 100,
                    HappenedOn = DateTime.UtcNow
                }
            });
            if (state != null)
            {
                things.Items.Add("Restored " + state.things + " things and last thing is " + state.something);
            }
        }

        private void DoSomething_Click(object sender, RoutedEventArgs e)
        {
            caller_.GetSomethingElse(new Something
                {
                    Thing = "Something",
                    Price = 100,
                    HappenedOn = DateTime.UtcNow
                }, 5151, something => Dispatcher.BeginInvoke((Action)(() => things.Items.Add("Got " + something))),
                null);
        }

        public class Something
        {
            public string Thing { get; set; }
            public double Price { get; set; }
            public DateTime HappenedOn { get; set; }

            public override string ToString()
            {
                return $"{nameof(Thing)}: {Thing}, {nameof(Price)}: {Price}, {nameof(HappenedOn)}: {HappenedOn}";
            }
        }
    }
}