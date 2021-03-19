using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text;

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
			wnd.Show();
		}
        public class GUIConsoleWriter : TextWriter
        {
            public ListBox consoleBox;
            public GUIConsoleWriter(ListBox consoleBox)
            {
                this.consoleBox = consoleBox;
            }
            public override void WriteLine(string value)
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                // Add a dot to the end of the line if it doesn't end on one of the specified symbols.
                var isEndCharASymbol = value.Last().ToString().IndexOfAny("!.?:".ToCharArray()) != -1;
                if (!isEndCharASymbol) value += ".";

                consoleBox.Items.Add($"{time}: {value}");
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }
    }
}
