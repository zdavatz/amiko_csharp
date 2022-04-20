using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            // Console.WriteLine("Hello");
            App application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}
