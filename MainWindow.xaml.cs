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
using System.IO;

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
            consoleBox = lbResult;
            Console.SetOut(new GUIConsoleWriter());
        }
        private void StartSerpent(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("=== RUNNING SERPENT ===");
            Serpent.Start(orbitApoapsisAlt: 80000, orbitPeriapsisAlt: 80000, consoleToGUI: true);
            Console.WriteLine("=== EXITING SERPENT ===");
        }
        public class GUIConsoleWriter : TextWriter
        {
            public override void WriteLine(string value)
            {
                consoleBox.Items.Add(value);
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }
    }
}
