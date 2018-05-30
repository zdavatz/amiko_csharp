using System.Diagnostics;
using System.Threading;
using System.Windows;
using System;

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
            Log.WriteLine("And so it begins");
            */

            main.Show();
        }
    }

    public static class Log
    {
        public static void WriteLine(string text)
        {
#if DEBUG
            StackTrace stackTrace = new StackTrace();
            Print(FormatText(stackTrace, text, new object[]{}));
#else
            // do nothing for release build :)
#endif
        }

        public static void WriteLine(string text, params object[] args)
        {
#if DEBUG
            StackTrace stackTrace = new StackTrace();
            Print(FormatText(stackTrace, text, args));
#else
            // do nothing for release build :)
#endif
        }

        private static void Print(string text)
        {
#if (DEBUG && TRACE)
            Trace.WriteLine(text);
#elif DEBUG
            Console.WriteLine(text);
#else
            // do nothing for release build :)
#endif
        }

        private static string FormatText(StackTrace stackTrace, string text, params object[] args)
        {
            StackFrame frame = stackTrace.GetFrame(1);
            string className = frame.GetMethod().ReflectedType.FullName;
            string methodName = frame.GetMethod().Name;
            string newText = String.Format("{0}/{1}: {2}", className, methodName, text);
            return String.Format(newText, args);
        }
    }
}
