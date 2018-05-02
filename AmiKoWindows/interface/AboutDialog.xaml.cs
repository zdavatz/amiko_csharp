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

using System.ComponentModel;
using System.Windows;
using MahApps.Metro.Controls;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : MetroWindow, INotifyPropertyChanged
    {
        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dependency Properties
        // Source object used for data binding, this is a property
        private string _appTitle;
        public string AppTitle
        {
            get { return _appTitle; }
            set
            {
                if (value != _appTitle)
                {
                    _appTitle = value;
                    OnPropertyChanged("AppTitle");
                }
            }
        }

        // Source object used for data binding, this is a property
        private string _appVersion;
        public string AppVersion
        {
            get { return _appVersion; }
            set
            {
                if (value != _appVersion)
                {
                    _appVersion = value;
                    OnPropertyChanged("AppVersion");
                }
            }
        }
        #endregion

        public AboutDialog()
        {
            InitializeComponent();
            MyAboutDialog.DataContext = this;
            AppTitle = Utilities.AppName();
            AppVersion = Utilities.AppVersion();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
