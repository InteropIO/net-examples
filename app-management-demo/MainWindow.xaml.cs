﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tick42;
using Tick42.AppManager;
using Tick42.StickyWindows;

namespace AppManagerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Glue42 glue42_;

        public MainWindow()
        {
            InitializeComponent();
            glue42_ = new Glue42();
            glue42_.Initialize("AppManagerDemo");

            var swOptions = glue42_.StickyWindows?.GetStartupOptions() ?? new SwOptions();
            swOptions.WithType(SwWindowType.Flat);
            swOptions.WithTitle("AppManager Demo");
            glue42_.StickyWindows?.RegisterWindow(this, swOptions);

            var appManager = glue42_.AppManager;
            if (appManager != null)
            {
                appManager.ApplicationAdded += OnApplicationAdded;
                appManager.ApplicationUpdated += OnApplicationUpdated;
                appManager.ApplicationRemoved += OnApplicationRemoved;
                appManager.ApplicationInstanceStarted += OnApplicationInstanceStarted;
                appManager.ApplicationInstanceStopped += OnApplicationInstanceStopped;
                appManager.LayoutAdded += OnLayoutAdded;
                appManager.LayoutRemoved += OnLayoutRemoved;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            var appManager = glue42_.AppManager;
            if (appManager != null)
            {
                appManager.ApplicationAdded -= OnApplicationAdded;
                appManager.ApplicationRemoved -= OnApplicationRemoved;
                appManager.ApplicationInstanceStarted -= OnApplicationInstanceStarted;
                appManager.ApplicationInstanceStopped -= OnApplicationInstanceStopped;
            }
            base.OnClosed(e);
        }

        private void OnApplicationAdded(object sender, AppManagerApplicationEventArgs e)
        {
            ExecuteAction(() => { OnApplicationAdded(e.Application); });
        }

        private void OnApplicationUpdated(object sender, AppManagerApplicationEventArgs e)
        {
            ExecuteAction(() => { OnApplicationUpdated(e.Application); });
        }

        private void OnApplicationRemoved(object sender, AppManagerApplicationEventArgs e)
        {
            ExecuteAction(() => { OnApplicationRemoved(e.Application); });
        }

        private void OnApplicationInstanceStarted(object sender, AppManagerApplicationInstanceEventArgs e)
        {
            ExecuteAction(() => { OnApplicationInstanceStarted(e.Instance); });
        }

        private void OnApplicationInstanceStopped(object sender, AppManagerApplicationInstanceEventArgs e)
        {
            ExecuteAction(() => { OnApplicationInstanceStopped(e.Instance); });
        }

        private void OnLayoutAdded(object sender, AppManagerLayoutEventArgs e)
        {
            ExecuteAction(() => { OnLayoutAdded(e.Layout); });
        }

        private void OnLayoutRemoved(object sender, AppManagerLayoutEventArgs e)
        {
            ExecuteAction(() => { OnLayoutRemoved(e.Layout); });
        }

        private void OnApplicationAdded(IAppManagerApplication application)
        {
            applicationList.Items.Add(application);
        }

        private void OnApplicationUpdated(IAppManagerApplication application)
        {
            applicationList.Items.Refresh();
        }

        private void OnApplicationRemoved(IAppManagerApplication application)
        {
            applicationList.Items.Remove(application);
        }

        private void OnApplicationInstanceStarted(IAppManagerApplicationInstance instance)
        {
            applicationInstanceList.Items.Add(instance);
        }

        private void OnApplicationInstanceStopped(IAppManagerApplicationInstance instance)
        {
            applicationInstanceList.Items.Remove(instance);
        }

        private void OnLayoutAdded(IAppManagerLayout layout)
        {
            layoutList.Items.Add(layout);
        }

        private void OnLayoutRemoved(IAppManagerLayout layout)
        {
            layoutList.Items.Remove(layout);
        }

        private void ExecuteAction(Action action)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(action, null);
            }
            else
            {
                action();
            }
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApplication = applicationList.SelectedItem as IAppManagerApplication;
            if (selectedApplication != null)
            {
                var context = AppManagerContext.CreateNew();
                context.Set("startedFrom", Process.GetCurrentProcess().Id);
                var innerObject = new AppManagerContextValues();
                innerObject.Set("x", 600);
                innerObject.Set("y", 600);
                innerObject.Set("width", 500);
                innerObject.Set("height", 500);
                context.Set("bounds", innerObject);
                var task = selectedApplication.Start(context);
                task.ContinueWith(t =>
                {
                    if (t.Status != TaskStatus.RanToCompletion)
                    {

                    }
                    else
                    {
                        var instance = t.Result;
                    }
                });
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApplicationInstance = applicationInstanceList.SelectedItem as IAppManagerApplicationInstance;
            if (selectedApplicationInstance != null)
            {
                selectedApplicationInstance.Stop();
            }
        }

        private void restoreButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLayout = layoutList.SelectedItem as IAppManagerLayout;
            if (selectedLayout != null)
            {
                selectedLayout.Restore(null);
            }
        }
    }
}