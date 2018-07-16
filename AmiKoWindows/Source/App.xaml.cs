using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MUTEX_NAME = "793237F8-5898-453C-8C17-839E3D9203BD";
        private const string HANDLE_KEY = "AB5E39E5-56B0-4863-99FB-61DE3C05D478";

        public static readonly int AMIKO_MSG = 0x4A;

        private EventWaitHandle _handle;
        private Mutex _mutex = null;

        // Struct object for WndProc messaging
        // `msg` field contains file path.
        public struct AMIKO_DAT
        {
            public IntPtr ptr;
            public int len;

            [MarshalAs(UnmanagedType.LPStr)]
            public string msg;
        }

        [DllImport("user32.dll")]
        public static extern Int32 FindWindow(String lpClassName, String lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(
            IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32.dll")]
        public static extern int SendMessage(
            IntPtr hWnd, int Msg, IntPtr wParam, ref AMIKO_DAT lParam);

        protected override void OnStartup(StartupEventArgs e)
        {
            string path = null;
            if (e.Args.Length == 1)
            {
                FileInfo info = new FileInfo(e.Args[0]);
                if (info.Exists)
                    path = info.FullName;
            }
            Log.WriteLine("args[0] (path): {0}", path);

            bool isOwned;
            this._mutex = new Mutex(true, MUTEX_NAME, out isOwned);
            this._handle = new EventWaitHandle(false, EventResetMode.AutoReset, HANDLE_KEY);

            GC.KeepAlive(_mutex);

            if (isOwned)
            {
                base.OnStartup(e);

                // temporary inbox/outbox
                this.Properties["InboxPath"] = Utilities.NewBoxPath("inbox");
                Log.WriteLine("InboxPath: {0}", this.Properties["InboxPath"]);

                this.Properties["OutboxPath"] = Utilities.NewBoxPath("outbox");
                Log.WriteLine("OutboxPath: {0}", this.Properties["OutboxPath"]);

//#if DEBUG
//#else
                SplashScreen splash = new SplashScreen();
                splash.Show();
//#endif

#if AMIKO
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de-CH");
#elif COMED
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("fr-CH");
#endif

                var main = new MainWindow();
                this.MainWindow = main;

//#if DEBUG
//#else
                Thread.Sleep(1000);
                splash.Close();
//#endif

                main.Show();

                if (path != null)
                    main.OpenFile(path);

                // prepare handle thread
                var thread = new Thread(() =>
                    {
                        while (_handle.WaitOne())
                        {
                            if (Current == null)
                                break;
                            // get back minimized/hidden window
                            Current.Dispatcher.BeginInvoke(
                                (Action)(() => ((MainWindow)Current.MainWindow).BringToFront()));
                        }
                    }
                );
                thread.IsBackground = true;
                thread.Start();
                return;
            }

            // notify (use previous app)
            _handle.Set();

            // Send the pointer of an object as message (See WndProc in MainWindow.xml) which contains
            // file path given by user.
            if (path != null)
            {
                byte[] arr = System.Text.Encoding.Default.GetBytes(path);
                int len = arr.Length;

                AMIKO_DAT dat;
                dat.ptr = (IntPtr)100;
                dat.len = len + 1;
                dat.msg = path;

                var title = Utilities.AppName();
                Log.WriteLine("title: {0}", title);
                var hwnd = FindWindow(null, title);
                SendMessage((IntPtr)hwnd, AMIKO_MSG, IntPtr.Zero, ref dat);
            }

            Application.Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Utilities.CleanupBoxes();

            base.OnExit(e);
        }

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            Utilities.CleanupBoxes();
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
