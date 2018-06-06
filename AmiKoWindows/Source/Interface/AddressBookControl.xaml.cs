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

        bool _isItemClick = false;
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
            this.Initialized += delegate
            {
                // This block is called after InitializeComponent
                this.DataContext = this;
                this.SearchResult.DataContext = _patientDb;
            };

            // Initialize Patient (In-App Address Book) DB
            _patientDb = new PatientDb();
            _patientDb.Init();

            InitializeComponent();

            // TODO
            // This method does not clear focus :'(
            Keyboard.ClearFocus();
            // Workaround
            this.SearchPatientBox.Focus();
            Keyboard.Focus(this.SearchPatientBox);
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(e.ToString());
            this.CurrentEntry = new Contact();
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _parent = this.Parent as MahApps.Metro.Controls.Flyout;
            _parent.AreAnimationsEnabled = false;

            var isVisible = e.NewValue as bool?;
            if (isVisible != null && isVisible.Value)
            {
                _mainWindow = Window.GetWindow(_parent.Parent) as AmiKoWindows.MainWindow;
                _patientDb.UpdateSearchResults();
            }
            else
                _mainWindow = null;
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
                Dictionary<string, string> values = GetContactValues();
                Contact contact;
                contact = this.CurrentEntry;
                if (contact == null || contact.Uid == null || contact.Uid.Equals(string.Empty))
                    contact = _patientDb.InitContact(values);

                foreach (var v in values)
                    contact[v.Key] = v.Value;

                _patientDb.SaveContact(contact);
                this.CurrentEntry = contact;

                _patientDb.UpdateSearchResults();
            }

            if (this.CurrentEntry.Uid == null && !result)
                return;

            // Re:set selected list item, `UpdateLayout` is needed.
            ListBoxItem li = null;
            this.SearchResult.UpdateLayout();
            foreach (Item item in this.SearchResult.Items)
            {
                if (item != null && item.Id == this.CurrentEntry.Id)
                {
                    li = (ListBoxItem)this.SearchResult.ItemContainerGenerator.ContainerFromItem(item);
                    if (li != null)
                    {
                        li.IsSelected = true;
                        break;
                    }
                }
            }
        }
        #endregion

        #region Actions on Right Pane
        // Preview action
        private void ContactItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Log.WriteLine(sender.GetType().Name);
            _isItemClick = true;
            EnableMinusButton(true);
        }

        private async void ContactItem_SelectionChanged(object sender, EventArgs e)
        {
            //Log.WriteLine(sender.GetType().Name);
            // NOTE:
            // This private variable prevents event triggered by the manual assignment
            // to `IsSelected` in `SaveButton_Click`
            if (!_isItemClick)
                return;
            _isItemClick = false;

            var item = this.SearchResult.SelectedItem as Item;
            if (item != null && item.Id != null)
            {
                ResetMessage();

                // clear only error styles on field
                foreach (string field in contactFields)
                    FeedbackField(this.FindName(field) as TextBox, false);

                Contact contact = await _patientDb.GetContactById(item.Id.Value);
                if (contact != null)
                    this.CurrentEntry = contact;
            }
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            this.SearchResult.UnselectAll();
            this.CurrentEntry = new Contact();

            ResetFields();
            ResetMessage();

            EnableMinusButton(false);
        }

        private async void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            //Log.WriteLine(sender.GetType().Name);
            var item = this.SearchResult.SelectedItem as Item;
            if (item != null && item.Id != null)
            {
                ResetFields();
                this.CurrentEntry = new Contact();

                await _patientDb.DeleteContact(item.Id.Value);
                _patientDb.UpdateSearchResults();
            }

            EnableMinusButton(false);
        }

        private void SwitchBookButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
        }
        #endregion

        // returns dictionary contains key (propertyName) and value
        private Dictionary<string, string> GetContactValues()
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

        private void ResetMessage()
        {
            FeedbackMessage(false, false);
        }

        private void ResetFields()
        {
            foreach (string field in contactFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                if (element is TextBox)
                {
                    var box = element as TextBox;
                    if (box != null)
                        box.Text = "";

                    FeedbackField(box, false);
                }
                else if (element is StackPanel) // RadioButton
                {
                    var panel = element as StackPanel;
                    if (panel != null)
                    {
                        var buttons = panel.Children.OfType<RadioButton>().ToList().Where(
                            r => r.GroupName != string.Empty && r.Tag.ToString().Equals("0"));
                        if (buttons != null)
                        {
                            // back to default selection
                            RadioButton btn = buttons.Single();
                            if (btn != null)
                                btn.IsChecked = true;
                        }
                    }
                }
            }
        }

        private bool ValidateField(FrameworkElement element)
        {
            bool hasError = false;

            if (element == null)
                return hasError;

            if (element is TextBox)
            {
                var box = element as TextBox;
                string columnName = Utilities.ConvertTitleCaseToSnakeCase(box.Name);
                hasError = !_patientDb.ValidateField(columnName, box.Text);
                FeedbackField(box, hasError);
            }
            else if (element is StackPanel) // Group for RadioButtons
            {
                var box = element as StackPanel;
                if (box == null)
                    return false; // skip

                var buttons = box.Children.OfType<RadioButton>().ToList().Where(
                    r => r.GroupName != string.Empty && (bool)r.IsChecked);
                if (buttons != null)
                {
                    RadioButton btn = buttons.Single();
                    if (btn != null)
                    {
                        string columnName = Utilities.ConvertTitleCaseToSnakeCase(btn.GroupName);
                        Label lbl = btn.Content as Label;
                        if (lbl != null)
                            hasError = !_patientDb.ValidateField(columnName, lbl.Content.ToString());
                    }
                }
            }
            return !hasError;
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
            FeedbackMessage(true, hasError);
            return !hasError;
        }

        private void FeedbackField(TextBox box, bool hasError)
        {
            if (box == null)
                return;

            if (hasError)
            {
                var converter = new BrushConverter();
                Brush errFieldColor = converter.ConvertFrom(Constants.ErrorFieldColor) as Brush;
                Brush errBrushColor = converter.ConvertFrom(Constants.ErrorBrushColor) as Brush;

                box.Background = errFieldColor;
                box.BorderBrush = errBrushColor;
            }
            else
            {
                box.Background = Brushes.White;
                box.BorderBrush = Brushes.LightGray;
            }
        }

        private void FeedbackMessage(bool needsDisplay, bool hasError)
        {
            TextBlock errMsg, okMsg = null;
            errMsg = this.FindName("SaveContactFailureMessage") as TextBlock;
            okMsg = this.FindName("SaveContactSuccessMessage") as TextBlock;

            if (!needsDisplay)
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
            { // needsDisplay && !hasError
                errMsg.Visibility = Visibility.Hidden;
                okMsg.Visibility = Visibility.Visible;
            }
        }

        private void EnableMinusButton(bool isEnabled)
        {
            this.MinusButton.IsEnabled = isEnabled;
            var image = this.MinusButton.Content as FontAwesome.WPF.ImageAwesome;
            if (image != null)
                if (isEnabled)
                    image.Foreground = Brushes.Black;
                else
                    image.Foreground = Brushes.LightGray;
        }
    }
}
