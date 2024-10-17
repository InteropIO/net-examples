using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DOT.AGM.GwTransport;
using Microsoft.Web.WebView2.Core;
using Tick42;
using Tick42.StartingContext;
using Tick42.Windows;

namespace GlueWebView2
{
    [ComVisible(true)]
    public class WebView2GlueContext
    {
        private readonly string windowId_;
        private string callbackRegistration_;
        private string environment_;
        private string gwUri_;
        private string identityUserName_;
        private string region_;
        private IGwProtocolSerializer serializer_;
        private string token_;
        private CoreWebView2 webView_;

        public WebView2GlueContext(CoreWebView2 webView,
            IGwProtocolSerializer serializer,
            string windowId,
            string environment,
            string region,
            string gwUri,
            string username,
            string token)
        {
            webView_ = webView;
            windowId_ = windowId;

            serializer_ = serializer;
            region_ = region;
            environment_ = environment;
            gwUri_ = gwUri;
            identityUserName_ = username;
            token_ = token;
        }

        public void RegisterCallback(string callbackId)
        {
            callbackRegistration_ = callbackId;
        }

        public string GetGwUri() => gwUri_;

        public string GetUsername() => identityUserName_;

        public string GetEnvironment() => environment_;

        public string GetRegion() => region_;

        public string GetWindowId() => windowId_;

        public string CreateGwToken() => token_;

        public void Notify(object data)
        {
            if (string.IsNullOrEmpty(callbackRegistration_))
            {
                return;
            }

            var message = serializer_.SerializeMessage(new
            {
                @event = "callback",
                callbackId = callbackRegistration_,
                data
            });
            webView_.PostWebMessageAsJson(message);
        }
    }

    public partial class Form1 : Form
    {
        private IGlueWindow window_;
        private WebView2GlueContext wv2Context_;

        public Form1()
        {
            InitializeComponent();
            // true to initialize glue, false to use the discovery service
            InitGlueAndWebView2(false).ContinueWith(_ => { });
        }

        private async Task InitGlueAndWebView2(bool initGlue)
        {
            textBox1.Text = "http://localhost:8080/index.html";
            textBox1.ReadOnly = true;

            CreateHandle();

            Text = "Registering Glue";
            CancellationTokenSource ct = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(250, ct.Token);

                    void Progress()
                    {
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }

                        Text += ".";
                    }

                    BeginInvoke((Action)Progress);
                }
            }, ct.Token);

            GDStartingContext gdsc;
            IGwProtocolSerializer gwp = new GwProtocolSerializer(ValueGwConverter.Settings.None);
            string gwToken = null;
            if (!initGlue)
            {
                var ds = new FileGlueDesktopDiscoveryService();
                GDStartingContext[] gdscs = await ds.Discover();
                // choose some gdsc
                gdsc = gdscs.FirstOrDefault();
            }
            else
            {
                var glue = await Glue42.InitializeGlue();
                window_ = await glue.GlueWindows.RegisterStartupWindowByHandle(Handle, "Glue WebView2",
                    o => o.WithChannelSupport(true));

                // we can create an auth token here and expose it instead of the username
                gdsc = glue.GDStartingContext;
            }

            ct.Cancel();
            Text = "Glue " + (gdsc != null ? "found: " + gdsc.GwURL : "not found");

            if (gdsc == null)
            {
                // no reason to proceed
                return;
            }

            webView21.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                wv2Context_ = new WebView2GlueContext(webView21.CoreWebView2, gwp, window_?.Id,
                    gdsc.Env, gdsc.Region, gdsc.GwURL,
                    gdsc?.Username ?? gdsc?.GDInstanceConfig?.Auth?.UserName ?? Environment.UserName, gwToken);
                webView21.CoreWebView2.AddHostObjectToScript("glue", wv2Context_);
                textBox1.ReadOnly = false;
            };
            CoreWebView2Environment wv2Env = await CoreWebView2Environment.CreateAsync();
            await webView21.EnsureCoreWebView2Async(wv2Env);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (textBox1.ReadOnly)
            {
                return;
            }

            if (e.KeyChar == (char)Keys.Enter)
            {
                webView21.CoreWebView2.Navigate(textBox1.Text);
            }
        }
    }
}