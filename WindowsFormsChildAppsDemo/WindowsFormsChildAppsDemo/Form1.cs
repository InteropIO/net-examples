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

namespace WindowsFormsChildAppsDemo
{
    public partial class Form1 : Form
    {
        private Glue42 glue_;
        public Form1()
        {
            InitializeComponent();

            var initOptions = new InitializeOptions() { ApplicationName = "MyWinFormsApp" };
            //the lambda will be called when save layout is called
            initOptions.SetSaveRestoreStateEndpoint(v =>
            {
                //MyState is an arbitrary complex object in which you can save any type of data and restore it later
                return Task.FromResult(new MyState() { Text = StateBox.Text, DateSaved = DateTime.UtcNow });
            });

            Glue42.InitializeGlue(initOptions)
                .ContinueWith(glue =>
                {
                    glue_ = glue.Result;

                    //restore the state when you need it
                    var restoredState = glue_.GetRestoreState<MyState>();

                    if (restoredState != null)
                    {
                        this.Invoke((Action)(() => StateBox.Text = restoredState.Text));
                    }

                    Dispatch(async () => await glue_.GlueWindows.RegisterWindow(this.Handle, new GlueWindowOptions() { Title = "MyWinformsChildrenFactory" }));

                    Dispatch(() => glue_.AppManager.RegisterWinFormsApp<ChildForm, MyChildState, Form>(builder =>
                        builder.WithTitle("WinFormChild")));

                    Dispatch(() => glue_.AppManager.RegisterWinFormsApp<ChildForm2, MyDateState, Form>(builder =>
                        builder.WithTitle("WinFormChild2")));
                });
        }

        private object Dispatch(Action action)
        {
            return this.Invoke(action);
        }
    }
}
