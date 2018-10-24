using DOT.AGM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tick42;

namespace WPFApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Glue42 Glue;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Glue = new Glue42();
            Glue.Initialize("MyDemo");
        }
    }
}
