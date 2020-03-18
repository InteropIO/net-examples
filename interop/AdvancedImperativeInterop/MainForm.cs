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
using Tick42.StartingContext;

namespace AdvancedImperativeInterop
{
    public partial class MainForm : Form
    {
        private readonly string endpointName_ = "HandleCompositeObject";
        private readonly PropertyInfo[] instanceProps_ = typeof(IInstance).GetProperties().Where(p => p.PropertyType == typeof(string)).ToArray();
        private readonly Random rnd_ = new Random();

        public Glue42 _glue42;

        public MainForm()
        {
            InitializeComponent();
            InitializeGlue();

            instanceProps_.Each(p => targets_.Columns.Add(p.Name));
        }

        private void InitializeGlue()
        {
            // get the form's synchronization context (gui thread) to be able to pass it for marshalling the events

            SynchronizationContext synchronizationContext = SynchronizationContext.Current;

            // initialize Tick42 Interop
            Log("Initializing Glue42");

            var initializeOptions = new InitializeOptions()
            {
                ApplicationName = "Hello Glue42 Advanced Imperative Interop",
                InitializeTimeout = TimeSpan.FromSeconds(5),
                AdvancedOptions = new Glue42.AdvancedOptions() {  SynchronizationContext = synchronizationContext }
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
                    _glue42.Interop.ConnectionStatusChanged += (s, args) => { Log($"Glue connection is now {args.Status}"); };
                    _glue42.Interop.EndpointStatusChanged += InteropEndpointStatusChanged;

                    _glue42.Interop.RegisterEndpoint(mdb => mdb.SetMethodName(endpointName_),
                        (method, context, caller, resultBuilder, asyncResponseCallback, cookie) =>
                           ThreadPool.QueueUserWorkItem(_ =>
                           {
                               Log($"Call from {caller}");
                               Value transactionValue = context.Arguments.GetValueByName("transaction", v => v);

                                // deserialize the transaction
                                Transaction transaction =
                                   _glue42.Interop.AGM.AGMObjectSerializer.Deserialize<Transaction>(transactionValue);

                                // prepare some values to get back to the caller as a result
                                string transactionId = Guid.NewGuid().ToString();

                                // do slow job here with context and caller
                                Thread.Sleep(500);

                                // respond when ready
                                asyncResponseCallback(resultBuilder
                                   .SetMessage("Transaction queued")
                                   .SetContext(cb => cb.AddValue("transactionId", transactionId)).Build());
                           }));

                    Log("Initialized Glue.");
                });
        }

        private void InteropEndpointStatusChanged(object sender, InteropEndpointStatusChangedEventArgs e)
        {
            // we can just interact with the ui since we have specified synchronization context
            if (!string.Equals(e.InteropEndpoint.Definition.Name, endpointName_, StringComparison.CurrentCultureIgnoreCase))
            {
                // ignore non-interesting endpoints
                return;
            }

            bool activated = e.InteropEndpoint.IsValid;
            string status = activated ? "+++ Discovered" : "--- Disappeared";

            Log($"{status} advanced instance {e.InteropEndpoint.OriginalServer}");

            if (activated)
            {
                AddTarget(e);
            }
            else
            {
                RemoveTarget(e);
            }
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
            _glue42.Interop.Invoke(endpointName_, builder => builder.AddObject("transaction", transaction),
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