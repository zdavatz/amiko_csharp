using System.Windows;
using MahApps.Metro.Controls;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : MetroWindow
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public async void UpdateDbAsync()
        {
            // Call updater
            UpdateDb updateDb = new UpdateDb();
            this.DataContext = updateDb;
            await updateDb.doIt();
        }
    }
}
