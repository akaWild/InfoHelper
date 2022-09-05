using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace InfoHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "InfoHelper";

            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show(@"Another instance of the application is already running!", @"Attention!", MessageBoxButton.OK);

                Current.Shutdown();
            }

            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            base.OnStartup(e);
        }
    }
}
