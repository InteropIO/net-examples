using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tick42;

namespace SimpleDeclarativeInterop
{
    public partial class Form1 : Form, IServiceContract
    {
        private IServiceContract service_;

        public Form1()
        {
            InitializeComponent();
        }

        public Glue42 Glue { get; private set; }

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
                "AdvancedInteropObjectClients", // application name - required
                useAgm: true,
                useAppManager: true,
                useMetrics: true,
                useContexts: false,
                useStickyWindows: false,
                credentials: Tuple.Create(glueUser, ""),
                advancedOptions: advancedOptions);

            service_ = Glue.Interop.CreateServiceProxy<IServiceContract>();
            // let's track the status of the service target - if anybody has implemented it.

            Glue.Interop.CreateServiceSubscription(service_,
                (_, status) => Log($"{nameof(IServiceContract)} is now {(status ? string.Empty : "in")}active"));

            Log("Initialized Glue Interop");
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

        private void BtnRegisterClick(object sender, EventArgs e)
        {
            btnRegister.Tag = btnRegister.Tag != null ? (object) null : 5;
            // register the service, and explicitly specify the interface we're registering since
            // the Form has a lot of parent types
            if (btnRegister.Tag != null)
            {
                Glue.Interop.RegisterService<IServiceContract>(this);
                btnRegister.Text = "Unregister";
            }
            else
            {
                Glue.Interop.UnregisterService<IServiceContract>(this);
                btnRegister.Text = "Register";
            }
        }

        private void BtnInvokeClick(object sender, EventArgs e)
        {
            // since this service operation is marked as AsyncIfPossible and ExceptionSafe this won't be delayed by
            // waiting the target and won't throw exceptions
            service_.StartDoing(Guid.NewGuid().ToString());

            // since this operation has a synchronous output it will not be asynchronous and since
            // it's not marked as ExceptionSafe it will throw exceptions on any issues such as missing target etc.
            bool success = service_.GetUpdatedEntityType(
                new SomeEntityType {Name = Guid.NewGuid().ToString(), Price = 15},
                out SomeEntityType type);

            // and now pass it by-ref
            success = service_.UpdateEntityType(ref type);
        }

        #region let's implement the service contract

        void IServiceContract.StartDoing(string jobName)
        {
            Log($"Doing job {jobName}");
        }

        bool IServiceContract.GetUpdatedEntityType(SomeEntityType input, out SomeEntityType updated)
        {
            updated = input.Price < 10 ? null : new SomeEntityType {Name = input.Name, Price = input.Price + 10};
            return input.Price < 10;
        }

        public bool UpdateEntityType(ref SomeEntityType input)
        {
            input = input ?? new SomeEntityType();
            input.Price = input.Price + 10;
            Log($"Returning price {input.Price}");
            return true;
        }

        #endregion
    }
}