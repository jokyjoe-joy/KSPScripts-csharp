using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KSPScripts
{
    public partial class HorizonWindow : Window
    {
        public static Task scriptTask = null;
        public static IProgress<string> progress;
        public HorizonWindow()
        {
            InitializeComponent();
            // Change console logging to this window's GUI on init.
            Console.SetOut(new App.GUIConsoleWriter(lbResult));
            progress = new Progress<string>(b => Console.WriteLine(b));
            Console.WriteLine("Initialized Horizon Controller");
        }
        private async void Land(object sender, RoutedEventArgs e)
        {
            // Only start if it is not running.
            if (scriptTask != null && scriptTask.Status.Equals(TaskStatus.Running))
            {
                Console.WriteLine("Script can't be run while another script has already been started.");
                return;
            }
            // Ask user for confirmation.
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to launch the landing sequence?", "Launch Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Console.WriteLine("Starting Horizon...");
                scriptTask = Task.Run(()=>Horizon.Start(true, lbResult, progress));
                await scriptTask;
                Console.WriteLine("Exiting Horizon.");
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
                Serpent.CollectExperimentResults(progress);
            }
            catch (Exception err)
            {
                Console.WriteLine($"Couldn't perform action: {err.Message}");
            }
        }
    }
}
