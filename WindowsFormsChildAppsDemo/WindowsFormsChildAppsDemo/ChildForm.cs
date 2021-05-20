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

namespace WindowsFormsChildAppsDemo
{
    public partial class ChildForm : Form, IGlueApp<MyChildState, Form>
    {
        public ChildForm()
        {
            InitializeComponent();
        }

        private void redColorBtn_Click(object sender, EventArgs e)
        {
            this.BackColor = Color.Red;
        }

        public void Shutdown()
        {
        }

        public void Initialize(Form context, MyChildState state, Glue42 glue, GDStartingContext startingContext, IGlueWindow glueWindow)
        {
            this.Invoke((Action)(() => this.BackColor = Color.FromName(state.Color)));
        }

        public Task<MyChildState> GetState()
        {
            return Task.FromResult(new MyChildState() { Color = "Red" });
        }
    }
}
