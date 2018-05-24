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
using System.Diagnostics;
using MahApps.Metro.Controls;

namespace AmiKoWindows
{
    /// <summary>
    /// NOTE:
    ///   This control is shown currently as Flyout, but it may be good to change
    ///   Modal Window or Dialog, if user is confused.
    ///
    ///   See: https://github.com/zdavatz/amiko_csharp/issues/30#issuecomment-391544843
    /// </summary>
    public partial class AddressBookControl : UserControl, INotifyPropertyChanged
    {
        #region Private Fields
        MainWindow _mainWindow;
        MahApps.Metro.Controls.Flyout _parent;
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public AddressBookControl()
        {
            InitializeComponent();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            //Trace.WriteLine(String.Format("[Control_Loaded] sender: {0}", sender));
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _parent = this.Parent as MahApps.Metro.Controls.Flyout;
            _parent.AreAnimationsEnabled = false;
            _mainWindow = Window.GetWindow(_parent.Parent) as AmiKoWindows.MainWindow;
            //Trace.WriteLine(String.Format("[Control_IsVisibleChanged] _mainWindow: {0}", _mainWindow));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //Trace.WriteLine("[CancelButton_Click]");
            if (_parent != null) {
                _parent.IsOpen = false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //Trace.WriteLine("[SaveButton_Click]");
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            //Trace.WriteLine("[OpenButton_Click]");
        }
    }
}
