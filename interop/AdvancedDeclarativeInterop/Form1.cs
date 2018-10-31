using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Extensions;
using DOT.AGM.Services;
using DOT.Core.Extensions;
using DOT.Logging;
using Tick42;
using Tick42.Entities;

namespace AdvancedDeclarativeInterop
{
    public partial class Form1 : Form, IServiceContract
    {
        private int invokeCount_;
        private IServiceContract service_;

        public Form1()
        {
            InitializeComponent();
        }

        public Glue42 Glue { get; private set; }

        void IServiceContract.ShowClient(T42Contact contact, IServiceOptions serviceOption)
        {
            Log($"Show contact {contact}");
        }

        void IServiceContract.GetState(Action<ClientPortfolioDemoState> handleResult)
        {
            handleResult(new ClientPortfolioDemoState
                {Height = 200, Left = 10, SelectedClient = Guid.NewGuid().ToString(), Top = 123, Width = 400});
        }

        void IServiceContract.CheckAsyncClientMethodResult(ref int someInt,
            Action<IClientMethodResult> handleClientMethodResult)
        {
            someInt += 5;
        }

        CompositeType IServiceContract.SetCurrentInstrument(string[] symbolNames, string instrumentDefaultType,
            string instrumentDefaultName,
            string venue, string instrumentContext, out SCIResultCode sciResultCode, out DateTime dt,
            out CompositeType outResponse)
        {
            sciResultCode = SCIResultCode.Succeeded;
            dt = DateTime.UtcNow.AddYears(5);
            outResponse = new CompositeType {Message = "This was an out response"};
            Log($"{nameof(IServiceContract.SetCurrentInstrument)}: {symbolNames?.AsString()}");
            return new CompositeType();
        }

        Rectangle IServiceContract.Offset(Rectangle rect, int x, int y)
        {
            rect.Offset(x, y);
            // let's set this rect to the service's bounds property
            (this as IServiceContract).Bounds = rect;
            return rect;
        }

        UnwrappedComposite IServiceContract.GetUnwrapped(string csv, int x)
        {
            return new UnwrappedComposite
            {
                SomeInt = x,
                SomeStringArray = csv.Split(',').ToArray()
            };
        }

        Rectangle? IServiceContract.Bounds { get; set; }

        void IServiceContract.CalculateSumAndMulAsync(int x, int y, Action<int, int> handleResult)
        {
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => handleResult(x + x, x * y));
        }

        void IServiceContract.ComplexAsyncOutput(int input, Action<CompositeType> response)
        {
            response(new CompositeType {Message = "You're fine with " + input});
        }

        void IServiceContract.TestAGMOptions(string s, IServiceOptions options)
        {
            string additionalValue =
                options.ServerOptions.InvocationContext.InvocationContext.Arguments.GetValueByName(
                    "AdditionalValue", v => v.AsString);

            Log($"{nameof(IServiceContract.TestAGMOptions)} {s} - invoked with {additionalValue} as additional value");

            if (additionalValue == "break")
            {
                // the additional value
                options.ServerOptions.PostInvocationInterceptor = (context, builder) =>
                {
                    builder.SetIsFailed(true);
                    builder.SetMessage("Intercepted because caller requested");
                    return InterceptedActionContinuation.Break;
                };
                return;
            }

            Log($"Normal work of {nameof(IServiceContract.TestAGMOptions)} {s}");
        }
        
        protected override void OnShown(EventArgs e)
        {
            SynchronizationContext synchronizationContext = SynchronizationContext.Current;

            Task.Run(() =>
            {
                try
                {
                    InitializeGlue(synchronizationContext);
                }
                catch (Exception exception)
                {
                    Log("Failed initializing Glue");
                    Log(exception.ToString());
                }
            });
        }

        private void InitializeGlue(SynchronizationContext synchronizationContext)
        {
            // initialize Tick42 Interop (AGM) and Metrics components

            Log("Initializing Glue42");

            string glueUser = Environment.UserName;
            Log($"User is {glueUser}");

            // these envvars are expanded in some configuration files
            Environment.SetEnvironmentVariable("PROCESSID", Process.GetCurrentProcess().Id + "");
            Environment.SetEnvironmentVariable("GW_USERNAME", glueUser);
            //Environment.SetEnvironmentVariable("DEMO_MODE", Mode + "");

            // The Glue42 facade provides a simplified programming
            // interface over the core Glue42 components.
            Glue = new Glue42();

            Glue.Interop.ConnectionStatusChanged += (sender, args) => { Log($"Glue connection is now {args.Status}"); };
            Glue.Interop.TargetStatusChanged += (sender, args) => Log($"{args.Status.State} target {args.Target}");

            var advancedOptions = new Glue42.AdvancedOptions {SynchronizationContext = synchronizationContext};

            Glue.Initialize(
                Assembly.GetEntryAssembly().GetName().Name, // application name - required
                useAgm: true,
                useAppManager: true,
                useMetrics: true,
                useContexts: false,
                useStickyWindows: false,
                credentials: Tuple.Create(glueUser, ""),
                advancedOptions: advancedOptions);

            // force single target
            service_ = Glue.Interop.CreateServiceProxy<IServiceContract>(targetType: MethodTargetType.Any);

            // let's track the status of the service target - if anybody has implemented it.

            Glue.Interop.CreateServiceSubscription(service_,
                (_, status) => Log($"{nameof(IServiceContract)} is now {(status ? string.Empty : "in")}active"));

            Log("Initialized Glue Interop");
        }

        private void Log(string logMessage)
        {
            if (InvokeRequired)
            {
                // post it, and exit to avoid dead-locks since we've asked Glue42 to interop marshal events
                // with the synchronization context
                BeginInvoke((Action) (() => Log(logMessage)));
                return;
            }

            txtLog_.AppendText(logMessage + "\r\n");
        }

        private void BtnRegisterClick(object sender, EventArgs e)
        {
            Glue.Interop.RegisterService<IServiceContract>(this);
        }

        private void BtnInvokeClick(object sender, EventArgs e)
        {
            invokeCount_++;
            var contact = new T42Contact
            {
                Name = new T42Name {FirstName = "Joe", LastName = "Smith"}
            };

            // intercepting the result
            service_.ShowClient(contact, new ServiceOptions(
                (so, invocation, cmr, ex) =>
                {
                    Log($"{nameof(service_.ShowClient)} of {contact.Name} completed with {cmr} : {ex}");
                    if (cmr == null || cmr.Status != MethodInvocationStatus.Succeeded || ex != null)
                    {
                        Log("Cannot show client due to " +
                            (cmr?.ToString() ?? "appropriate target method not found"));
                    }
                }));

            // async contract result
            service_.GetState(state => { Log("State is " + state); });

            int x = 5;

            // async handle the internal method result
            service_.CheckAsyncClientMethodResult(ref x,
                cmr => Log(
                    $"{nameof(IServiceContract.CheckAsyncClientMethodResult)} completed with {cmr?.Status}"));

            // let's intercept the result and also add one additional value - to not break the contract (alter the interface)
            service_.TestAGMOptions("Hello",
                new ServiceOptions(
                    builder =>
                        // we can tweak 
                        builder.SetInvocationTarget(MethodTargetType.Any)
                            //we can check the current invocation arguments via .MethodInvocationContext.Arguments
                            //add one additional value
                            .SetContext(cb =>
                                cb.AddValue("AdditionalValue",
                                    invokeCount_ % 2 == 0 ? "break" : Guid.NewGuid().ToString()))
                            // let's set the logging level for that call
                            .SetInvocationLoggingLevel(LogLevel.Debug),

                    // intercept the result
                    (operation, invocation, cmr, ex) => Log("Intercepted result: " + cmr.ToString())));


            // let's consume some composite types, enums, arrays etc.
            CompositeType result = service_.SetCurrentInstrument(new[] {"RIC=VOD.L"}, "RIC", "VOD.L", "LN", "BaiKai",
                out SCIResultCode sciResultCode, out DateTime dt, out CompositeType outResponse);

            // try the custom rectangle serializer
            var offsetRect = service_.Offset(new Rectangle(0, 0, 50, 50), 20, 20);

            // JS friendly - unwrapped object
            var compositeUnwrapped = service_.GetUnwrapped("a,b,c,d,e", 15);

            // remote property
            var oldBounds = service_.Bounds;
            Log($"Old bounds : {oldBounds}");
            service_.Bounds = new Rectangle(100, 100, 500, 500);
            var newBounds = service_.Bounds;
            Log($"New bounds : {newBounds}");

            // multiple async results
            service_.CalculateSumAndMulAsync(2, 5, (i, i1) =>
            {
                Log($"i = {i}; i1  = {i1}");
            });

            // composite async output
            service_.ComplexAsyncOutput(511, response =>
            {
                Log("Complex async response " + response.Message);
            });
        }
    }
}