using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            AllocConsole();
            Console.WriteLine("And so it begins");
        }
    }
}
