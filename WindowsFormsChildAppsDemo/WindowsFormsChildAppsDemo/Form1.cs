using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Tick42;
using Tick42.AppManager;
using Tick42.StartingContext;
using Tick42.Windows;

namespace WindowsFormsChildAppsDemo
{
    public partial class Form1 : Form
    {
        private Glue42 glue_;

        public Form1()
        {
            Debugger.Launch();
            InitializeComponent();

            var initOptions = new InitializeOptions
            {
                ApplicationName = "MyWinFormsApp",
                AppDefinition = new AppDefinition
                {
                    IgnoreFromLayouts = true,
                    TerminateOnShutdown = true,
                }
            };

            //the lambda will be called when save layout is called
            initOptions.SetSaveRestoreStateEndpoint(v =>
            {
                //MyState is an arbitrary complex object in which you can save any type of data and restore it later
                return Task.FromResult(new MyState { Text = StateBox.Text, DateSaved = DateTime.UtcNow });
            });

            initOptions.OnShutdownRequested = () => { return Task.FromResult(false); };

            Glue42.InitializeGlue(initOptions)
                .ContinueWith(glue =>
                {
                    glue_ = glue.Result;

                    //restore the state when you need it
                    var restoredState = glue_.GetRestoreState<MyState>();

                    if (restoredState != null)
                    {
                        Invoke((Action)(() => StateBox.Text = restoredState.Text));
                    }

                    Dispatch(async () =>
                        await glue_.GlueWindows.RegisterStartupWindow(Handle, "MyWinformsChildrenFactory"));

                    var thread = new Thread(() =>
                    {
                        glue_.AppManager.RegisterWinFormsApp<ChildForm, MyChildState, Form>(builder =>
                            builder.WithTitle("WinFormChild"));
                        Dispatcher.Run();
                    })
                    {
                        IsBackground = true
                    };
                    thread.Start();

                    var thread2 = new Thread(() =>
                    {
                        glue_.AppManager.RegisterWinFormsApp<ChildForm2, MyDateState, Form>(builder =>
                            builder.WithTitle("WinFormChild2"));
                        Dispatcher.Run();
                    })
                    {
                        IsBackground = true
                    };
                    thread2.Start();

                    for (int i = 0; i < 2; ++i)
                    {
                        FormAppHandle.RegisterApp("AppHandle_" + (i + 1), glue_.AppManager);
                    }

                    //Dispatch(() => glue_.AppManager.RegisterWinFormsApp<ChildForm, MyChildState, Form>(builder =>
                    //    builder.WithTitle("WinFormChild")));

                    //Dispatch(() => glue_.AppManager.RegisterWinFormsApp<ChildForm2, MyDateState, Form>(builder =>
                    //    builder.WithTitle("WinFormChild2")));
                });
        }

        private object Dispatch(Action action)
        {
            return Invoke(action);
        }

        private class FormAppHandle : IGlueAppHandle<object, object>
        {
            private static readonly Task<object> State = Task.FromResult(new object());

            private readonly Form _form;

            private readonly IntPtr _handle;

            private IDisposable _channelSubscription;

            public FormAppHandle(Form form)
            {
                _form = form;
                _handle = _form.Handle;
                _form.FormClosed += (_, __) => { _channelSubscription?.Dispose(); };
            }

            public IntPtr GetHandle() => _handle;

            public void Shutdown()
            {
                if (_form.IsDisposed)
                {
                    return;
                }
                _form.Invoke((Action)(() => _form.Close()));
            }

            public void Initialize(
                object context,
                object state,
                Glue42 glue,
                GDStartingContext startingContext,
                IGlueWindow glueWindow)
            {
                //_channelSubscription = GlueChannelSubscriber.Subscribe(_form, glueWindow);
            }

            public Task<object> GetState() => State;

            internal static IDisposable RegisterApp(
                string app,
                IAppManager appManager)
            {
                try
                {
                    return appManager.RegisterAppFactory<object, FormAppHandle, object, object>(
                        builder =>
                        {
                            builder.WithName(app)
                                .WithTitle(app)
                                .WithFolder("handles")
                                .WithAllowMultiple(true)
                                .WithChannelSupport(true);
                        },
                        (context, builder, key) =>
                        {
                            try
                            {
                                var tcs = new TaskCompletionSource<FormAppHandle>();
                                Task.Run(() =>
                                {
                                    var form = new Form
                                    {
                                        Text = app
                                    };
                                    form.Show();
                                    form.Closed += (sender, args) => Dispatcher.ExitAllFrames();
                                    tcs.TrySetResult(new FormAppHandle(form));
                                    Dispatcher.Run();
                                });
                                return tcs.Task;
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        });
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
    }
}