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
using System.Windows.Input;
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
        // visible fields
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

        #region Other Fields
        private Contact _CurrentEntry;
        public Contact CurrentEntry {
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

        public AddressBookControl()
        {
            InitializeComponent();

            // Initialize Patient (In-App Address Book) DB
            _patientDb = new PatientDb();
            _patientDb.Init();

            this.DataContext = this;
            this.SearchResult.DataContext = _patientDb;

            this.CurrentEntry = new Contact();

            // TODO
            // this does not clear focus :'(
            Keyboard.ClearFocus();
            // Workaround
            this.SearchPatientBox.Focus();
            Keyboard.Focus(this.SearchPatientBox);
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _patientDb.UpdateSearchResults();
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _parent = this.Parent as MahApps.Metro.Controls.Flyout;
            _parent.AreAnimationsEnabled = false;
            _mainWindow = Window.GetWindow(_parent.Parent) as AmiKoWindows.MainWindow;
        }

        #region Actions on Left Pane
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

        private void Country_LostFocus(object sender, RoutedEventArgs e)
        {
            // pass
        }

        private void Birthdate_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void WeightKg_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void HeightCm_LostFocus(object sender, RoutedEventArgs e)
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            if (_parent != null)
                _parent.IsOpen = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = ValidateFields();
            if (result)
            {
                Dictionary<string, string> values = getContactValues();
                Contact contact;
                contact = this.CurrentEntry;
                if (contact == null || contact.Uid == null || contact.Uid.Equals(string.Empty))
                    contact = _patientDb.InitContact(values);

                foreach (var v in values)
                    contact[v.Key] = v.Value;

                Log.WriteLine("Uid: {0}", contact.Uid);
                _patientDb.SaveContact(contact);
                _patientDb.UpdateSearchResults();
            }
        }
        #endregion

        #region Actions on Right Pane
        private void ContactItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // preview action
            //Log.WriteLine(sender.GetType().Name);
            this.MinusButton.IsEnabled = true;
            var image = this.MinusButton.Content as FontAwesome.WPF.ImageAwesome;
            if (image != null)
                image.Foreground = Brushes.Black;
        }

        private async void ContactItem_SelectionChanged(object sender, EventArgs e)
        {
            //Log.WriteLine(sender.GetType().Name);
            var item = this.SearchResult.SelectedItem as Item;
            if (item != null && item.Id != null)
            {
                Log.WriteLine("Item.Id {0}", item.Id.Value);
                Contact contact = await _patientDb.LoadContactById(item.Id.Value);
                if (contact != null)
                    this.CurrentEntry = contact;
                    Feedback(false, false);
            }

            this.MinusButton.IsEnabled = true;
            var image = this.MinusButton.Content as FontAwesome.WPF.ImageAwesome;
            if (image != null)
                image.Foreground = Brushes.Black;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }

        private async void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            //Log.WriteLine(sender.GetType().Name);
            var item = this.SearchResult.SelectedItem as Item;
            if (item != null && item.Id != null)
            {
                await _patientDb.DeleteContact(item.Id.Value);
                _patientDb.UpdateSearchResults();
            }
        }

        private void SwitchBookButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }
        #endregion

        // returns dictionary contains key (propertyName) and value
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
                        values.Add(box.Name, box.Text);
                }
                else if (element is StackPanel) // RadioButton
                {
                    var panel = element as StackPanel;
                    List<RadioButton> buttons = panel.Children.OfType<RadioButton>().ToList();
                    RadioButton btn = buttons.Where(
                        r => r.GroupName != string.Empty && (bool)r.IsChecked).Single();
                    if (btn != null)
                        values.Add(btn.GroupName, btn.Tag.ToString());
                }
            }
            return values;
        }

        private bool ValidateField(FrameworkElement element)
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

        private bool ValidateFields()
        {
            bool hasError = false;

            foreach (string field in contactFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                var result = ValidateField(element);
                //Log.WriteLine("field: {0} validateField: {0}", field, result);
                if (!hasError)
                    hasError = !result;
            }
            Log.WriteLine("hasError: {0}", hasError);
            Feedback(true, hasError);
            return !hasError;
        }

        private void Feedback(bool display, bool hasError)
        {
            TextBlock errMsg, okMsg = null;
            errMsg = this.FindName("SaveContactFailureMessage") as TextBlock;
            okMsg = this.FindName("SaveContactSuccessMessage") as TextBlock;

            if (!display)
            {
                errMsg.Visibility = Visibility.Hidden;
                okMsg.Visibility = Visibility.Hidden;
            }
            else if (hasError)
            {
                errMsg.Visibility = Visibility.Visible;
                okMsg.Visibility = Visibility.Hidden;
            }
            else
            { // display && !hasError
                errMsg.Visibility = Visibility.Hidden;
                okMsg.Visibility = Visibility.Visible;
            }
        }
    }
}
