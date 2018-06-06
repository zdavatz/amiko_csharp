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

namespace AmiKoWindows
{
    public class Contact : INotifyPropertyChanged
    {
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

        private string _FamilyName;
        public string FamilyName
        {
            get { return _FamilyName; }
            set { SetField(ref _FamilyName, value, "FamilyName"); }
        }

        private string _GivenName;
        public string GivenName
        {
            get { return _GivenName; }
            set { SetField(ref _GivenName, value, "GivenName"); }
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
            set { SetField(ref _Gender, value, "Gender"); }
        }
        public string Gender
        {
            get { return _Gender.ToString(); }
            set
            {
                int val = GENDER_FEMALE;
                Int32.TryParse(value.ToString(), out val);
                SetField(ref _Gender, val == GENDER_MALE ? GENDER_MALE : GENDER_FEMALE, "Gender");
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
            set { SetField(ref _Zip, value, "Zip"); }
        }

        private string _City;
        public string City
        {
            get { return _City; }
            set { SetField(ref _City, value, "City"); }
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

        // Additional virtual properties for `Gender` (OneWay)
        public bool IsFemale
        {
            get { return _Gender == GENDER_FEMALE; }
        }

        public bool IsMale
        {
            get { return _Gender == GENDER_MALE; }
        }

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

        // NOTE:
        //
        // See also the implementation on macOS Version (v3.4.4):
        // https://github.com/zdavatz/amiko-osx/blob/a4892277bde48e358c9e3042b14bf8b6cddd22c4/MLPatient.m#L70
        //
        // * https://developer.apple.com/documentation/foundation/nsstring/1417245-hash
        // * https://msdn.microsoft.com/en-us/library/system.string.gethashcode%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
        public string GenerateUid()
        {
            DateTimeOffset offset = DateTimeOffset.UtcNow;
            string timestamp = offset.ToUnixTimeMilliseconds().ToString();

            string baseString = String.Format(
                "{0}.{1}.{2}.{3}", this.FamilyName, this.GivenName, this.Birthdate, timestamp);

            Log.WriteLine("baseString: {0}", baseString);
            return Utilities.GenerateHash(baseString);
        }

        // Returns flatten property values for specified names, using delimiter and enclosure (as needed)
        public string Flatten(string delimiter, string[] propertyNames)
        {
            string result = "";
            foreach (var propertyName in propertyNames)
            {
                // It seems that `Item` is reserved property in C#
                if (propertyName == null || propertyName.Equals(string.Empty) ||
                    propertyName.Equals("Item") || propertyName.Equals("Id"))
                    continue;
                result += String.Format("{0}{1}", delimiter, GetStringValue(propertyName));
            }
            //Log.WriteLine("result: {0}", result);
            if (result.Length > 0)
                return result.Substring(1);
            return result;
        }

        // Returns an array contains a string like 'key = "value"'.
        public string[] FlattenColumnPairs(string[] columnNames)
        {
            int length = columnNames.Length;
            string[] result = new string[length];

            for (int i = 0; i < length; i++)
            {
                var columnName = columnNames[i];
                if (columnName == null || columnName.Equals(string.Empty) ||
                    columnName.Equals("item") || columnName.Equals("_id"))
                    continue;

                var propertyName = Utilities.ConvertSnakeCaseToTitleCase(columnName);
                result[i] = String.Format("{0} = {1}", columnName, GetStringValue(propertyName));
            }
            //Log.WriteLine("result: {0}", String.Join(",", result));
            return result;
        }

        // returns raw value as string (for database value)
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
                    text = String.Format("\"{0}\"", (string)this[propertyName]);
                    break;
            }
            return text;
        }
   }
}
