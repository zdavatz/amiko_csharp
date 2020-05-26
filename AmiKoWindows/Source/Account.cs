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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;

namespace AmiKoWindows
{
    public class Account : ApplicationSettingsBase, INotifyPropertyChanged
    {
        private string _title = "";
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Title {
            get { return _title; }
            set {
                _title = value;
                OnPropertyChanged("Title");
                OnPropertyChanged("Fullname");
            }
        }

        private string _givenName = "";
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string GivenName {
            get { return _givenName; }
            set {
                this._givenName = value;
                OnPropertyChanged("GivenName");
                OnPropertyChanged("Fullname");
            }
        }

        private string _familyName;
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string FamilyName {
            get { return _familyName; }
            set {
                this._familyName = value;
                OnPropertyChanged("FamilyName");
                OnPropertyChanged("Fullname");
            }
        }

        private string _address;
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Address {
            get { return _address; }
            set {
                this._address = value;
                OnPropertyChanged("Address");
            }
        }

        private string _city;
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string City {
            get { return _city; }
            set {
                this._city = value;
                OnPropertyChanged("City");
                OnPropertyChanged("Place");
            }
        }

        private string _zip;
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Zip {
            get { return _zip; }
            set {
                this._zip = value;
                OnPropertyChanged("Zip");
                OnPropertyChanged("Place");
            }
        }

        private string _phone;
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Phone {
            get { return this._phone; }
            set {
                this._phone = value;
                OnPropertyChanged("Phone");
            }
        }

        private string _email;
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Email {
            get { return this._email; }
            set {
                this._email = value;
                OnPropertyChanged("Email");
            }
        }

        #region Event Handlers
        // NOTE: The ApplicationSettingsBase has `PropertyChanged`
        public new event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Virtual Fields
        public string Fullname
        {
            get { return Utilities.Concat(Title, GivenName, FamilyName); }
        }

        public string Place
        {
            get { return Utilities.Concat(Zip, City); }
        }

        public string PictureFile
        {
            get {
                return Utilities.AccountPictureFilePath();
            }
        }

        private string _Signature;
        public string Signature
        {
            get {
                if (_Signature != null && !_Signature.Equals(string.Empty))
                    return _Signature;

                string content = "";
                try
                {
                    var path = PictureFile;
                    if (path == null || path.Equals(string.Empty) || !File.Exists(path))
                        return content;

                    byte[] bytes = File.ReadAllBytes(path);
                    content = Convert.ToBase64String(bytes);
                }
                catch (IOException ex)
                {
                    Log.WriteLine(ex.Message);
                }
                return content;
            }

            set {
                this._Signature = value;
                OnPropertyChanged("Signature");
            }
        }
        #endregion

        public static void MigrateFromOldSettings()
        {
            // We used to save doctor information in Properties.Settings.Default.Account
            // and that has been moved to app folder's doctor.json for syncing
            if (IsSet()) return;
            var a = AmiKoWindows.Properties.Settings.Default.Account;
            if (a == null) return;
            a.Reload();
            var account = new Account();
            account.Title = (string)a[nameof(Title)];
            account.GivenName = (string)a[nameof(GivenName)];
            account.FamilyName = (string)a[nameof(FamilyName)];
            account.Address = (string)a[nameof(Address)];
            account.City = (string)a[nameof(City)];
            account.Zip = (string)a[nameof(Zip)];
            account.Phone = (string)a[nameof(Phone)];
            account.Email = (string)a[nameof(Email)];
            account.Save();
            AmiKoWindows.Properties.Settings.Default.Reset();
        }

        static public Account Read()
        {
            var accountPath = AccountFilePath();
            if (!File.Exists(accountPath))
            {
                return null;
            }

            string jsonStr;
            using (var fileStream = new FileStream(accountPath, FileMode.Open,
                              FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    jsonStr = reader.ReadToEnd();
                }
            }
            var serializer = new JavaScriptSerializer();
            SettingAccountJSONPresenter presenter = serializer.Deserialize<SettingAccountJSONPresenter>(jsonStr);
            return presenter.Account;
        }
        
        public override void Save()
        {
            var serializer = new JavaScriptSerializer();
            var str = serializer.Serialize(new SettingAccountJSONPresenter(this));
            File.WriteAllText(AccountFilePath(), str);
        }

        public static string AccountFilePath()
        {
            var filesDir = Utilities.AppRoamingDataFolder();
            var accountPath = Path.Combine(filesDir, "doctor.json");
            return accountPath;
        }

        static readonly string[] requiredPlainTextFields = new string[] {
            "GivenName", "FamilyName", "Address", "City", "Zip",
        };

        // Returns profile (required text fields) is saved in user.config
        static public bool IsSet()
        {
            // NOTE: Namespace `AmiKoWindows` is required in static context
            if (!File.Exists(AccountFilePath()))
            {
                return false;
            }

            Account account = Read();
            if (account == null)
                return false;

            if (account.GivenName.Equals("")
                || account.FamilyName.Equals("")
                || account.Address.Equals("")
                || account.City.Equals("")
                || account.Zip.Equals("")
                )
            {
                return false;
            }
            return true;
        }

        static public bool ValidateProperty(string propertyName, string text)
        {
            if (propertyName == null || propertyName.Equals(string.Empty))
                return false;

            // TODO
            // consider more appropriate limitations (length, formats etc.)
            int maxLength = 255;

            // required
            if (requiredPlainTextFields.Contains(propertyName))
                return text != string.Empty && text.Length < maxLength;
            else if (propertyName.Equals("Email"))
                // TODO
                return text != string.Empty && text.Length < maxLength;

            // optional
            if (propertyName.Equals("Title"))
                return text.Length < maxLength;
            else if (propertyName.Equals("Phone"))
                return text.Length < maxLength;

            return false;
        }
    }
}
