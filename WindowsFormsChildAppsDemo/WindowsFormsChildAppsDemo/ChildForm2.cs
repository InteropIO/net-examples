using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tick42;
using Tick42.AppManager;
using Tick42.StartingContext;
using Tick42.Windows;

namespace WindowsFormsChildAppsDemo
{
    public partial class ChildForm2 : Form, IGlueApp<MyDateState, Form>
    {
        private readonly DateTime initialDateTime_;

        public ChildForm2()
        {
            InitializeComponent();

            initialDateTime_ = DateTime.UtcNow;

            StartDateLabel.Text = initialDateTime_.ToString();
        }

        public void Shutdown()
        {
        }

        public void Initialize(Form context, MyDateState state, Glue42 glue, GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            if (state?.Date is DateTime date)
            {
                StartDateLabel.Text = $"Started on: {state.Date}";
            }
        }

        public Task<MyDateState> GetState()
        {
            return Task.FromResult(new MyDateState {Date = initialDateTime_});
        }
    }
}