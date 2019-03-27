using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Extensions;
using DOT.Core.Extensions;
using Tick42;

namespace AdvancedImperativeInterop
{
    public partial class MainForm : Form
    {
        private readonly string endpointName_ = "HandleCompositeObject";

        private readonly PropertyInfo[] instanceProps_ =
            typeof(IInstance).GetProperties().Where(p => p.PropertyType == typeof(string)).ToArray();

        private readonly Random rnd_ = new Random();

        public MainForm()
        {
            InitializeComponent();

            instanceProps_.Each(p => targets_.Columns.Add(p.Name));
            targets_.DoubleClick += TargetsOnMouseDoubleClick;
        }

        public string GlueUsername { get; set; }

        public Glue42 Glue { get; private set; }

        private void TargetsOnMouseDoubleClick(object o, EventArgs e)
        {
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

        private void InitializeGlue(SynchronizationContext synchronizationContext)
        {
            // initialize Tick42 Interop (AGM) and Metrics components

            Log("Initializing Glue42");
            GlueUsername = Environment.UserName;
            Log($"User is {GlueUsername}");

            // these envvars are expanded in some configuration files
            Environment.SetEnvironmentVariable("PROCESSID", Process.GetCurrentProcess().Id + "");
            Environment.SetEnvironmentVariable("GW_USERNAME", GlueUsername);
            //Environment.SetEnvironmentVariable("DEMO_MODE", Mode + "");

            // The Glue42 facade provides a simplified programming
            // interface over the core Glue42 components.
            Glue = new Glue42();

            Glue.Interop.ConnectionStatusChanged += (sender, args) => { Log($"Glue connection is now {args.Status}"); };
            Glue.Interop.EndpointStatusChanged += (sender, args) =>
            {
                // we can just interact with the ui since we have specified synchronization context

                if (!string.Equals(args.InteropEndpoint.Definition.Name, endpointName_,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    // ignore non-interesting endpoints
                    return;
                }

                bool activated = args.InteropEndpoint.IsValid;

                string status = activated ? "+++ Discovered" : "--- Disappeared";

                Log($"{status} advanced instance {args.InteropEndpoint.OriginalServer}");

                if (activated)
                {
                    AddTarget(args);
                }
                else
                {
                    RemoveTarget(args);
                }
            };

            var advancedOptions = new Glue42.AdvancedOptions {SynchronizationContext = synchronizationContext};
            
            Glue.Initialize(
                Assembly.GetEntryAssembly().GetName().Name, // application name - required
                useAgm: true,
                useAppManager: true,
                useMetrics: true,
                useContexts: false,
                useStickyWindows: false,
                credentials: Tuple.Create(GlueUsername, ""),
                advancedOptions: advancedOptions);

            Glue.Interop.RegisterEndpoint(mdb => mdb.SetMethodName(endpointName_),
                (method, context, caller, resultBuilder, asyncResponseCallback, cookie) =>
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Log($"Call from {caller}");
                        Value transactionValue = context.Arguments.GetValueByName("transaction", v => v);

                        // deserialize the transaction
                        Transaction transaction =
                            Glue.Interop.AGM.AGMObjectSerializer.Deserialize<Transaction>(transactionValue);

                        // prepare some values to get back to the caller as a result
                        string transactionId = Guid.NewGuid().ToString();

                        // do slow job here with context and caller
                        Thread.Sleep(500);

                        // respond when ready
                        asyncResponseCallback(resultBuilder
                            .SetMessage("Transaction queued")
                            .SetContext(cb => cb.AddValue("transactionId", transactionId)).Build());
                    }));

            Log("Initialized Glue Metrics and AGM");

            //Glue.Metrics.TrackUserJourneyMetrics(this, trackWindows: true, trackClicks: true);

            Log("Initialized Glue");
        }

        private void RemoveTarget(InteropEndpointStatusChangedEventArgs args)
        {
            targets_.Items.RemoveByKey(args.InteropEndpoint.OriginalServer.InstanceId);
        }

        private void AddTarget(InteropEndpointStatusChangedEventArgs args)
        {
            ListViewItem item = null;

            instanceProps_.Each(p =>
            {
                IInstance target = args.InteropEndpoint.OriginalServer;

                string text = p.GetValue(target)?.ToString();

                if (item == null)
                {
                    item = targets_.Items.Add(target.InstanceId, text, -1);
                    item.Tag = target;
                }
                else
                {
                    item.SubItems.Add(text);
                }
            });
        }

        private void MainFormShown(object sender, EventArgs e)
        {
            // get the form's synchronization context (gui thread) to be able to pass it for marshalling the events

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

        private void TargetsMouseDoubleClick(object sender, MouseEventArgs e)
        {
            var item = targets_.GetItemAt(e.X, e.Y);
            if (!(item?.Tag is IInstance target))
            {
                return;
            }

            // create a sample transaction
            var transaction = CreateTransaction();

            // invoke the target passing the transaction as an argument
            Glue.Interop.Invoke(endpointName_, builder => builder.AddObject("transaction", transaction),
                    // specify that we want the selected target only
                    new TargetSettings().WithTargetSelector((method, instance) =>
                        instance.InstanceId == target.InstanceId))
                .ContinueWith(cmrT =>
                {
                    if (cmrT.IsFaulted)
                    {
                        Log($"Invocation failed with {cmrT.Exception?.Flatten()}");
                        return;
                    }

                    IClientMethodResult cmr = cmrT.Result;
                    Log(
                        $"Server said {cmr.ResultMessage} with transactionId = {cmr.ResultContext.GetValueByName("transactionId", v => v.AsString)}");
                });
        }

        private Transaction CreateTransaction()
        {
            return new Transaction
            {
                Account = (long) (rnd_.NextDouble() * Math.Pow(10, rnd_.Next(5, 8))),
                Status = (TransactionStatus) rnd_.Next(3),
                Broker = GetRandomString(3),
                Currency = GetRandomString(3),
                TotalValue = rnd_.NextDouble() * Math.Pow(10, rnd_.Next(2, 5)),
                Emploee = GetRandomString(8).ToLower(),
                Security = new SecurityDetail
                {
                    Country = GetRandomString(3),
                    Cusip = GetRandomString(10),
                    Ratings = rnd_.Next()
                }
            };
        }

        private string GetRandomString(int length)
        {
            return new string(Enumerable.Range(0, length).Select(_ => (char) (65 + rnd_.Next(26))).ToArray());
        }

        // we can define a type with an 'arbitrary' complexity and use attributes to control serializaiton 
        // e.g. 
        //[ServiceContractType]
        //private class ComplexObject
        //{
        //    public IContextValue AGMContextValue;
        //    public Value AGMValue = Value.Null;
        //    public List<int> IntList;
        //    public KeyValuePair<string, KeyValuePair<string, bool>> KVPair;
        //    public Dictionary<int, string> Map;
        //    public KeyValuePair<string, KeyValuePair<int, bool>>[] Pairs;
        //    public Collection<double> Doubles { get; set; }
        //    public string[] StringArray { get; set; }
        //    public DateTime DateTime { get; set; }
        //    public bool Success { get; set; }
        //    public string Message { get; set; }

        //    [ServiceContractType(ObjectType = typeof(Instance))]
        //    public IInstance InstanceObject { get; set; }

        //    public Dictionary<string, IInstance> InstancesMap { get; set; }
        //    public ComplexObject RecursiveInner { get; set; }
        //}

        // but for now let's define a smaller type that we will use to send over the Glue42 wire

        #region Business objects

        public enum TransactionStatus
        {
            Pending,
            Passed,
            Failed
        }

        internal class SecurityDetail
        {
            public string Country { get; set; }
            public string Cusip { get; set; }
            public double Ratings { get; set; }
        }

        internal class Transaction
        {
            public SecurityDetail Security { get; set; }
            public TransactionStatus Status { get; set; }
            public double TotalValue { get; set; }

            public string Broker { get; set; }
            public string Currency { get; set; }
            public long Account { get; set; }
            public string Emploee { get; set; }
        }

        #endregion
    }
}