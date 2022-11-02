using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tick42;
using Tick42.StartingContext;
using Tick42.Windows;

namespace WindowsFormsDemo
{
    public partial class Form1 : Form
    {
        private Glue42 glue_;
        private IGlueWindow glueWindow_;
        public Form1()
        {
            InitializeComponent();

            var initOptions = new InitializeOptions { ApplicationName = "MyWinFormsApp", AwaitAndTrackGlue = false };
            //the lambda will be called when save layout is called
            initOptions.SetSaveRestoreStateEndpoint(v =>
            {
                //MyState is an arbitrary complex object in which you can save any type of data and restore it later
                return Task.FromResult(new MyState { Text = StateBox.Text, DateSaved = DateTime.UtcNow });
            });

            Glue42.InitializeGlue(initOptions)
                .ContinueWith(async glue =>
                {
                    glue_ = glue.Result;

                    //restore the state when you need it
                    var restoredState = glue_.GetRestoreState<MyState>();

                    if (restoredState != null)
                    {
                        Invoke((Action)(() => StateBox.Text = restoredState.Text));
                    }

                    // we can take the handle here because we have passed task scheduler
                    // alternatively we can obtain the handle before initializing Glue
                    glueWindow_ = await glue_.GlueWindows.RegisterStartupWindow(Handle, "My WinForms App");
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}