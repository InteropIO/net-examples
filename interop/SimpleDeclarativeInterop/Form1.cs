using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tick42;
using Tick42.StartingContext;

namespace SimpleDeclarativeInterop
{
    public partial class Form1 : Form, IServiceContract
    {
        private IServiceContract _service;
        private Glue42 _glue42;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            Log("Initializing Glue42");

            var initializeOptions = new InitializeOptions()
            {
                ApplicationName = "Hello Glue42 Interop",
                InitializeTimeout = TimeSpan.FromSeconds(5)
            };

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith((glue) =>
                {
                    //unable to register glue
                    if (glue.Status == TaskStatus.Faulted)
                    {
                        Log("Unable to initialize Glue42");
                        return;
                    }

                    _glue42 = glue.Result;

                    // subscribe to interop connection status event
                    _glue42.Interop.ConnectionStatusChanged += (sender, args) => { Log($"Glue connection is now {args.Status}"); };
                    _glue42.Interop.TargetStatusChanged += (sender, args) => Log($"{args.Status.State} target {args.Target}");

                    _service = _glue42.Interop.CreateServiceProxy<IServiceContract>();
                    // let's track the status of the service target - if anybody has implemented it.

                    _glue42.Interop.CreateServiceSubscription(
                        _service,
                        (_, status) => Log($"{nameof(IServiceContract)} is now {(status ? string.Empty : "in")}active"));

                    BeginInvoke((Action)(() => btnRegister.Enabled = true));

                    Log("Initialized Glue Interop");
                });
        }

        private void BtnRegisterClick(object sender, EventArgs e)
        {
            btnRegister.Tag = btnRegister.Tag != null ? (object)null : 5;
            // register the service, and explicitly specify the interface we're registering since
            // the Form has a lot of parent types
            if (btnRegister.Tag != null)
            {
                _glue42.Interop.RegisterService<IServiceContract>(this);
                btnRegister.Text = "Unregister";
                btnInvoke.Enabled = true;
            }
            else
            {
                _glue42.Interop.UnregisterService<IServiceContract>(this);
                btnRegister.Text = "Register";
                btnInvoke.Enabled = false;
            }
        }

        private void BtnInvokeClick(object sender, EventArgs e)
        {
            // since this service operation is marked as AsyncIfPossible and ExceptionSafe this won't be delayed by
            // waiting the target and won't throw exceptions
            _service.StartDoing(Guid.NewGuid().ToString());

            // since this operation has a synchronous output it will not be asynchronous and since
            // it's not marked as ExceptionSafe it will throw exceptions on any issues such as missing target etc.
            bool success = _service.GetUpdatedEntityType(
                new SomeEntityType { Name = Guid.NewGuid().ToString(), Price = 15 },
                out SomeEntityType type);

            // and now pass it by-ref
            success = _service.UpdateEntityType(ref type);
        }

        private void Log(string logMessage)
        {
            if (InvokeRequired)
            {
                // post it, and exit to avoid dead-locks since we've asked Glue42 to interop marshal events
                // with the synchronization context
                BeginInvoke((Action)(() => Log(logMessage)));
                return;
            }

            txtLog_.AppendText(logMessage + "\r\n");
        }

        #region let's implement the service contract

        void IServiceContract.StartDoing(string jobName)
        {
            Log($"Doing job {jobName}");
        }

        bool IServiceContract.GetUpdatedEntityType(SomeEntityType input, out SomeEntityType updated)
        {
            updated = input.Price < 10 ? null : new SomeEntityType { Name = input.Name, Price = input.Price + 10 };
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