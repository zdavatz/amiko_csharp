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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        static string[] contactFields = {
            "GivenName", "FamilyName", "Address", "City", "Zip", "Birthdate",
            "Gender",
            "Country", "WeightKg", "HeightCm", "Phone", "Email",
        };

        #region Private Fields
        PatientDb _patientDb; // has contacts as patient

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

            // Initialize Patient (In-App Address Book) DB
            _patientDb = new PatientDb();
            _patientDb.Init();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            this.SearchResult.DataContext = _patientDb;
            _patientDb.UpdateSearchResults();
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _parent = this.Parent as MahApps.Metro.Controls.Flyout;
            _parent.AreAnimationsEnabled = false;
            _mainWindow = Window.GetWindow(_parent.Parent) as AmiKoWindows.MainWindow;
        }

        private void GivenName_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void FamilyName_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void Address_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void City_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void Zip_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void Country_LostFocus(object sender, RoutedEventArgs e)
        {
            // pass
        }

        private void Birthdate_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void WeightKg_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void HeightCm_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void Phone_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private void Email_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            validateField(box);
        }

        private Dictionary<string, string> getContactValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string field in contactFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                if (element is TextBox)
                {
                    var box = element as TextBox;
                    if (box != null)
                    {
                        string columnName = Utilities.ConvertTitleCaseToSnakeCase(box.Name);
                        values.Add(columnName, box.Text);
                    }
                }
                else if (element is StackPanel) // RadioButton
                {
                    var panel = element as StackPanel;
                    List<RadioButton> buttons = panel.Children.OfType<RadioButton>().ToList();
                    RadioButton btn = buttons.Where(
                        r => r.GroupName != string.Empty && (bool)r.IsChecked).Single();
                    if (btn != null)
                    {
                        string columnName = Utilities.ConvertTitleCaseToSnakeCase(btn.GroupName);
                        values.Add(columnName, btn.Tag.ToString());
                    }
                }
            }
            return values;
        }

        private bool validateField(FrameworkElement element)
        {
            if (element == null)
                return false;

            if (element is TextBox)
            {
                var converter = new BrushConverter();
                Brush errFieldColor = converter.ConvertFrom(Constants.ErrorFieldColor) as Brush;
                Brush errBrushColor = converter.ConvertFrom(Constants.ErrorBrushColor) as Brush;

                var box = element as TextBox;
                string columnName = Utilities.ConvertTitleCaseToSnakeCase(box.Name);
                if (!_patientDb.ValidateField(columnName, box.Text))
                {
                    box.Background = errFieldColor;
                    box.BorderBrush = errBrushColor;
                    return false;
                }
                else
                {
                    box.Background = Brushes.White;
                    box.BorderBrush = Brushes.LightGray;
                    return true;
                }
            }
            else if (element is StackPanel) // Group for RadioButtons
            {
                var box = element as StackPanel;
                List<RadioButton> buttons = box.Children.OfType<RadioButton>().ToList();
                RadioButton btn = buttons.Where(
                    r => r.GroupName != string.Empty && (bool)r.IsChecked).Single();
                if (btn != null)
                {
                    string columnName = Utilities.ConvertTitleCaseToSnakeCase(btn.GroupName);
                    Label lbl = btn.Content as Label;
                    return _patientDb.ValidateField(columnName, lbl.Content.ToString());
                }
                return false;
            }
            return false;
        }

        private bool validateFields()
        {
            bool hasError = false;

            foreach (string field in contactFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                var result = validateField(element);
                //Log.WriteLine("field: {0} validateField: {0}", field, result);
                if (!hasError)
                    hasError = !result;
            }

            Log.WriteLine("hasError: {0}", hasError);
            TextBlock errMsg, okMsg = null;
            errMsg = this.FindName("SaveContactFailureMessage") as TextBlock;
            okMsg = this.FindName("SaveContactSuccessMessage") as TextBlock;
            if (hasError)
            {
                errMsg.Visibility = Visibility.Visible;
                okMsg.Visibility = Visibility.Hidden;
            }
            else
            {
                errMsg.Visibility = Visibility.Hidden;
                okMsg.Visibility = Visibility.Visible;
            }
            return !hasError;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            if (_parent != null)
                _parent.IsOpen = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = validateFields();
            if (result)
            {
                Dictionary<string, string> values = getContactValues();
                Contact contact = _patientDb.InitContact(values);
                _patientDb.SaveContact(contact);
                _patientDb.UpdateSearchResults();
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }

        private void SwitchBookButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }
    }
}
