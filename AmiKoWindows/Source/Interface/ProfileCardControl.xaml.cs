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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;


namespace AmiKoWindows
{
    using ControlExtensions;

    /// <summary>
    /// View controls for doctor's (operator) profile and signature.
    /// </summary>
    public partial class ProfileCardControl : UserControl, INotifyPropertyChanged
    {
        // visible fields
        static string[] profileFields = {
            "Title", "GivenName", "FamilyName", "Address", "City", "Zip", "Phone", "Email",
            "Picture",
        };

        #region Private Fields
        MainWindow _mainWindow;
        MahApps.Metro.Controls.Flyout _parent;
        #endregion

        #region Public Fields
        private Operator _CurrentEntry;
        public Operator CurrentEntry {
            get { return _CurrentEntry; }
            set {
                _CurrentEntry = value;
                OnPropertyChanged("CurrentEntry");
            }
        }
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public ProfileCardControl()
        {
            this.Initialized += delegate
            {
                // This block is called after InitializeComponent
                this.DataContext = this;
            };

            if (Properties.Settings.Default.Operator == null)
                Properties.Settings.Default.Operator = new Operator();
            else
                Properties.Settings.Default.Operator.Reload();

            InitializeComponent();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(e.ToString());

            this.CurrentEntry = Properties.Settings.Default.Operator;
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
        }

        #region Actions
        private void Title_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void GivenName_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void FamilyName_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Address_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void City_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Zip_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Phone_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Email_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            var valid = ValidateFields();

            if (valid)
            {
                Properties.Settings.Default.Operator = this.CurrentEntry;
                Properties.Settings.Default.Operator.Save();
                Properties.Settings.Default.Save();
            }

            if (_parent != null)
                _parent.IsOpen = !valid;
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }

        private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }
        #endregion

        // Returns dictionary contains key (propertyName) and value
        private Dictionary<string, string> GetProfileValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string field in profileFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                if (element is TextBox)
                {
                    var box = element as TextBox;
                    if (box != null)
                        values.Add(box.Name, box.Text);
                }
            }
            return values;
        }

        // Returns the input is valid or not
        private bool ValidateField(FrameworkElement element)
        {
            bool hasError = false;

            if (element == null)
                return hasError;

            if (element is TextBox)
            {
                var box = element as TextBox;
                // Check text using Operator's validation method
                hasError = !Operator.ValidateProperty(box.Name, box.Text);
                this.FeedbackField(box, hasError);
            }
            else
            {
                // TODO image
                hasError = false;
            }
            return !hasError;
        }

        private bool ValidateFields()
        {
            bool hasError = false;
            foreach (string field in profileFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                var result = ValidateField(element);
                //Log.WriteLine("field: {0} validateField: {0}", field, result);
                if (!hasError)
                    hasError = !result;
            }
            Log.WriteLine("hasError: {0}", hasError);

            ShowMessage(hasError);
            return !hasError;
        }

        private void ShowMessage(bool hasError)
        {
            this.FeedbackMessage(this.SaveProfileFailureMessage, hasError);
        }
    }
}
