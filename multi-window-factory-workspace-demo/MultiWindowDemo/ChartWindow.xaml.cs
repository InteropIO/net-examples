using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tick42;
using Tick42.AppManager;
using Tick42.Channels;
using Tick42.Contexts;
using Tick42.StartingContext;
using Tick42.Windows;
using Tick42.Workspaces;
using static MultiWindowFactoryDemo.App;

namespace MultiWindowFactoryDemo;

// The window implements the IGlueApp interface and indicates the shape of the state which will be used and the context which in this case is the MainWindow
public partial class ChartWindow : Window, IGlueApp<ChartWindow.SymbolState, AppFactoryContext>,
    IGlueChannelEventHandler<ChartWindow.Instrument>
{
    private readonly SemaphoreSlim cxtSem_ = new(1, 1);
    private readonly Random random_ = new();
    private Glue42 glue_;
    private IGlueWindow glueWindow_;

    private IDisposable subscription_;
    private IContextWrapper workspaceContext_;
    private IDisposable workspaceEvents_;

    public ChartWindow()
    {
        InitializeComponent();
        Symbol.Text = RandomSymbol();
    }

    public async Task<SymbolState> GetState()
    {
        // Returning the state which will be saved when the window is saved in a layout
        // The state of the app is the currently selected symbol
        return Dispatcher.Invoke(() =>
        {
            var state = new SymbolState
            {
                ActiveSymbol = Symbol.Text
            };

            return state;
        });
    }

    public async void Initialize(AppFactoryContext context, SymbolState state, Glue42 glue,
        GDStartingContext startingContext,
        IGlueWindow glueWindow)
    {
        // do something based on the app type
        switch (context.AppType)
        {
            case AppType.ChartOne:
                break;
            case AppType.ChartTwo:
                break;
            case AppType.ChartThree:
                break;
            case AppType.ChartFour:
                break;
            case AppType.ChartFive:
                break;
        }

        glueWindow_ = glueWindow;
        glue_ = glue;
        workspaceEvents_ = await glue_.Workspaces.SubscribeWindowEvent(we =>
        {
            if (we.WindowId == glueWindow.Id)
            {
                HandleWorkspaceEvent(we);
            }
        });
        // Invoked when the window is restored
        Dispatcher.Invoke(() => { Symbol.Text = state?.ActiveSymbol ?? RandomSymbol(); });
    }

    public void Shutdown()
    {
        Close();
    }

    void IGlueChannelEventHandler<Instrument>.HandleUpdate(IGlueChannelContext channelContext,
        ChannelUpdateInfo updateInfo, Instrument data)
    {
        Dispatcher.Invoke(() => { Symbol.Text = data.Symbol + " " + data.PublishedOn; });
    }

    void IGlueChannelEventHandler.HandleChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel,
        IGlueChannel prevChannel)
    {
        // ignored
    }

    void IGlueChannelEventHandler.HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel,
        ContextUpdatedEventArgs updateArgs)
    {
        // ignored
    }

    protected override void OnClosed(EventArgs e)
    {
        // cleanup
        base.OnClosed(e);
        ChartControl.Dispose();
        Interlocked.Exchange(ref subscription_, null)?.Dispose();
        Interlocked.Exchange(ref workspaceEvents_, null)?.Dispose();
    }

    string RandomWord() => new(Enumerable.Range(0, random_.Next(3, 8))
        .Select(__ => (char)random_.Next('a', 'z' + 1)).ToArray());

    string GenerateRic(string symbol) =>
        $"{symbol.ToUpper()}.{new[] { "L", "O", "N", "PA", "DE", "HK" }[random_.Next(6)]}";

    string RandomSymbol() => GenerateRic(RandomWord());

    private void HandleWorkspaceEvent(WorkspaceEvent we)
    {
        if (we.Action != WindowEventAction.Loaded && we.Action != WindowEventAction.Added && we.Action != WindowEventAction.Removed)
        {
            return;
        }

        _ = cxtSem_.InSemaphore(async () =>
        {
            if (we.Action == WindowEventAction.Removed)
            {
                Interlocked.Exchange(ref subscription_, null)?.Dispose();
                workspaceContext_ = null;
            }
            else
            {
                if (await glue_.Workspaces.GetWorkspaceContext(glueWindow_) is { } context)
                {
                    workspaceContext_ = await glue_.Contexts.GetContextWrapper(context.ContextName);
                    subscription_ = workspaceContext_.Subscribe(this, "market.instrument");

                    // NOTE: if required - query metadata for the workspace
                    var workspace = await glue_.Workspaces.GetWorkspaceSnapshot(we.WorkspaceId);
                    var layoutName = workspace.LayoutName;
                    var workspaceTitle = workspace.Title;
                    var windows = workspace.Windows;
                }
            }
        });
    }

    private void Switch_Click(object sender, RoutedEventArgs e)
    {
        string symbol = RandomSymbol();
        Symbol.Text = symbol;
        // let's publish in the workspace context as well
        workspaceContext_?.SetValue(new Instrument
        {
            Symbol = symbol,
            PublishedOn = DateTime.Now
        }, "market.instrument");
    }

    // The shape of the state which will be used when the window is being saved or restored
    public class SymbolState
    {
        public string ActiveSymbol { get; set; }
    }

    public class Instrument
    {
        public string Symbol { get; set; }
        public DateTime PublishedOn { get; set; }
    }
}