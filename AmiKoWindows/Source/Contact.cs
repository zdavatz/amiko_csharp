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
using System.Reflection;

namespace AmiKoWindows
{
    public class Contact
    {
        public long? Id { get; set; }

        public string TimeStamp { get; set; }
        public string Uid { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public string Birthdate { get; set; }

        public int _Gender { get; set; }
        public string Gender {
            get { return _Gender.ToString(); }
            set
            {
                int val = 0; Int32.TryParse(value.ToString(), out val);
                _Gender = val;
            }
        }

        public int _WeightKg { get; set; }
        public string WeightKg {
            get { return _WeightKg.ToString(); }
            set
            {
                int val = 0; Int32.TryParse(value.ToString(), out val);
                _WeightKg = val;
            }
        }

        public int _HeightCm { get; set; }
        public string HeightCm {
            get { return _HeightCm.ToString(); }
            set
            {
                int val = 0; Int32.TryParse(value.ToString(), out val);
                _HeightCm = val;
            }
        }

        public string Zip { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public object this[string propName]
        {
            get { return this.GetType().GetProperty(propName).GetValue(this, null); }
            set { this.GetType().GetProperty(propName).SetValue(this, value, null); }
        }

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

        // Generates new Dictionary with Properties, besides (`Item` and `Id`)
        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (var p in this.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance))
            {
                string propName = p.Name;
                // It seems that `Item` is reserved property in C#
                if (p == null || propName.Equals("Item") || propName.Equals("Id"))
                    continue;

                string v = p.GetValue(this, null) as string;
                values.Add(propName, v == null ? "" : v.ToString());
            }
            return values;
        }

        // Returns flattern property values for column name, using delimiter and enclosure
        public string Flatten(string[] names)
        {
            string delimiter = ",";
            string result = "";

            Dictionary<string, string> values = this.ToDictionary();
            foreach (var name in names)
            {
                if (name.Equals(string.Empty) || name.Equals("_id"))
                    continue;
                string text = "";
                string propName = Utilities.ConvertSnakeCaseToTitleCase(name);
                if (values.TryGetValue(propName, out text))
                    result += String.Format("{0}\"{1}\"", delimiter, text);
                else
                    result += delimiter;

            }
            if (result.Length > 0)
                return result.Substring(1);
            return result;
        }
    }
}
