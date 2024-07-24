using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tick42;
using Tick42.AppManager;
using Tick42.Channels;
using Tick42.StartingContext;
using Tick42.Windows;

namespace WindowsFormsChildAppsDemo
{
    public partial class ChildForm2 : Form, IGlueApp<MyDateState, Form>
    {
        private readonly DateTime initialDateTime_;
        private IGlueWindow glueWindow_;
        private IDisposable subscription_;

        public ChildForm2()
        {
            InitializeComponent();

            initialDateTime_ = DateTime.UtcNow;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;
            dataGridView1.DataSource = new[]
            {
                new { Name = "John", Age = 25 },
                new { Name = "Jane", Age = 22 },
                new { Name = "Doe", Age = 30 },
                new { Name = "Smith", Age = 35 },
                new { Name = "Brown", Age = 40 },
                new { Name = "Johnson", Age = 45 },
                new { Name = "Williams", Age = 50 },
                new { Name = "Jones", Age = 55 },
                new { Name = "Miller", Age = 60 },
                new { Name = "Davis", Age = 65 },
                new { Name = "Garcia", Age = 70 },
                new { Name = "Rodriguez", Age = 75 },
                new { Name = "Wilson", Age = 80 },
                new { Name = "Martinez", Age = 85 },
                new { Name = "Anderson", Age = 90 },
                new { Name = "Taylor", Age = 95 },
                new { Name = "Thomas", Age = 100 },
                new { Name = "Hernandez", Age = 105 },
                new { Name = "Moore", Age = 110 },
                new { Name = "Martin", Age = 115 },
                new { Name = "Jackson", Age = 120 },
                new { Name = "Thompson", Age = 125 },
                new { Name = "White", Age = 130 },
                new { Name = "Lopez", Age = 135 },
                new { Name = "Lee", Age = 140 },
                new { Name = "Gonzalez", Age = 145 },
                new { Name = "Harris", Age = 150 },
                new { Name = "Clark", Age = 155 },
                new { Name = "Lewis", Age = 160 },
                new { Name = "Robinson", Age = 165 },
                new { Name = "Walker", Age = 170 },
                new { Name = "Perez", Age = 175 },
                new { Name = "Hall", Age = 180 },
                new { Name = "Young", Age = 185 },
                new { Name = "Allen", Age = 190 },
                new { Name = "Sanchez", Age = 195 },
                new { Name = "Wright", Age = 200 },
                new { Name = "King", Age = 205 },
                new { Name = "Scott", Age = 210 },
            };

            StartDateLabel.Text = initialDateTime_.ToString();
        }

        public void Shutdown()
        {
            Close();
            // the glueWindow_ and subscription_ are disposed by the Glue42 library
        }

        public void Initialize(Form context, MyDateState state, Glue42 glue, GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            glueWindow_ = glueWindow;

            SubscribePoi((channelContext, info, poi) =>
            {
                // since we are subscribing to the root of the context tree
                // we have to dig in the data to find the actual poi
                // if we had subscribed to a specific part of the context tree (e.g. "poi")
                // then we could receive the actual poi directly

                DataGridViewRow tr = dataGridView1.Rows.Cast<DataGridViewRow>()
                    .FirstOrDefault(r => string.Equals(GetTValue(r, new { Name = "", Age = 0 }).Name, poi.poi.Name,
                        StringComparison.InvariantCultureIgnoreCase));

                if (tr != null && !tr.Selected)
                {
                    richTextBox1.AppendText($"Selecting {poi.poi.Name}{Environment.NewLine}");
                    dataGridView1.SuspendLayout();
                    dataGridView1.ClearSelection();
                    tr.Selected = true;
                    dataGridView1.ResumeLayout();
                }
            }, new { poi = new { Name = "", Age = 0 } });

            if (state?.Date is DateTime date)
            {
                StartDateLabel.Text = $"Started on: {state.Date}";
            }
        }

        public Task<MyDateState> GetState()
        {
            return Task.FromResult(new MyDateState { Date = initialDateTime_ });
        }

        private static T GetTValue<T>(DataGridViewRow r, T _) where T : class
        {
            return r.DataBoundItem as T;
        }

        private async void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (glueWindow_ == null)
            {
                return;
            }

            if (dataGridView1.CurrentRow is DataGridViewRow row && row.Selected)
            {
                object currentRowDataBoundItem = row.DataBoundItem;

                // this publishes in another part of the channel's context tree
                // which will trigger our context subscription, since it's affecting the root of the tree
                // you will then notice that the previous row is being selected in the grid
                // and after that: the newly selected one
                await glueWindow_.ChannelContext.SetValue(Guid.NewGuid().ToString("N"), "guid");

                // then our data inside "poi" will be updated
                await glueWindow_.ChannelContext.SetValue(currentRowDataBoundItem, "poi");
            }
        }

        private void SubscribePoi<T>(Action<IGlueChannelContext, ChannelUpdateInfo, T> onUpdate, T _)
        {
            // notice that we subscribe to the root of the channel's context tree (dottedFieldPath: null)
            // this means that we will receive all updates for channel's context tree (in any node)
            // if you want to receive updates only for a specific part of the context tree
            // you can specify a dotted field path - in our case "poi"

            // if you specify "poi" as a dotted field path, you will receive only updates for the "poi" part of the context tree
            // then just change the handling code to receive directly the poi object instead an object with poi property inside
            subscription_ =
                glueWindow_.ChannelContext.Subscribe(new LambdaGlueChannelEventHandler<T>(onUpdate),
                    dottedFieldPath: null);
        }
    }
}