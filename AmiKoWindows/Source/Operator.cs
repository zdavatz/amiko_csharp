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
using System.Configuration;
using System.Linq;

namespace AmiKoWindows
{
    /// https://msdn.microsoft.com/en-us/library/system.configuration.applicationsettingsbase%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
    [Serializable]
    public class Operator : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Title {
            get { return (string)this[nameof(Title)]; }
            set { this[nameof(Title)] = value; }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string GivenName {
            get { return (string)this[nameof(GivenName)]; }
            set { this[nameof(GivenName)] = value; }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string FamilyName {
            get { return (string)this[nameof(FamilyName)]; }
            set { this[nameof(FamilyName)] = value; }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Address {
            get { return (string)this[nameof(Address)]; }
            set { this[nameof(Address)] = value; }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string City {
            get { return (string)this[nameof(City)]; }
            set { this[nameof(City)] = value; }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Zip {
            get { return (string)this[nameof(Zip)]; }
            set { this[nameof(Zip)] = value; }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Phone {
            get { return (string)this[nameof(Phone)]; }
            set { this[nameof(Phone)] = value; }
        }

        [UserScopedSetting()]
        [SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")]
        public string Email {
            get { return (string)this[nameof(Email)]; }
            set { this[nameof(Email)] = value; }
        }

        static public bool ValidateProperty(string propertyName, string text)
        {
            if (propertyName == null || propertyName.Equals(string.Empty))
                return false;

            // TODO
            // consider more appropriate limitations (length, formats etc.)
            int maxLength = 255;

            // required
            var requiredPlainTextFields = new string[] {
                "GivenName", "FamilyName", "Address", "City", "Zip",
            };
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
