using System.Diagnostics;
using System.Windows;
using Microsoft.VisualBasic;

namespace MainAppBootstrapper
{
    /// <summary>
    ///     Interaction logic for Launcher.xaml
    ///     Notice that the bootstrapper does not have reference to io.connect.net, as its only purpose is to launch the main app.
    /// </summary>
    public partial class Launcher : Window
    {
        public Launcher()
        {
            InitializeComponent();
        }

        private void BtnLaunchApp_OnClick(object sender, RoutedEventArgs e)
        {
            string appPath = @"..\..\..\BootstrappedMainApp\bin\Debug\BootstrappedMainApp.exe";

            // simulate custom bootstrapping and pass something to the exe - this can be achieved by any interprocess mechanism
            // here we're passing command-line arguments
            Process.Start(appPath, $"\"credentials:{Interaction.InputBox("Enter credentials")}\"");

            // we're done, so quit
            Application.Current.Shutdown();
        }
    }
}