using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Extensions;
using Tick42;
using Tick42.StartingContext;

namespace SimpleImperativeInteropDuplexChat
{
    public partial class MainForm : Form
    {
        private readonly string _chatEndpointName = "SendSimpleTextMessage";
        public Glue42 _glue42;

        public MainForm()
        {
            InitializeComponent();
            InitializeGlue();
        }

        private void InitializeGlue()
        {
            // initialize Tick42 Interop
            Log("Initializing Glue42");

            var initializeOptions = new InitializeOptions()
            {
                ApplicationName = "Hello Glue42 Imperative Duplex Chat",
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
                    _glue42.Interop.EndpointStatusChanged += InteropEndpointStatusChanged;

                    _glue42.Interop.RegisterEndpoint(mdb => mdb.SetMethodName(_chatEndpointName),
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

                    //Enable textbox for send of messages
                    BeginInvoke((Action)(() => txtMsg_.Enabled = true));

                    Log("Initialized Glue.");
                });
        }

        private void InteropEndpointStatusChanged(object sender, InteropEndpointStatusChangedEventArgs e)
        {
            if (string.Equals(e.InteropEndpoint.Definition.Name, _chatEndpointName, StringComparison.CurrentCultureIgnoreCase))
            {
                string status = e.InteropEndpoint.IsValid ? "+++ Discovered" : "--- Disappeared";

                Log($"{status} duplex chat instance {e.InteropEndpoint.OriginalServer}");
            }
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
            _glue42.Interop.Invoke(_chatEndpointName, mib => mib
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
                        if (r.IsFaulted)
                        {
                            Log($"Invocation failed with {r.Exception?.Flatten()}");
                            return;
                        }
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
    }
}