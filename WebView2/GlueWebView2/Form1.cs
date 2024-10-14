using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Tick42;
using Tick42.Windows;

namespace GlueWebView2
{
    [ComVisible(true)]
    public class WebView2GlueContext
    {
        private readonly Glue42 glue_;
        private readonly string windowId_;
        private string callbackRegistration_;
        private CoreWebView2 webView_;
        private string token_;

        public WebView2GlueContext(CoreWebView2 webView, Glue42 glue, string windowId)
        {
            webView_ = webView;
            glue_ = glue;
            windowId_ = windowId;

            // we can create an auth token here and expose it instead of the username
        }

        public void RegisterCallback(string callbackId)
        {
            callbackRegistration_ = callbackId;
        }

        public string GetGwUri() => glue_.GatewayUri;

        public string GetUsername() => glue_.Identity.UserName;

        public string GetIdentity() => glue_.ProtocolSerializer.SerializeMessage(glue_.Identity);

        public string GetWindowId() => windowId_;

        public string CreateGwToken() => token_;

        public void Notify(object data)
        {
            if (string.IsNullOrEmpty(callbackRegistration_))
            {
                return;
            }

            var message = glue_.ProtocolSerializer.SerializeMessage(new
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
            InitGlueAndWebView2().ContinueWith(_ => { });
        }

        private async Task InitGlueAndWebView2()
        {
            textBox1.Text = "http://localhost:8080/index.html";
            textBox1.ReadOnly = true;

            Text = "Registering Glue";
            CancellationTokenSource ct = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(250, ct.Token);
                    void Progress() => Text += ".";
                    BeginInvoke((Action)Progress);
                }
            }, ct.Token);
            var glue = await Glue42.InitializeGlue();
            ct.Cancel();
            Text = "Glue initialized";
            window_ = await glue.GlueWindows.RegisterStartupWindowByHandle(Handle, "Glue WebView2",
                o => o.WithChannelSupport(true));
            webView21.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                wv2Context_ = new WebView2GlueContext(webView21.CoreWebView2, glue, window_.Id);
                webView21.CoreWebView2.AddHostObjectToScript("glue", wv2Context_);
                textBox1.ReadOnly = false;
            };
            CoreWebView2Environment wv2Env = await CoreWebView2Environment.CreateAsync();
            await webView21.EnsureCoreWebView2Async(wv2Env);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                webView21.CoreWebView2.Navigate(textBox1.Text);
            }
        }
    }
}