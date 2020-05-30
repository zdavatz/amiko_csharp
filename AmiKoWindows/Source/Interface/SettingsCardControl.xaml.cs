/*
Copyright (c) ywesee GmbH

This file is part of AmiKo for Windows.

AmiKo for Windows is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;


namespace AmiKoWindows
{
    using ControlExtensions;

    /// <summary>
    /// View controls for doctor's profile (account) and signature.
    /// </summary>
    public partial class SettingsCardControl : UserControl, INotifyPropertyChanged
    {
        #region Private Fields
        MainWindow _mainWindow;
        MahApps.Metro.Controls.Flyout _parent;
        #endregion

        #region Public Fields
        private String _LoginButtonText = "";
        public String LoginButtonText
        {
            get { return _LoginButtonText; }
            set { _LoginButtonText = value; OnPropertyChanged("LoginButtonText"); }
        }
        private Boolean _IsLoggedIn = false;
        public Boolean IsLoggedIn
        {
            get { return _IsLoggedIn; }
            set { _IsLoggedIn = value; OnPropertyChanged("IsLoggedIn"); OnPropertyChanged("SyncButtonEnabled"); }
        }
        private string _SyncStatus = "";
        public string SyncStatus
        {
            get { return _SyncStatus; }
            set { _SyncStatus = value; OnPropertyChanged("SyncStatus"); }
        }
        private bool _IsSyncing = false;
        public bool IsSyncing
        {
            get { return _IsSyncing; }
            set { _IsSyncing = value; OnPropertyChanged("IsSyncing"); OnPropertyChanged("SyncButtonEnabled"); }
        }
        public bool SyncButtonEnabled
        {
            get { return !_IsSyncing && IsLoggedIn; }
        }
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public SettingsCardControl()
        {
            this.Initialized += delegate
            {
                // This block is called after InitializeComponent
                this.DataContext = this;
            };

            InitializeComponent();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(e.ToString());
            ReloadGoogleLoginStateAsync();
            GoogleSyncManager.Instance.PropertyChanged += (_sender, prop) =>
            {
                if (prop.PropertyName.Equals("IsSyncing"))
                {
                    IsSyncing = GoogleSyncManager.Instance.IsSyncing;
                }
            };
            GoogleSyncManager.Instance.Progress.ProgressChanged += (progressSender, p) =>
            {
                if (p is SyncProgressText)
                {
                    SyncStatus = ((SyncProgressText)p).Value;
                }
            };
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _parent = this.Parent as MahApps.Metro.Controls.Flyout;
            _parent.AreAnimationsEnabled = false;

            var isVisible = e.NewValue as bool?;
            if (isVisible != null && isVisible.Value)
                _mainWindow = Window.GetWindow(_parent.Parent) as AmiKoWindows.MainWindow;
            else
                _mainWindow = null;
            if (!IsSyncing)
            {
                SyncStatus = "Last Synced: " + GoogleSyncManager.LastSynced();
            }
        }

        private async Task ReloadGoogleLoginStateAsync()
        {
            var loggedIn = await GoogleSyncManager.Instance.IsGoogleLoggedInAsync();
            this.IsLoggedIn = loggedIn;
            if (loggedIn)
            {
                this.LoginButtonText = Properties.Resources.logoutFromGoogle;
            } else
            {
                this.LoginButtonText = Properties.Resources.loginWithGoogle;
            }
        }

        #region Actions

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            if (_parent != null)
                _parent.IsOpen = false;
        }

        private void LoginGoogleButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (IsLoggedIn)
                {
                    await GoogleSyncManager.Instance.Logout();
                }
                else
                {
                    await GoogleSyncManager.Instance.Login();
                }
                await ReloadGoogleLoginStateAsync();
                GoogleSyncManager.Instance.Synchronise();
            });
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoggedIn || IsSyncing) return;
            Task.Run(() =>
                GoogleSyncManager.Instance.Synchronise()
            );
        }

        #endregion
    }
}
