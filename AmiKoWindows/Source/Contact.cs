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
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace AmiKoWindows
{
    public class Contact : INotifyPropertyChanged
    {
        // Previous version of seems to use a different timestamp format than the one in Android / iOS / Mac
        public const string TIME_STAMP_DATE_FORMAT = "yyyy-MM-dd'T'HH:mm.ss";
        public const string OLD_TIME_STAMP_DATE_FORMAT = "yyyy-MM-dd'T'HHmmss";

        private const int GENDER_FEMALE = 0;
        private const int GENDER_MALE = 1;

        public Contact()
        {
            OnPropertyChanged(string.Empty);
        }

        private long? _Id;
        public long? Id
        {
            get { return _Id; }
            set { SetField(ref _Id, value, "Id"); }
        }

        private string _TimeStamp;
        public string TimeStamp
        {
            get { return _TimeStamp; }
            set { SetField(ref _TimeStamp, value, "TimeStamp"); }
        }

        private string _Uid;
        public string Uid
        {
            get { return _Uid; }
            set { SetField(ref _Uid, value, "Uid"); }
        }

        private string _GivenName;
        public string GivenName
        {
            get { return _GivenName; }
            set
            {
                SetField(ref _GivenName, value, "GivenName");
                OnPropertyChanged("Fullname");
            }
        }

        private string _FamilyName;
        public string FamilyName
        {
            get { return _FamilyName; }
            set
            {
                SetField(ref _FamilyName, value, "FamilyName");
                OnPropertyChanged("Fullname");
            }
        }

        private string _Birthdate;
        public string Birthdate
        {
            get { return _Birthdate; }
            set { SetField(ref _Birthdate, value, "Birthdate"); }
        }

        // NOTE:
        // The Following 3 fields (Gender/WeightKg/HeightCm) have 2 setters
        // for raw value.
        private int _Gender;
        public int RawGender
        {
            get { return _Gender; }
            set
            {
                SetField(ref _Gender, value, "Gender");
                OnPropertyChanged("IsMale");
                OnPropertyChanged("IsFemale");
            }
        }
        public string Gender
        {
            get { return _Gender.ToString(); }
            set
            {
                int val = GENDER_FEMALE;
                Int32.TryParse(value.ToString(), out val);
                SetField(ref _Gender, val == GENDER_MALE ? GENDER_MALE : GENDER_FEMALE, "Gender");
                OnPropertyChanged("IsMale");
                OnPropertyChanged("IsFemale");
            }
        }

        private float _WeightKg;
        public float RawWeightKg
        {
            get { return _WeightKg; }
            set { SetField(ref _WeightKg, value, "WeightKg"); }
        }
        public string WeightKg
        {
            get { return _WeightKg == 0f ? "" : _WeightKg.ToString(); }
            set
            {
                float val = 0f; float.TryParse(value.ToString(), out val);
                SetField(ref _WeightKg, val, "WeightKg");
            }
        }

        private float _HeightCm;
        public float RawHeightCm
        {
            get { return _HeightCm; }
            set { SetField(ref _HeightCm, value, "HeightCm"); }
        }
        public string HeightCm
        {
            get { return _HeightCm == 0f ? "" : _HeightCm.ToString(); }
            set
            {
                float val = 0f; float.TryParse(value.ToString(), out val);
                SetField(ref _HeightCm, val, "HeightCm");
            }
        }

        private string _Zip;
        public string Zip
        {
            get { return _Zip; }
            set
            {
                SetField(ref _Zip, value, "Zip");
                OnPropertyChanged("Place");
            }
        }

        private string _City;
        public string City
        {
            get { return _City; }
            set
            {
                SetField(ref _City, value, "City");
                OnPropertyChanged("Place");
            }
        }

        private string _Country;
        public string Country
        {
            get { return _Country; }
            set { SetField(ref _Country, value, "Country"); }
        }

        private string _Address;
        public string Address
        {
            get { return _Address; }
            set { SetField(ref _Address, value, "Address"); }
        }

        private string _Phone;
        public string Phone
        {
            get { return _Phone; }
            set { SetField(ref _Phone, value, "Phone"); }
        }

        private string _Email;
        public string Email
        {
            get { return _Email; }
            set { SetField(ref _Email, value, "Email"); }
        }

        #region Virtual Fields
        // for `Gender` (TwoWay for RadioButton in Xaml)
        public bool IsFemale
        {
            get { return _Gender == GENDER_FEMALE; }
            set { SetField(ref _Gender, value ? GENDER_FEMALE : GENDER_MALE, "Gender"); }
        }

        public bool IsMale
        {
            get { return _Gender == GENDER_MALE; }
            set { SetField(ref _Gender, value ? GENDER_MALE : GENDER_FEMALE, "Gender"); }
        }

        // for view
        public string Fullname
        {
            get { return Utilities.Concat(this.GivenName, this.FamilyName); }
        }

        public string Place
        {
            get { return Utilities.Concat(this.Zip, this.City); }
        }

        public string PersonalInfo
        {
            get {
                if (Uid == null || Uid.Equals(string.Empty)) { return ""; }
                var w = WeightKg; if (w != null && !w.Equals(string.Empty)) w += "kg";
                var h = HeightCm; if (h != null && !h.Equals(string.Empty)) h += "cm";
                var g = ""; if (IsFemale) g = "F"; else if (IsMale) g = "M";
                return Utilities.Concat(Utilities.ConcatWith("/", w, h), g, Birthdate);
            }
        }

        public bool HasName
        {
            get {
                var name = Fullname.Replace(" ", "");
                return (name != null && !name.Equals(string.Empty));
            }
        }
        #endregion

        #region Setter/Getter Utilities
        public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set {
                this.GetType().GetProperty(propertyName).SetValue(this, value, null);
                OnPropertyChanged(propertyName);
            }
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        // manually clone
        public Contact Clone()
        {
            var contact = new Contact();
            contact._Id = Id;
            contact._TimeStamp = TimeStamp;
            contact._GivenName = GivenName;
            contact._FamilyName = FamilyName;
            contact._Birthdate = Birthdate;
            contact._Gender = RawGender;
            contact._WeightKg = RawWeightKg;
            contact._HeightCm = RawHeightCm;
            contact._Zip = Zip;
            contact._City = City;
            contact._Country = Country;
            contact._Address = Address;
            contact._Phone = Phone;
            contact._Email = Email;

            contact._Uid = contact.GenerateUid();
            return contact;
        }

        // ## NOTE
        //
        // Check also the early implementation on macOS/iOS Version (til, AmiKo macOS v3.4.4, AmiKo iOS v2.8.143):
        // Its rely on the value of __current__ NSString's `hash`. Thus We need to keep this hashed value by its algorithm for consistency.
        //
        // * https://github.com/zdavatz/amiko-osx/blob/a4892277bde48e358c9e3042b14bf8b6cddd22c4/MLPatient.m#L70
        // * https://github.com/zdavatz/AmiKo-iOS/blob/d1ad38727931bb3b079bfff85d1d93dbcc8de567/AmiKoDesitin/MLPatient.m#L50
        //
        // This `GenerateUid` function is based on the hashed value by `Utilities.Hash` function same as NSString's (CFString CF-1151.16) Hash Implementation in Obj-C.
        // But it's written in C# (in Utilities.cs).
        //
        // See also `Utilities.Hash` function (Utilities.cs) and unit tests (UtilityTest.cs).
        public string GenerateUid()
        {
            // e.g. davatz.zeno.2.6.1942
            string baseString = String.Format(
                "{0}.{1}.{2}", FamilyName, GivenName, Birthdate).ToLower();
            return Utilities.GenerateHash(baseString);
        }

        // Returns a dictionary contains db parameters to SqliteCommand. e.g. @id => "1"
        public Dictionary<string, string> ToParameters(string[] columnNames)
        {
            int length = columnNames.Length;
            var result = new Dictionary<string, string>();

            for (int i = 0; i < length; i++)
            {
                var columnName = columnNames[i];
                if (columnName == null || columnName.Equals(string.Empty) ||
                    columnName.Equals("item") || columnName.Equals("_id"))
                    continue;

                var propertyName = Utilities.ConvertSnakeCaseToTitleCase(columnName);
                var key = String.Format("@{0}", columnName);
                result.Add(key, GetStringValue(propertyName));
            }
            return result;
        }

        // returns raw property value as string (for database value)
        private string GetStringValue(string propertyName)
        {
            string text;
            switch (propertyName)
            {
                case "Gender":
                    text = ((int)this[String.Format("Raw{0}", propertyName)]).ToString();
                    break;
                case "WeightKg":
                case "HeightCm":
                    // NOTE: value will be rounded
                    text = ((float)this[String.Format("Raw{0}", propertyName)]).ToString(
                        "F2", CultureInfo.InvariantCulture);
                    break;
                default:
                    text = this[propertyName] as string;
                    break;
            }
            return text;
        }

        public Contact(IDictionary<string, string> dict)
        {
            this.TimeStamp = dict[PatientDb.KEY_TIME_STAMP];
            this.Uid = dict[PatientDb.KEY_UID];
            this.FamilyName = dict[PatientDb.KEY_FAMILY_NAME];
            this.GivenName = dict[PatientDb.KEY_GIVEN_NAME];
            this.Birthdate = dict[PatientDb.KEY_BIRTHDATE];
            this.Gender = dict[PatientDb.KEY_GENDER];
            this.WeightKg = dict[PatientDb.KEY_WEIGHT_KG];
            this.HeightCm = dict[PatientDb.KEY_HEIGHT_CM];
            this.Zip = dict[PatientDb.KEY_ZIP];
            this.City = dict[PatientDb.KEY_CITY];
            this.Country = dict[PatientDb.KEY_COUNTRY];
            this.Address = dict[PatientDb.KEY_ADDRESS];
            this.Phone = dict[PatientDb.KEY_PHONE];
            this.Email = dict[PatientDb.KEY_EMAIL];
        }

        public Dictionary<string, string>ToMapForSync()
        {
            var dict = new Dictionary<string, string>();
            string timeStampString;
            try
            {
                DateTime.ParseExact(this.TimeStamp, TIME_STAMP_DATE_FORMAT, null);
                timeStampString = this.TimeStamp;
            } catch (FormatException e)
            {
                // Migrate from the old format
                var dt = DateTime.ParseExact(this.TimeStamp, OLD_TIME_STAMP_DATE_FORMAT, null);
                timeStampString = dt.ToString(TIME_STAMP_DATE_FORMAT);
            }
            dict[PatientDb.KEY_TIME_STAMP] = timeStampString;
            dict[PatientDb.KEY_UID] = this.Uid;
            dict[PatientDb.KEY_FAMILY_NAME] = this.FamilyName;
            dict[PatientDb.KEY_GIVEN_NAME] = this.GivenName;
            dict[PatientDb.KEY_BIRTHDATE] = this.Birthdate;
            dict[PatientDb.KEY_GENDER] = this.Gender;
            dict[PatientDb.KEY_WEIGHT_KG] = this.WeightKg;
            dict[PatientDb.KEY_HEIGHT_CM] = this.HeightCm;
            dict[PatientDb.KEY_ZIP] = this.Zip;
            dict[PatientDb.KEY_CITY] = this.City;
            dict[PatientDb.KEY_COUNTRY] = this.Country;
            dict[PatientDb.KEY_ADDRESS] = this.Address;
            dict[PatientDb.KEY_PHONE] = this.Phone;
            dict[PatientDb.KEY_EMAIL] = this.Email;
            return dict;
        }
    }
}
