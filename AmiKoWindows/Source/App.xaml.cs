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
        App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SplashScreen splash = new SplashScreen();
            //splash.Show();

#if AMIKO
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de-CH");
#elif COMED
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("fr-CH");
#endif

            MainWindow main = new MainWindow();
            Thread.Sleep(1000);
            //splash.Close();
            /*
            AllocConsole();
            Console.WriteLine("And so it begins");
            */

            main.Show();
        }
    }

    public static class Log
    {
        public static void WriteLine(string text)
        {
            Log.WriteLine(text, new object[]{});
        }

        public static void WriteLine(string text, params object[] args)
        {
#if (DEBUG && TRACE)
            System.Diagnostics.Trace.WriteLine(
                System.String.Format(text, args));
#elif DEBUG
            System.Console.WriteLine(text, args);
#else
            // do nothing for release build :)
#endif
        }
    }
}
