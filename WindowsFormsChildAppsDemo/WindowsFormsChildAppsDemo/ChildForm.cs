using System;
using System.Drawing;
using System.Linq;
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
        private readonly Random rnd_ = new Random();

        public ChildForm()
        {
            InitializeComponent();
        }

        public void Shutdown()
        {
            Close();
        }

        public void Initialize(Form context, MyChildState state, Glue42 glue, GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            if (state?.Color is string color)
            {
                BackColor = Color.FromName(color);
            }
        }

        public Task<MyChildState> GetState()
        {
            return Task.FromResult(new MyChildState {Color = BackColor.Name});
        }

        private void BtnRndColorClick(object sender, EventArgs e)
        {
            var colors = Enum.GetValues(typeof(KnownColor)).Cast<KnownColor>().ToArray();
            BackColor = Color.FromKnownColor(colors[rnd_.Next(colors.Length)]);
        }
    }
}