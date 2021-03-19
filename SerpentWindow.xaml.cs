using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;

namespace KSPScripts
{
    public partial class SerpentWindow : Window
    {
        public SerpentWindow()
        {
            InitializeComponent();
            // Change console logging to this window's GUI on init.
            Console.SetOut(new App.GUIConsoleWriter(lbResult));
            Console.WriteLine("Initialized Serpent Controller");
        }
        public Thread StartTheThread(int orbitApoapsisAlt, int orbitPeriapsisAlt, ListBox consoleBoxGUI, bool consoleToGUI = true)
        {
            var t = new Thread(() => Serpent.Start(orbitApoapsisAlt, orbitPeriapsisAlt, consoleToGUI: consoleToGUI, consoleBoxGUI: consoleBoxGUI));
            t.Start();
            return t;
        }
        private void Launch(object sender, RoutedEventArgs e)
        {
            // Ask user for confirmation.
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to launch the rocket?", "Launch Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Console.WriteLine("Starting Serpent...");
                int apoapsis = Int32.Parse(orbitApoapsisAltBox.Text);
                int periapsis = Int32.Parse(orbitPeriapsisAltBox.Text);
                Console.WriteLine($"Initializing Serpent with an apoapsis of {apoapsis} m and a periapsis of {periapsis} m.");

                //var t = StartTheThread(apoapsis, periapsis, lbResult, true); // TODO: For some reason this crashes.
                Serpent.Start(apoapsis, periapsis, true, lbResult);

            }

        }
        private void CollectExperiments(object sender, RoutedEventArgs e)
        {
            try
            {
                Serpent.CollectExperimentResults();
            }
            catch (Exception err)
            {
                Console.WriteLine($"Couldn't perform action: {err.Message}");
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
