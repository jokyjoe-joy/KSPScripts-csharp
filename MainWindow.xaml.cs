using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;

namespace KSPScripts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ListBox consoleBox;
        public MainWindow()
        {
            InitializeComponent();
            Console.SetOut(new App.GUIConsoleWriter(lbResult));
            Console.WriteLine("Initialized Rocket Controller.");
        }
        private void StartScript(object sender, RoutedEventArgs e)
        {
            // Button name is the window's name, e.g.: SerpentWindow.
            string buttonName = (sender as Button).Name.ToString();

            // Only log out the name of the script with first char capitalized.
            string controllerName = buttonName.Replace("Window","").FirstCharToUpper();
            
            Console.WriteLine($"Starting {controllerName} controller.");

            // Get type from KSPScripts namespace.
            Type t = Type.GetType("KSPScripts." + buttonName); 
            dynamic window = Activator.CreateInstance(t);

            this.Hide();
            window.ShowDialog();
            this.Show();

            // Change back console to this window's console
            Console.SetOut(new App.GUIConsoleWriter(lbResult));
            Console.WriteLine($"Exiting {controllerName} controller.");
        }
    }
}
