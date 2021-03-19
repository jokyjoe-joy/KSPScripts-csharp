using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KSPScripts
{
    public partial class SerpentWindow : Window
    {
        public static Task serpentScriptTask = null;
        public SerpentWindow()
        {
            InitializeComponent();
            // Change console logging to this window's GUI on init.
            Console.SetOut(new App.GUIConsoleWriter(lbResult));
            Console.WriteLine("Initialized Serpent Controller");
        }
        private async void Launch(object sender, RoutedEventArgs e)
        {
            // Only start if it is not running.
            if (serpentScriptTask != null && serpentScriptTask.Status.Equals(TaskStatus.Running))
            {
                Console.WriteLine("Script can't be run while another script has already been started.");
                return;
            }
            // Ask user for confirmation.
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to launch the rocket?", "Launch Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Console.WriteLine("Starting Serpent...");
                int apoapsis, periapsis;
                // Try getting text from GUI
                if (!Int32.TryParse(orbitApoapsisAltBox.Text, out apoapsis))
                {
                    Console.WriteLine("Apoapsis value is incorrect. Exiting.");
                    return;
                }
                if (!Int32.TryParse(orbitPeriapsisAltBox.Text, out periapsis))
                {
                    Console.WriteLine("Periapsis value is incorrect. Exiting.");
                    return;
                }
                Console.WriteLine($"Initializing Serpent with an apoapsis of {apoapsis} m and a periapsis of {periapsis} m.");
                IProgress<string> progress = new Progress<string>(b => Console.WriteLine(b));
                serpentScriptTask = Task.Run(()=>Serpent.Start(apoapsis, periapsis, true, lbResult, progress));
                await serpentScriptTask;
                Console.WriteLine("Exiting Serpent.");
            }
        }
        private async void Deorbit(object sender, RoutedEventArgs e)
        {
            if (serpentScriptTask == null) return;
            // Only start if it is not running.
            if (serpentScriptTask.Status.Equals(TaskStatus.Running))
            {
                Console.WriteLine("Script can't be run while another script has already been started.");
                return;
            }
            if (!serpentScriptTask.IsCompletedSuccessfully)
            {
                Console.WriteLine("Deorbit can be only run after running the launch sequence.");
                return;
            }
            // Ask user for confirmation.
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to deorbit the rocket?", "Deorbit Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Console.WriteLine("Starting Serpent...");
                serpentScriptTask = Task.Run(()=>Serpent.DeorbitalStageLoop());
                await serpentScriptTask;
                Console.WriteLine("Exiting Serpent.");
            }
        }
        private void StopScript(object sender, RoutedEventArgs e)
        {
            // TODO: This
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
