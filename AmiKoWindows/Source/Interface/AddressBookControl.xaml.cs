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

        private long RawContactsCount {
            set {
                ContactsCount.Text = String.Format("({0})", value.ToString());
                OnPropertyChanged("ContactsCount");
            }
        }
        #endregion

        #region Public Fields
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
                this.ContactList.DataContext = _patientDb;
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
                _patientDb.UpdateContactList();
                this.RawContactsCount = _patientDb.Count;
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
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Birthdate_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            if (box != null && !box.Text.Equals(string.Empty))
                box.Text = FormatBirthDate(box.Text);

            ValidateField(box);
        }

        private void FemaleButton_Checked(object sender, RoutedEventArgs e)
        {
            //Log.WriteLine("Source: {0}", e.Source);
            this.CurrentEntry.IsFemale = true;
        }

        private void MaleButton_Checked(object sender, RoutedEventArgs e)
        {
            //Log.WriteLine("Source: {0}", e.Source);
            this.CurrentEntry.IsMale = true;
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

        private string FormatBirthDate(string text)
        {
            // Input Support e.g. 08.06.2018 -> 8.6.2018
            string result = text;
            result = PatientDb.BIRTHDATE_NONDEVIDER_RGX.Replace(result, ".");
            result = PatientDb.BIRTHDATE_ZEROPADDED_RGX.Replace(result, "$1");
            return result;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            if (_parent != null)
                _parent.IsOpen = false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = ValidateFields();
            Contact contact = null;
            if (result)
            {
                Dictionary<string, string> values = GetContactValues();
                contact = this.CurrentEntry;
                if (contact == null || contact.Uid == null || contact.Uid.Equals(string.Empty))
                    contact = _patientDb.InitContact(values);

                foreach (var v in values)
                {
                    string val = v.Value;
                    if (v.Key.Equals("Birthdate"))
                        val = FormatBirthDate(val);

                    contact[v.Key] = val;
                }

                if (contact.Uid != null && !contact.Uid.Equals(string.Empty) && contact.Uid.Equals(contact.GenerateUid()))
                    await _patientDb.UpdateContact(contact);
                else
                {
                    // NEW Entry or `Contact.Uid` has been changed
                    long? id = await _patientDb.InsertContact(contact);
                    if (id != null && id.Value > 0)
                    {
                        contact.Id = id.Value;
                        this.SearchPatientBox.Text = "";
                        // TODO
                        // copied/update issued prescriptions if uid has been changed
                    }
                    else
                        // TODO need another message?
                        ShowMessage(true);

                    await _patientDb.LoadAllContacts();
                    this.RawContactsCount = _patientDb.Count;
                }
            }

            if (contact == null || contact.Uid == null || !result)
                return;
            else
            {
                // See ContactList_ItemStatusChanged
                this.ContactList.ItemContainerGenerator.StatusChanged += ContactList_ItemStatusChanged;

                this.CurrentEntry = contact;
                this.ContactList.UpdateLayout();
                _patientDb.UpdateContactList();
            }
        }
        #endregion

        #region Actions on Right Pane
        private async void SearchPatientBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = sender as TextBox;
            string text = box.Text;

            await _patientDb.Search(text);
            this.RawContactsCount = _patientDb.Count;
            _patientDb.UpdateContactList();
        }

        private void SearchPatientBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            this.SearchPatientBox.Text = "";
        }

        private void ContactList_KeyDown(object sender, KeyEventArgs e)
        {
            // Enable item selection using arrow keys + `Return`
            if (object.ReferenceEquals(sender, this.ContactList))
            {
                if (!e.IsDown || e.Key != Key.Return)
                    return;

                e.Handled = true;
                ListBoxItem li = (ListBoxItem)this.ContactList.ItemContainerGenerator.ContainerFromItem(
                    this.ContactList.SelectedItem);

                this.ContactItem_MouseLeftButtonDown(li, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
                this.ContactItem_SelectionChanged(li, new RoutedEventArgs());
            }
        }

        // ItemContainerGenerator
        private void ContactList_ItemStatusChanged(object sender, EventArgs e)
        {
            Log.WriteLine("");

            // Re:set selected list item, `UpdateLayout` is needed.
            if (this.ContactList.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                this.ContactList.ItemContainerGenerator.StatusChanged -= ContactList_ItemStatusChanged;

                //foreach (Item item in this.ContactList.Items)
                for (var i = 0; i < this.ContactList.Items.Count; i++)
                {
                    var item = this.ContactList.Items[i] as Item;
                    if (item != null && item.Id == this.CurrentEntry.Id)
                    {
                        var li = (ListBoxItem)this.ContactList.ItemContainerGenerator.ContainerFromIndex(i);
                        if (li != null)
                        {
                            li.IsSelected = true;
                            li.Focus();
                            this.ContactList.SelectedIndex = i;
                            this.ContactList.SelectedItem = li;
                            this.ContactList.ScrollIntoView(item);
                            break;
                        }
                    }
                }
            }
        }

        // DoubleClick
        private async void ContactList_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (object.ReferenceEquals(sender, this.ContactList))
            {
                if (ContactList.SelectedItem != null)
                {
                    Log.WriteLine("{0}", ContactList.SelectedItem);

                    var item = this.ContactList.SelectedItem as Item;
                    if (item != null && item.Id != null)
                    {
                        long id = item.Id.Value;
                        Contact contact = await _patientDb.GetContactById(id);
                        if (contact != null)
                        {
                            this.CurrentEntry = contact;
                            if (this._mainWindow != null)
                                this._mainWindow.ActiveContact = contact;
                        }
                    }
                }
            }
            if (_parent != null)
                _parent.IsOpen = false;
        }

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

            var item = this.ContactList.SelectedItem as Item;
            if (item != null && item.Id != null)
            {
                ResetMessage();

                // clear only error styles on field
                foreach (string field in contactFields)
                    this.FeedbackField<TextBox>(this.FindName(field) as TextBox, false);

                Contact contact = await _patientDb.GetContactById(item.Id.Value);
                if (contact != null)
                    this.CurrentEntry = contact;
            }
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            this.ContactList.UnselectAll();
            this.CurrentEntry = new Contact();

            ResetFields();
            ResetMessage();

            this.GivenName.Focus();

            EnableMinusButton(false);
        }

        private async void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            //Log.WriteLine(sender.GetType().Name);
            MessageBoxResult result = MessageBox.Show(
                Properties.Resources.msgContactDeleteConfirmation, "", MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK)
            {
                var item = this.ContactList.SelectedItem as Item;
                if (item != null && item.Id != null)
                {
                    ResetFields();
                    this.CurrentEntry = new Contact();

                    await _patientDb.DeleteContact(item.Id.Value);
                    await _patientDb.LoadAllContacts();
                    this.RawContactsCount = _patientDb.Count;
                    _patientDb.UpdateContactList();
                }
                EnableMinusButton(false);
            }
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

                    this.FeedbackField<TextBox>(box, false);
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
                this.FeedbackField<TextBox>(box, hasError);
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

            ShowMessage(hasError);
            return !hasError;
        }

        private void ShowMessage(bool hasError)
        {
            if (hasError)
            {
                this.FeedbackMessage(this.SaveContactFailureMessage, true);
                this.FeedbackMessage(this.SaveContactSuccessMessage, false);
            }
            else
            {
                this.FeedbackMessage(this.SaveContactFailureMessage, false);
                this.FeedbackMessage(this.SaveContactSuccessMessage, true);
            }
        }

        private void ResetMessage()
        {
            var needsDisplay = false;
            this.FeedbackMessage(this.SaveContactFailureMessage, needsDisplay);
            this.FeedbackMessage(this.SaveContactSuccessMessage, needsDisplay);
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
