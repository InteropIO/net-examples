using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tick42;
using Tick42.AppManager;
using Tick42.Channels;
using Tick42.Contexts;
using Tick42.StartingContext;
using Tick42.Windows;

namespace WindowsFormsChildAppsDemo
{
    public partial class ChildForm : Form, IGlueApp<MyChildState, Form>, IGlueChannelEventHandler
    {
        private readonly Random rnd_ = new Random();
        private IGlueWindow glueWindow_;

        public ChildForm()
        {
            InitializeComponent();
        }

        public void Shutdown()
        {
            // the glueWindow_ is disposed by the Glue42 library
            Close();
        }

        public void Initialize(Form context, MyChildState state, Glue42 glue, GDStartingContext startingContext,
            IGlueWindow glueWindow)
        {
            glueWindow_ = glueWindow;
            if (state?.Color is string color)
            {
                BackColor = Color.FromName(color);
            }

            glueWindow.ChannelContext.Subscribe(this);
        }

        public Task<MyChildState> GetState()
        {
            return Task.FromResult(new MyChildState { Color = BackColor.Name });
        }

        public void HandleChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel,
            IGlueChannel prevChannel)
        {
            if (IsDisposed)
            {
                throw new Exception();
            }

            if (newChannel?.Name is string color && Enum.TryParse<KnownColor>(color, true, out var c))
            {
                BackColor = Color.FromKnownColor(c);
            }
            else
            {
                BtnRndColorClick(this, EventArgs.Empty);
            }
        }

        public void HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel,
            ContextUpdatedEventArgs updateArgs)
        {
            if (IsDisposed)
            {
                throw new Exception();
            }
        }

        private void BtnRndColorClick(object sender, EventArgs e)
        {
            var colors = Enum.GetValues(typeof(KnownColor)).Cast<KnownColor>().ToArray();
            BackColor = Color.FromKnownColor(colors[rnd_.Next(colors.Length)]);
        }
    }
}