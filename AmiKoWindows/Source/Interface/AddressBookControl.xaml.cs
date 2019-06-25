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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.VisualBasic.FileIO;
using MahApps.Metro.Controls;

namespace AmiKoWindows
{
    using ControlExtensions;
    using System.Threading.Tasks;

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

        bool _openCsvFile = false;
        string _csvFilepath = null;
        List<Contact> _contacts = new List<Contact>(); // cached
        #endregion

        private long RawContactsCount {
            set {
                this.ContactsCount = String.Format("({0})", value.ToString());
                OnPropertyChanged("ContactsCount");
            }
        }

        #region Public Accessors
        public string ContactsCount { get; set; }

        private string _SearchTextBoxWaterMark;
        public string SearchTextBoxWaterMark
        {
            get { return _SearchTextBoxWaterMark; }
            set {
                _SearchTextBoxWaterMark = value;
                OnPropertyChanged("SearchTextBoxWaterMark");
            }
        }

        private Contact _CurrentEntry;
        public Contact CurrentEntry {
            get { return _CurrentEntry; }
            set {
                this._CurrentEntry = value;
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

        public static string FormatBirthdate(string text)
        {
            if (text == null || text.Equals(string.Empty))
                return text;

            // Input Support e.g. 08.06.2018 -> 8.6.2018
            string result = text;
            result = PatientDb.BIRTHDATE_NONDEVIDER_RGX.Replace(result, ".");
            result = PatientDb.BIRTHDATE_ZEROPADDED_RGX.Replace(result, "$1");
            return result;
        }

        public AddressBookControl()
        {
            this.Initialized += delegate
            {
                // This block is called after InitializeComponent
                this.DataContext = this;
            };

            InitializeComponent();

            // TODO
            // This method does not clear focus :'(
            Keyboard.ClearFocus();
            // Workaround
            SearchPatientBox.Focus();
            Keyboard.Focus(SearchPatientBox);
        }

        public async void Select(Contact contact)
        {
            if (_patientDb != null && contact != null)
            {
                this.CurrentEntry = await _patientDb.GetContactById(contact.Id);

                await _patientDb.LoadAllContacts();
                _patientDb.UpdateContactList();

                this.RawContactsCount = _patientDb.Count;
            }

            SetCurrentEntryAsSelected();
            EnableButton("MinusButton", true);
        }

        public async Task<Contact> ReceivedCardResult(SmartCard.Result r)
        {
            ContactList.UnselectAll();
            this.CurrentEntry = new Contact();

            ResetFields();
            ResetMessage();

            this.CurrentEntry.FamilyName = r.FamilyName;
            this.CurrentEntry.GivenName = r.GivenName;
            this.CurrentEntry.Birthdate = r.BirthDate;

            const int GENDER_FEMALE = 0;
            const int GENDER_MALE = 1;
            if (r.Gender.Equals("woman"))
            {
                this.CurrentEntry.RawGender = GENDER_FEMALE;
            }
            else if (r.Gender.Equals("man"))
            {
                this.CurrentEntry.RawGender = GENDER_MALE;
            }
            EnableButton("MinusButton", false);

            if (_patientDb != null)
            {
                var uid = this.CurrentEntry.GenerateUid();
                var existingContact = await _patientDb.GetContactByUid(uid);
                if (existingContact != null)
                {
                    this.CurrentEntry = existingContact;
                }
                SetCurrentEntryAsSelected();
                EnableButton("MinusButton", true);
                return existingContact;
            } else {
                return null;
            }
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(e.ToString());

            _parent = Parent as MahApps.Metro.Controls.Flyout;
            _mainWindow = Window.GetWindow(_parent.Parent) as AmiKoWindows.MainWindow;

            // maybe we should make shared instance...
            FieldInfo info = _mainWindow.GetType().GetField("_patientDb", BindingFlags.NonPublic | BindingFlags.Instance);
            _patientDb = (PatientDb)info.GetValue(_mainWindow);

            this.ContactList.DataContext = _patientDb;
            this.CurrentEntry = new Contact();
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var isVisible = e.NewValue as bool?;
            if (isVisible != null && (bool)isVisible.Value)
            {
                _parent.AreAnimationsEnabled = false; // disable closing animation
                _patientDb.UpdateContactList();
                this.RawContactsCount = _patientDb.Count;

                SetCurrentEntryAsSelected();
            }
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
                box.Text = AddressBookControl.FormatBirthdate(box.Text);

            ValidateField(box);
        }

        private void FemaleButton_Checked(object sender, RoutedEventArgs e)
        {
            //Log.WriteLine("Source: {0}", e.Source);
            if (CurrentEntry != null)
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

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            if (_openCsvFile)
            {
                this._openCsvFile = false;
                this._contacts = new List<Contact>();
                EnableFileBar(false);
                await _patientDb.LoadAllContacts();
                _patientDb.UpdateContactList();
            }

            if (_parent != null)
                _parent.IsOpen = false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            bool result = ValidateFields();
            Contact contact = null;
            if (result)
            {
                Dictionary<string, string> values = GetContactValues();
                contact = CurrentEntry;
                if (contact == null || contact.Uid == null || contact.Uid.Equals(string.Empty))
                    contact = _patientDb.InitContact(values);

                foreach (var v in values)
                {
                    string val = v.Value;
                    if (v.Key.Equals("Birthdate"))
                        val = AddressBookControl.FormatBirthdate(val);

                    contact[v.Key] = val;
                }

                if (contact.Uid != null && !contact.Uid.Equals(string.Empty) && contact.Uid.Equals(contact.GenerateUid()))
                {
                    await _patientDb.UpdateContact(contact);
                    await _patientDb.LoadAllContacts();  // need reload all :'(
                }
                else
                {
                    // NEW Entry or `Contact.Uid` has been changed
                    long? id = await _patientDb.InsertContact(contact);
                    if (id != null && id.Value > 0)
                    {
                        contact.Id = id.Value;
                        this.SearchPatientBox.Text = "";
                    }
                    else
                        // TODO need another message? (already exists)
                        ShowMessage(true);

                    await _patientDb.LoadAllContacts();
                    this.RawContactsCount = _patientDb.Count;
                }
            }

            if (contact == null || contact.Uid == null || !result)
                return;
            else
            {
                this.CurrentEntry = contact;
                _patientDb.UpdateContactList();

                if (_openCsvFile)
                {
                    this._openCsvFile = false;
                    EnableFileBar(false);
                    EnableButton("PlusButton", true);
                    EnableButton("MinusButton", true);
                }

                // See ContactList_ItemStatusChanged
                this.ContactList.ItemContainerGenerator.StatusChanged += ContactList_ItemStatusChanged;
            }
            e.Handled = true;
        }
        #endregion

        #region Actions on Right Pane
        private async void SearchPatientBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = sender as TextBox;
            string text = box.Text;

            Log.WriteLine("_openCsvFile: {0}", _openCsvFile);
            if (_openCsvFile)
            {
                var clone = _contacts.Select(c => c.Clone()).ToList();
                await _patientDb.Search(text, clone);
            }
            else
                await _patientDb.Search(text);

            this.RawContactsCount = _patientDb.Count;
        }

        private void SearchPatientBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            this.SearchPatientBox.Text = "";
        }

        #region Contact ListBox
        private void ContactList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;

            // a hack to enable item selection using arrow keys as tab
            if (object.ReferenceEquals(sender, ContactList) && e.IsDown)
            {
                if ((e.Key == Key.Down) || e.Key == Key.Up)
                {
                    this._isItemClick = true;
                    // it works same as `sendkeys(\t)`
                    if (Keyboard.PrimaryDevice != null && Keyboard.PrimaryDevice.ActiveSource != null)
                    {
                        var key = Key.Tab;
                        var args = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key)
                        {
                            RoutedEvent = Keyboard.KeyUpEvent,
                        };
                        InputManager.Current.ProcessInput(args);
                    }
                }
            }
        }

        private void ContactList_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;


            if (object.ReferenceEquals(sender, ContactList))
            {
                if (e.IsDown && e.Key == Key.Tab)
                    this._isItemClick = true;
                else if (e.IsDown && e.Key == Key.Back && !_openCsvFile)
                    MinusButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        // ItemContainerGenerator
        private void ContactList_ItemStatusChanged(object sender, EventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            // Re:set selected list item, `UpdateLayout` is needed.
            if (ContactList.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                this.ContactList.ItemContainerGenerator.StatusChanged -= ContactList_ItemStatusChanged;

                SetCurrentEntryAsSelected();
            }
        }

        // DoubleClick
        private async void ContactList_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (_openCsvFile)
            {
                this._openCsvFile = false;
                this._contacts = new List<Contact>();
                return;
            }

            if (object.ReferenceEquals(sender, ContactList))
            {
                if (ContactList.SelectedItem != null)
                {
                    Log.WriteLine("{0}", ContactList.SelectedItem);

                    var item = ContactList.SelectedItem as Item;
                    if (item != null && item.Id != null)
                    {
                        Contact contact = await _patientDb.GetContactById(item.Id.Value);
                        if (contact != null)
                        {
                            this.CurrentEntry = contact;
                            if (_mainWindow != null)
                                _mainWindow.ActiveContact = contact;
                        }
                    }
                }
            }
            if (_parent != null)
                _parent.IsOpen = false;
        }

        private async void ContactList_SelectionChanged(object sender, EventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            // fix scroll position
            ContactList.ScrollIntoView(ContactList.SelectedItem);

            // NOTE:
            // This private variable prevents event triggered by the manual assignment
            // to `IsSelected` in `SaveButton_Click`
            if (!_isItemClick)
                return;
            this._isItemClick = false;

            var item = ContactList.SelectedItem as Item;
            if (item != null && item.Id != null)
            {
                ResetMessage();

                // clear only error styles on field
                foreach (string field in contactFields)
                    this.FeedbackField<TextBox>(FindName(field) as TextBox, false);

                Contact contact = await _patientDb.GetContactById(item.Id.Value);
                if (contact != null)
                    this.CurrentEntry = contact;
            }
        }

        // Preview action
        private void ContactItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            this._isItemClick = true;
            EnableButton("MinusButton", !_openCsvFile);
        }

        private void ContactItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            var menu = (sender as FrameworkElement)?.ContextMenu;
            if (menu != null)
            {
                if (_openCsvFile)
                {
                    menu.IsEnabled = false;
                    menu.Visibility = Visibility.Collapsed;
                }
                else
                {
                    menu.IsEnabled = true;
                    menu.Visibility = Visibility.Visible;
                }
            }

            e.Handled = _openCsvFile;
        }

        private void ContactContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            if (_openCsvFile)
                return;

            var item = (sender as MenuItem)?.DataContext as Item;
            if (item != null && item.Id != null)
            {
                MinusButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
            }
        }
        #endregion

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            if (_openCsvFile)
                return;

            ContactList.UnselectAll();
            this.CurrentEntry = new Contact();

            ResetFields();
            ResetMessage();

            GivenName.Focus();

            EnableButton("MinusButton", false);
        }

        private async void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            if (_openCsvFile)
                return;

            var item = ContactList.SelectedItem as Item;
            if (item == null || item.Id == null)
            {
                EnableButton("MinusButton", false);
                e.Handled = true;
                return;
            }

            //Log.WriteLine(sender.GetType().Name);
            var dialog = Utilities.MessageDialog(
                Properties.Resources.msgContactDeleteConfirmation, "", "OKCancel");
            dialog.ShowDialog();
            var result = dialog.MessageBoxResult;

            if (result == MessageBoxResult.OK)
            {
                Contact contact = await _patientDb.GetContactById(item.Id.Value);
                if (contact == null)
                    return;

                if (_mainWindow != null && _mainWindow.ActiveContact != null &&
                    contact.Uid.Equals(_mainWindow.ActiveContact.Uid))
                {
                    _mainWindow.ActiveContact = null;
                    _mainWindow.FillContactFields();
                }

                var uid = contact.Uid;
                if (uid == null)
                    return;
                ResetFields();
                this.CurrentEntry = new Contact();

                await _patientDb.DeleteContact(contact.Id.Value);
                await _patientDb.LoadAllContacts();
                this.RawContactsCount = _patientDb.Count;
                _patientDb.UpdateContactList();

                string[] dirs = new string[]
                {
                    Path.Combine(Utilities.GetInboxPath(), uid),
                    Path.Combine(Utilities.PrescriptionsPath(), uid)
                };
                await Utilities.DeleteAll(dirs);
                EnableButton("MinusButton", false);
            }
        }

        private async void SwitchBookButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            if (_openCsvFile)
            {
                await _patientDb.LoadAllContacts();
                _patientDb.UpdateContactList();
                _openCsvFile = false;
                EnableFileBar(false);
                EnableButton("PlusButton", true);
            }
            else
            {
                string filepath = null;
                if (_csvFilepath != null)
                    filepath = _csvFilepath;
                else
                {
                    var dialog = new System.Windows.Forms.OpenFileDialog();
                    dialog.Filter = String.Format(
                        "{0} | {1}", "CSV File (*.csv)", "*.csv");
                    dialog.DefaultExt = ".csv";
                    var result = dialog.ShowDialog();
                    switch (result)
                    {
                        case System.Windows.Forms.DialogResult.OK:
                            var file = dialog.FileName;
                            var name = Path.GetFileName(file);
                            var tmpPath = Path.Combine(Utilities.GetInboxPath(), name);
                            if (File.Exists(tmpPath))
                                File.Delete(tmpPath);

                            File.Copy(file, tmpPath);

                            if (File.Exists(tmpPath))
                                filepath = tmpPath;
                            break;
                        case System.Windows.Forms.DialogResult.Cancel:
                        default:
                            break;
                    }
                }

                if (filepath != null)
                {
                    Log.WriteLine("filepath: {0}", filepath);

                    ResetFields();
                    this.CurrentEntry = new Contact();
                    this._csvFilepath = filepath;
                    ImportContacts();
                    Log.WriteLine("_contacts (cache): {0}", _contacts.Count);
                    EnableButton("PlusButton", false);
                    EnableButton("MinusButton", false);
                }
            }
            this.RawContactsCount = _patientDb.Count;
        }

        private async void DeleteCsvFileButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            await _patientDb.LoadAllContacts();
            _patientDb.UpdateContactList();
            this._openCsvFile = false;
            this._contacts = new List<Contact>();
            this._csvFilepath = null;
            EnableFileBar(false);
            EnableButton("PlusButton", true);
        }
        #endregion

        private void ImportContacts()
        {
            var filepath = this._csvFilepath;
            if (filepath == null || !File.Exists(filepath))
                return;

            var contacts = new List<Contact>();
            if (_contacts.Count > 0) // cached
            {
                Log.WriteLine("cached");
                contacts = _contacts;
            }
            else
            {
                Log.WriteLine("loaded");
                using (TextFieldParser parser = new TextFieldParser(filepath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    var i = 0;
                    Dictionary<string, int> keys = new Dictionary<string, int>();
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        // Log.WriteLine("Fields (Length): {0}", fields.Length);
                        if (fields.Length < 60) // default keys count 61
                            return;
                        if (i < 1)
                        {
                            var j = 0;
                            // NOTE:
                            // exported csv from (as Outlook format CSV)
                            //
                            // * https://outlook.live.com/people
                            // * https://contacts.google.com
                            //
                            // ## Header
                            //
                            // Default (no change by user) Headers
                            //
                            // (from outlook.live.com)
                            // First Name,Middle Name,Last Name,Title,Suffix,Nickname,Given Yomi,Surname Yomi,E-mail Address,E-mail 2 Address,E-mail 3 Address,Home Phone,Home Phone 2,Business Phone,Business Phone 2,Mobile Phone,Car Phone,Other Phone,Primary Phone,Pager,Business Fax,Home Fax,Other Fax,Company Main Phone,Callback,Radio Phone,Telex,TTY/TDD Phone,IMAddress,Job Title,Department,Company,Office Location,Manager's Name,Assistant's Name,Assistant's Phone,Company Yomi,Business Street,Business City,Business State,Business Postal Code,Business Country/Region,Home Street,Home City,Home State,Home Postal Code,Home Country/Region,Other Street,Other City,Other State,Other Postal Code,Other Country/Region,Personal Web Page,Spouse,Schools,Hobby,Location,Web Page,Birthday,Anniversary,Notes
                            //
                            // (from google.com)
                            // First Name,Middle Name,Last Name,Title,Suffix,Initials,Web Page,Gender,Birthday,Anniversary,Location,Language,Internet Free Busy,Notes,E-mail Address,E-mail 2 Address,E-mail 3 Address,Primary Phone,Home Phone,Home Phone 2,Mobile Phone,Pager,Home Fax,Home Address,Home Street,Home Street 2,Home Street 3,Home Address PO Box,Home City,Home State,Home Postal Code,Home Country,Spouse,Children,Manager's Name,Assistant's Name,Referred By,Company Main Phone,Business Phone,Business Phone 2,Business Fax,Assistant's Phone,Company,Job Title,Department,Office Location,Organizational ID Number,Profession,Account,Business Address,Business Street,Business Street 2,Business Street 3,Business Address PO Box,Business City,Business State,Business Postal Code,Business Country,Other Phone,Other Fax,Other Address,Other Street,Other Street 2,Other Street 3,Other Address PO Box,Other City,Other State,Other Postal Code,Other Country,Callback,Car Phone,ISDN,Radio Phone,TTY/TDD Phone,Telex,User 1,User 2,User 3,User 4,Keywords,Mileage,Hobby,Billing Information,Directory Server,Sensitivity,Priority,Private,Categorie
                            foreach (var text in fields)
                            {
                                // first match priority
                                if (!keys.ContainsKey("GivenName") && (text.Equals("First Name") || text.Equals("Middle Name")))
                                    keys.Add("GivenName", j);
                                else if (text.Equals("Last Name"))
                                    keys.Add("FamilyName", j);
                                else if (!keys.ContainsKey("Address") && (text.Equals("Home Address") || text.Equals("Home Street")))
                                    keys.Add("Address", j);
                                else if (!keys.ContainsKey("City") && (text.Equals("Home City") || text.Equals("Home State")))
                                    keys.Add("City", j);
                                else if (text.Equals("Home Postal Code"))
                                    keys.Add("Zip", j);
                                else if (!keys.ContainsKey("Country") && (text.Equals("Home Country/Region") || text.Equals("Home Country")))
                                    keys.Add("Country", j);
                                else if (!keys.ContainsKey("Phone") && (text.Equals("Home Phone") || text.Equals("Mobile Phone")))
                                    keys.Add("Phone", j);
                                else if (!keys.ContainsKey("Email") && (text.Equals("E-mail Address") || text.Equals("E-mail 2 Address") || text.Equals("E-mail 3 Address")))
                                    keys.Add("Email", j);
                                else if (text.Equals("Birthday"))
                                    keys.Add("Birthdate", j);
                                j++;
                            }
                            i++;
                            continue;
                        }

                        var contact = new Contact();
                        if (keys.ContainsKey("GivenName"))
                            contact.GivenName = fields[keys["GivenName"]];
                        if (keys.ContainsKey("FamilyName"))
                            contact.FamilyName = fields[keys["FamilyName"]];
                        if (keys.ContainsKey("Address"))
                            contact.Address = fields[keys["Address"]];
                        if (keys.ContainsKey("City"))
                            contact.City = fields[keys["City"]];
                        if (keys.ContainsKey("Zip"))
                            contact.Zip = fields[keys["Zip"]];
                        if (keys.ContainsKey("Country"))
                            contact.Country = fields[keys["Country"]];
                        if (keys.ContainsKey("Phone"))
                            contact.Phone = fields[keys["Phone"]];
                        if (keys.ContainsKey("Email"))
                            contact.Email = fields[keys["Email"]];
                        if (keys.ContainsKey("Birthdate"))
                            contact.Birthdate = AddressBookControl.FormatBirthdate(fields[keys["Birthdate"]]);

                        if (contact.HasName)
                        {
                            contact.Id = i;
                            contacts.Add(contact);
                        }
                        i++;
                    }
                }
            }
            Log.WriteLine("contacts: {0}", contacts.Count);
            this._contacts = contacts;

            this._openCsvFile = true;
            this.CsvFileName.Text = Path.GetFileName(filepath);
            EnableFileBar(true);

            var clone = _contacts.Select(c => c.Clone()).ToList();
            _patientDb.UpdateContactList(clone);
        }

        private void SetCurrentEntryAsSelected()
        {
            if (CurrentEntry == null || CurrentEntry.Id == null)
                return;

            for (var i = 0; i < ContactList.Items.Count; i++)
            {
                var item = ContactList.Items[i] as Item;
                if (item != null && item.Id != null && item.Id.Value == CurrentEntry.Id)
                {
                    ContactList.SelectedIndex = i;
                    break;
                }
            }
        }

        // returns dictionary contains key (propertyName) and value
        private Dictionary<string, string> GetContactValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string field in contactFields)
            {
                var element = FindName(field) as FrameworkElement;
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
                var element = FindName(field) as FrameworkElement;
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
                var element = FindName(field) as FrameworkElement;
                var result = ValidateField(element);
                Log.WriteLine("field: {0} validateField: {1}", field, result);
                if (!hasError)
                    hasError = !result;
            }
            Log.WriteLine("hasError: {0}", hasError);

            ShowMessage(hasError);
            return !hasError;
        }

        private void ShowMessage(bool hasError)
        {
            Log.WriteLine("hasError: {0}", hasError);
            if (hasError)
            {
                this.FeedbackMessage(SaveContactFailureMessage, true);
                this.FeedbackMessage(SaveContactSuccessMessage, false);
            }
            else
            {
                this.FeedbackMessage(SaveContactFailureMessage, false);
                this.FeedbackMessage(SaveContactSuccessMessage, true);
            }
        }

        private void ResetMessage()
        {
            Log.WriteLine("");
            var needsDisplay = false;
            this.FeedbackMessage(SaveContactFailureMessage, needsDisplay);
            this.FeedbackMessage(SaveContactSuccessMessage, needsDisplay);
        }

        private void EnableFileBar(bool isVisible)
        {
            this.CsvFileBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            this.ContactList.Height = isVisible ? 404 : 432; // ugly, but auto re-sizing is not working ... :'(
            this.ContactList.UpdateLayout();
        }

        private void EnableButton(string name, bool isEnabled)
        {
            var button = this.FindVisualChildren<Button>().First(b => b.Name.Equals(name)) as Button;
            if (button != null)
                button.IsEnabled = isEnabled;

            // icon button
            var image = button.Content as FontAwesome.WPF.ImageAwesome;
            if (image != null)
                if (isEnabled)
                    image.Foreground = Brushes.Black;
                else
                    image.Foreground = Brushes.LightGray;
        }
    }
}
