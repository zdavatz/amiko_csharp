using System.Threading;
using System.Windows;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /*
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();
        */
        protected override void OnStartup(StartupEventArgs e)
        {
            SplashScreen splash = new SplashScreen();
            splash.Show();
            MainWindow main = new MainWindow();
            Thread.Sleep(1000);
            splash.Close();
            /*
            AllocConsole();
            Console.WriteLine("And so it begins");
            */
            main.Show();
        }
    }
}
