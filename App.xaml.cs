using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KSPScripts
{
    public partial class App : Application
    {
		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			MainWindow wnd = new MainWindow();
			wnd.Title = "KSP Controller";
			wnd.Show();
		}
    }
}
