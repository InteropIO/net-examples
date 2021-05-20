using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tick42;
using Tick42.AppManager;
using Tick42.StartingContext;
using Tick42.Windows;

namespace WindowsFormsDemo
{
    public partial class FormChild2 : Form, IGlueApp<MyDateState, Form>
    {
        private readonly DateTime initialDateTime_;
        public FormChild2()
        {
            InitializeComponent();

            initialDateTime_ = DateTime.UtcNow;

            StartDateLabel.Text = initialDateTime_.ToString();
        }

        public void Shutdown()
        {
        }

        public void Initialize(Form context, MyDateState state, Glue42 glue, GDStartingContext startingContext, IGlueWindow glueWindow)
        {
            this.Invoke((Action) (() => StartDateLabel.Text = $"Started on: {state.Date}"));
        }

        public Task<MyDateState> GetState()
        {
            return Task.FromResult(new MyDateState() {Date = initialDateTime_});
        }
    }
}
