using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Extensions;
using Tick42;

namespace SimpleImperativeInteropDuplexChat
{
    public partial class MainForm : Form
    {
        private readonly string chatEndpointName_ = "SendSimpleTextMessage";

        public MainForm()
        {
            InitializeComponent();
        }

        public string GlueUsername { get; set; }

        public Glue42 Glue { get; set; }

        private void InitializeGlue()
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
                if (string.Equals(args.InteropEndpoint.Definition.Name, chatEndpointName_,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    string status = args.InteropEndpoint.IsValid ? "+++ Discovered" : "--- Disappeared";

                    Log($"{status} duplex chat instance {args.InteropEndpoint.OriginalServer}");
                }
            };
            Glue.Initialize(
                "InteropDuplex", // application name - required
                useAgm: true,
                useAppManager: true,
                useMetrics: true,
                useContexts: false,
                useStickyWindows: false,
                credentials: Tuple.Create(GlueUsername, ""));

            Glue.Interop.RegisterEndpoint(mdb => mdb.SetMethodName(chatEndpointName_),
                (method, context, caller, resultBuilder, asyncResponseCallback, cookie) =>
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        // simulate asynchronous work
                        // grab the invocation context (e.g. take "id" from the input arguments)
                        // we don't care about the id type, so get it as Value
                        Value id = context.Arguments.GetValueByName("id", v => v);
                        // we care about the message type - we take it as a string
                        var message = context.Arguments.GetValueByName("message", v => v.AsString);
                        Log($"Call from {caller} with id {id}: {message}");

                        // do slow job here with context and caller
                        Thread.Sleep(500);

                        // respond when ready
                        asyncResponseCallback(resultBuilder.SetMessage(id + " processed")
                            .SetContext(cb => cb.AddValue("id", id)).Build());
                    }));

            Log("Initialized Glue Metrics and AGM");

            //Glue.Metrics.TrackUserJourneyMetrics(this, trackWindows: true, trackClicks: true);

            Log("Initialized Glue");
        }

        private void TxtMsgKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Control)
            {
                string txtMsgText = txtMsg_.Text;
                txtMsg_.Clear();
                SendMessage(txtMsgText);
            }
        }

        private void SendMessage(string message)
        {
            // send a call to all targets that implement that endpoint, passing some id and the message.

            Glue.Interop.Invoke(chatEndpointName_, mib => mib
                        // set the invocation context (any arguments to be sent over to the targets)
                        .SetContext(cb => cb
                            // e.g. we can add some id
                            .AddValue("id", Guid.NewGuid().ToString("N"))
                            // and the time
                            .AddValue("time", DateTime.UtcNow)
                            // and then a message
                            .AddValue("message", message)),
                    new TargetSettings()
                        // timeout if a target has not responsed in 8 seconds
                        .WithTargetInvokeTimeout(TimeSpan.FromSeconds(8))
                        // invoke all available targets
                        .WithTargetType(MethodTargetType.All))
                .ContinueWith(
                    r =>
                    {
                        // consume the result

                        IClientMethodResult result = r.Result;

                        // since we're sending an 'All' call, we have to check the inner results (per target)
                        foreach (var inner in result.InnerClientMethodResults)
                        {
                            //Log($"{inner.Server}: Result arrived for: {inner.ResultContext.First(cv => cv.Name == "id").Value}");
                        }
                    });
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

        private void MainFormShown(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    InitializeGlue();
                }
                catch (Exception exception)
                {
                    Log("Failed initializing Glue");
                    Log(exception.ToString());
                }
            });
        }
    }
}