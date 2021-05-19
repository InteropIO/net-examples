using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
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
        public Form1()
        {
            InitializeComponent();

            Debugger.Launch();

            var initOptions = new InitializeOptions() { ApplicationName = "MyWinFormsApp" };
            //the lambda will be called when save layout is called
            initOptions.SetSaveRestoreStateEndpoint(v => Task.FromResult(StateBox.Text));

            Glue42.InitializeGlue(initOptions)
                .ContinueWith(async glue =>
                {
                    glue_ = glue.Result;

                    //this will be called when the layout is restored
                    var restoredState = glue_.GetRestoreState<string>();

                    if (restoredState != null)
                    {
                        this.Invoke((Action)(() => StateBox.Text = restoredState));
                    }

                    var window = await glue_.GlueWindows.RegisterWindow(this.Handle, new GlueWindowOptions() { Title = "MyWinformsApp" });
                });
        }
    }
}
