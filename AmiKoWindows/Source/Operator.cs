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
using System.Linq;
using System.Windows;

namespace AmiKoWindows
{
    /// https://msdn.microsoft.com/en-us/library/system.configuration.applicationsettingsbase%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
    [Serializable]
    public class Operator : ApplicationSettingsBase, INotifyPropertyChanged
    {
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Title {
            get { return (string)this[nameof(Title)]; }
            set {
                this[nameof(Title)] = value;
                OnPropertyChanged("Title");
                OnPropertyChanged("Fullname");
            }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string GivenName {
            get { return (string)this[nameof(GivenName)]; }
            set {
                this[nameof(GivenName)] = value;
                OnPropertyChanged("GivenName");
                OnPropertyChanged("Fullname");
            }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string FamilyName {
            get { return (string)this[nameof(FamilyName)]; }
            set {
                this[nameof(FamilyName)] = value;
                OnPropertyChanged("FamilyName");
                OnPropertyChanged("Fullname");
            }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Address {
            get { return (string)this[nameof(Address)]; }
            set {
                this[nameof(Address)] = value;
                OnPropertyChanged("Address");
            }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string City {
            get { return (string)this[nameof(City)]; }
            set {
                this[nameof(City)] = value;
                OnPropertyChanged("City");
                OnPropertyChanged("Place");
            }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Zip {
            get { return (string)this[nameof(Zip)]; }
            set {
                this[nameof(Zip)] = value;
                OnPropertyChanged("Zip");
                OnPropertyChanged("Place");
            }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Phone {
            get { return (string)this[nameof(Phone)]; }
            set {
                this[nameof(Phone)] = value;
                OnPropertyChanged("Phone");
            }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Email {
            get { return (string)this[nameof(Email)]; }
            set {
                this[nameof(Email)] = value;
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
            get { return Utilities.Concat(this.Title, this.GivenName, this.FamilyName); }
        }

        public string Place
        {
            get { return Utilities.Concat(this.Zip, this.City); }
        }

        public string PictureFile
        {
            get {
                return Utilities.OperatorPictureFilePath();
            }
        }
        #endregion

        static readonly string[] requiredPlainTextFields = new string[] {
            "GivenName", "FamilyName", "Address", "City", "Zip",
        };

        // Returns operator's profile (required text fields) is saved in user.config
        static public bool IsSet()
        {
            // NOTE: Namespace `AmiKoWindows` is required in static context
            if (AmiKoWindows.Properties.Settings.Default == null)
                return false;

            Operator op = AmiKoWindows.Properties.Settings.Default.Operator as Operator;
            if (op == null)
                return false;

            return 0 == requiredPlainTextFields.Where(f => {
                string v = (string)op[f];
                return (v == null || v.Equals(string.Empty));
            }).Count();
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
