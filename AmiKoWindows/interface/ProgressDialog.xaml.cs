using System.Windows;
using MahApps.Metro.Controls;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : MetroWindow
    {
        private UpdateDb _updateDb = null;

        public ProgressDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _updateDb.DownloadingCompleted = true;
            this.Close();
        }

        public async void UpdateDbAsync()
        {
            // Call updater
            _updateDb = new UpdateDb();
            this.DataContext = _updateDb;
            await _updateDb.doIt();
        }
    }
}
